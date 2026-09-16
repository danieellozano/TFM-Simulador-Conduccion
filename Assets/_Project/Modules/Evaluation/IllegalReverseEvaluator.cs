using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Audita y sanciona la maniobra de marcha atrás no autorizada en la vía pública (infracción SIG-REV).
    // De acuerdo con el Reglamento General de Conductores de la DGT, retroceder en calzada urbana constituye
    // una falta de gravedad Eliminatoria, autorizándose de forma exclusiva en zonas habilitadas para estacionamiento.
    // El componente discrimina el contexto espacial del vehículo mediante disparadores de zonas seguras.
    public class IllegalReverseEvaluator : MonoBehaviour
    {
        [Header("Referencias SOA")]
        [Tooltip("Contrato de datos de entrada del conductor para monitorizar la marcha actual engranada.")]
        public InputDataSO inputData;      

        [Tooltip("Canal de telemetría que registra la velocidad instantánea del utilitario en Km/h.")]
        public FloatVariable vehicleSpeed;  

        [Tooltip("Canal del bus de eventos globales para notificar la falta eliminatoria.")]
        public GameEvent infractionEvent; 

        [Tooltip("Activo de datos que parametriza la infracción eliminatoria por marcha atrás indebida (SIG-REV).")]
        public InfraccionSO reverseInfraction; 

        [Header("Estado")]
        [Tooltip("Indica si el vehículo se encuentra dentro de un volumen de estacionamiento donde la marcha atrás es legal.")]
        [SerializeField] private bool isInsideSafeZone = false;

        // Bucle de actualización donde se evalúa en cada fotograma la relación entre marcha, velocidad y contexto espacial
        private void Update()
        {
            if (inputData == null || vehicleSpeed == null) return;

            // Condición cinética: El conductor tiene engranada la marcha atrás (-1) y el utilitario registra movimiento real (> 0.1 Km/h)
            if (inputData.CurrentGear == -1 && vehicleSpeed.Value > 0.1f)
            {
                // Si la maniobra se produce fuera de los límites espaciales autorizados (Parking), se valida la infracción
                if (!isInsideSafeZone)
                {
                    RegistrarInfraccionInstantanea();
                }
            }
        }

        // Emite el evento de falta eliminatoria al bus reactivo y aplica una pausa temporal de seguridad
        private void RegistrarInfraccionInstantanea()
        {
            if (infractionEvent != null)
            {
                infractionEvent.Raise(reverseInfraction);
                Debug.Log("<color=red><b>[DGT]</b></color> Marcha atrás no permitida en vía pública.");
                
                // Desactiva temporalmente el componente durante 2.0 segundos para evitar la saturación del bus de eventos
                this.enabled = false;
                Invoke("ReactivarSensor", 2f);
            }
        }

        // Restablece la operatividad del sensor tras expirar el tiempo de enfriamiento
        private void ReactivarSensor() => this.enabled = true;

        // --- DETECCIÓN DE CONTEXTO ESPACIAL (ZONAS SEGURAS) ---

        // Valida de forma continua si el utilitario permanece dentro de una zona de maniobra permitida
        // Parámetros:
        //   other: Colisionador de la zona espacial con la que intersecta el vehículo.
        private void OnTriggerStay(Collider other)
        {
            // Las zonas de aparcamiento intermedio, final o detención reglamentaria eximen de sanción
            if (other.CompareTag("Parking") || other.CompareTag("Parking Meta") || other.CompareTag("ValidStopZone"))
            {
                isInsideSafeZone = true;
            }
        }

        // Detecta el abandono del volumen de control del estacionamiento, restableciendo la vigilancia estricta
        // Parámetros:
        //   other: Colisionador de la zona espacial que el vehículo acaba de abandonar.
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Parking") || other.CompareTag("Parking Meta") || other.CompareTag("ValidStopZone"))
            {
                isInsideSafeZone = false;
                Debug.Log("<color=white>SISTEMA:</color> Saliendo de zona segura.");
            }
        }
    }
}