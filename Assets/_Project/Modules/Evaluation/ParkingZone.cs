using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Coordina y audita de forma integral las maniobras de estacionamiento (en línea o batería).
    // Implementa una arquitectura multi-etapa que valida: la anticipación previa con intermitente,
    // la reserva de espacio frente al tráfico autónomo mediante un muro de cortesía en la capa AI_Blocker,
    // la inmovilización mecánica del vehículo con freno de mano dentro de un margen de tiempo y
    // la detección de colisiones físicas contra bordillos o vehículos contiguos durante la maniobra.
    public class ParkingZone : MonoBehaviour
    {
        [Header("Configuración de Maniobra")]
        [Tooltip("Indica si esta plaza corresponde al hito final de finalización del examen o a una maniobra intermedia.")]
        public bool isFinalGoal = false;

        [Tooltip("Lado reglamentario hacia el que se debe señalizar el estacionamiento (-1 para Izquierda, 1 para Derecha).")]
        public int requiredBlinkerSide;

        [Tooltip("Tiempo de tolerancia (en segundos) que el alumno puede permanecer detenido en la plaza antes de sancionar la omisión del freno de mano.")]
        public float waitTimeBeforePenalty = 5.0f;

        [Header("Gestión de Tráfico IA")]
        [Tooltip("Colisionador invisible situado en la capa AI_Blocker que retiene el tráfico trasero mientras el alumno maniobra.")]
        public GameObject aiCourtesyBlocker; 

        [Header("Referencias SOA")]
        [Tooltip("Canal de datos de entrada del usuario para consultar el intermitente activo y el estado del freno de mano.")]
        public InputDataSO inputData;

        [Tooltip("Canal de telemetría que expone la velocidad lineal instantánea del vehículo en km/h.")]
        public FloatVariable vehicleSpeed;

        [Tooltip("Evento invocado al completar con éxito una maniobra intermedia en el circuito.")]
        public GameEvent onManiobraSuccess;

        [Tooltip("Evento invocado al completar satisfactoriamente la meta final del examen.")]
        public GameEvent onMissionComplete;

        [Tooltip("Canal global del bus de eventos para notificar infracciones normativas DGT.")]
        public GameEvent infractionEvent;
        
        [Header("Reglas DGT")]
        [Tooltip("Infracción leve por omitir la señalización luminosa anticipada en estacionamiento (EST-FL).")]
        public InfraccionSO blinkerInfraction;

        [Tooltip("Infracción deficiente por no asegurar mecánicamente el vehículo con el freno de mano al estacionar (EST-FD).")]
        public InfraccionSO noHandbrakeInfraction;

        [Tooltip("Infracción eliminatoria por impacto físico contra bordillo u otros vehículos durante el estacionamiento (EST-FE).")]
        public InfraccionSO ruleEliminatoria;

        // Banderas y contadores de estado lógico interno
        private bool hasSignaledInTime = false;       // Valida si se activó el intermitente correcto en la aproximación
        private bool isInsideFinalSpot = false;       // Indica si el vehículo ha entrado en los límites de la plaza
        private bool isManiobraEvaluated = false;     // Evita reevaluaciones una vez emitido el veredicto
        private bool isVehicleNear = false;           // Indica si el vehículo está en el perímetro de aproximación
        private float stopTimer = 0f;                 // Cronómetro para la tolerancia de inmovilización

        // Registra el ingreso del vehículo en el perímetro de aproximación y evalúa la señalización luminosa.
        // Método invocado desde el componente secundario AnticipationTrigger.
        // Parámetros:
        //   Ninguno.
        // Salida:
        //   No devuelve ningún valor (void).
        public void RegistrarAnticipacion()
        {
            if (isManiobraEvaluated) return;

            isVehicleNear = true;
            // Comprueba si el intermitente activado coincide con el lado de la plaza asignada
            hasSignaledInTime = (inputData.ActiveBlinker == requiredBlinkerSide);
        }

        // Actualiza el estado de presencia del vehículo dentro del volumen final de estacionamiento.
        // Método invocado desde el componente secundario SpotTrigger.
        // Parámetros:
        //   inside: Verdadero si el vehículo entra al área de la plaza; falso al abandonarla.
        // Salida:
        //   No devuelve ningún valor (void).
        public void SetInsideFinalSpot(bool inside)
        {
            isInsideFinalSpot = inside;
            isVehicleNear = inside; // Si está dentro de la plaza, permanece dentro del área de influencia

            // Si el coche sale de la plaza sin finalizar, se reinicia el temporizador de detención
            if (!inside) stopTimer = 0f; 
        }

        // Ciclo de actualización que supervisa el bloqueo de cortesía para la IA y la inmovilización segura del coche.
        private void Update()
        {
            // --- 1. GESTIÓN DEL MURO DE CORTESÍA PARA EL TRÁFICO AUTÓNOMO ---
            if (aiCourtesyBlocker != null)
            {
                // El muro se habilita únicamente si el alumno está cerca, la maniobra está en curso
                // y se encuentra señalizando correctamente hacia la plaza con el intermitente
                bool shouldBlockAI = isVehicleNear && 
                                     !isManiobraEvaluated && 
                                     inputData.ActiveBlinker == requiredBlinkerSide;

                aiCourtesyBlocker.SetActive(shouldBlockAI);
            }

            // --- 2. AUDITORÍA DE INMOVILIZACIÓN Y FRENO DE MANO ---
            // Solo se evalúa si el vehículo se encuentra dentro de la plaza y la maniobra no ha concluido
            if (!isInsideFinalSpot || isManiobraEvaluated) return;

            // Detección de reposo absoluto (< 0.1 km/h)
            if (vehicleSpeed.Value < 0.1f)
            {
                stopTimer += Time.deltaTime;

                // Caso de éxito: el alumno asegura el vehículo accionando el freno de mano
                if (inputData.Handbrake)
                {
                    FinalizarMision(true);
                }
                // Caso de fallo por omisión: se agota la ventana de tolerancia de 5 segundos sin accionar el freno
                else if (stopTimer >= waitTimeBeforePenalty)
                {
                    FinalizarMision(false);
                }
            }
            else 
            { 
                // Si el vehículo reanuda el movimiento o hace ajustes de maniobra, se resetea el reloj
                stopTimer = 0f; 
            }
        }

        // Registra colisiones producidas durante la maniobra antes de inmovilizar el vehículo.
        // Invocado por CollisionEvaluator ante impactos contra bordillos, mobiliario o coches aparcados.
        // Parámetros:
        //   Ninguno.
        // Salida:
        //   No devuelve ningún valor (void).
        public void RegistrarColisionEnZona()
        {
            // Solo se sanciona si el impacto ocurre dentro de la plaza y antes de asegurar el freno de mano
            if (isInsideFinalSpot && !inputData.Handbrake && !isManiobraEvaluated)
            {
                if (infractionEvent != null && ruleEliminatoria != null)
                {
                    infractionEvent.Raise(ruleEliminatoria);
                }
            }
        }

        // Concluye formalmente el estacionamiento, retira los bloqueos de tráfico y procesa las faltas acumuladas.
        // Parámetros:
        //   pusoFrenoMano: Verdadero si el alumno accionó el freno de mano dentro de la tolerancia; falso en caso contrario.
        // Salida:
        //   No devuelve ningún valor (void).
        private void FinalizarMision(bool pusoFrenoMano)
        {
            isManiobraEvaluated = true;

            // Desactiva inmediatamente la barrera física de cortesía para reanudar la fluidez del tráfico de la IA
            if (aiCourtesyBlocker != null) 
            {
                aiCourtesyBlocker.SetActive(false);
            }

            // Registro de infracción leve si no se señalizó con antelación en la aproximación
            if (!hasSignaledInTime) 
            {
                infractionEvent?.Raise(blinkerInfraction);
            }

            // Registro de falta deficiente si no se aseguró el coche con el freno de mano
            if (!pusoFrenoMano) 
            {
                infractionEvent?.Raise(noHandbrakeInfraction);
            }

            // Disparo del evento de éxito correspondiente según la fase activa del examen
            if (isFinalGoal) 
            {
                onMissionComplete?.Raise();
            }
            else 
            {
                onManiobraSuccess?.Raise();
            }
        }

        // Restablece el entorno y libera el paso de la IA si el alumno abandona la zona sin aparcar.
        // Parámetros:
        //   other: Colisionador que abandona el volumen de influencia del disparador.
        // Salida:
        //   No devuelve ningún valor (void).
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isVehicleNear = false;
                isInsideFinalSpot = false;

                // Desactiva la barrera de cortesía para evitar retenciones artificiales en la calle
                if (aiCourtesyBlocker != null) 
                {
                    aiCourtesyBlocker.SetActive(false);
                }
            }
        }
    }
}