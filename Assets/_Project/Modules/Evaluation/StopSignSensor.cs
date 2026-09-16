using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Sensor de detención obligatoria para circuitos cerrados y pistas de maniobras sin tráfico transversal.
    // Supervisa que el alumno alcance el reposo absoluto (v < 0.1 km/h) dentro del área de influencia de la señal de STOP.
    // Notifica el cumplimiento exitoso para avanzar la misión o dispara una falta eliminatoria si se abandona la zona sin frenar.
    public class StopSignSensor : MonoBehaviour
    {
        [Header("Referencias de Datos")]
        [Tooltip("Canal de telemetría que expone la velocidad lineal instantánea del vehículo en km/h.")]
        public FloatVariable vehicleSpeed;

        [Tooltip("Evento global que notifica al gestor de misiones que el alumno ha completado el STOP con éxito.")]
        public GameEvent onStopSuccessful;

        [Header("Reglas y Notificación de Infracción")]
        [Tooltip("Activo normativo que define la falta eliminatoria por no detenerse en señal de STOP (SIG-STOP).")]
        public InfraccionSO stopInfraction;

        [Tooltip("Canal del bus de eventos para emitir la infracción hacia el evaluador central.")]
        public GameEvent infractionEvent;

        // Bandera interna que confirma si el vehículo ha alcanzado la inmovilización total durante su estancia
        private bool hasStopped = false;

        // Evalúa en cada paso físico si el vehículo del alumno alcanza el reposo absoluto dentro de la zona de parada.
        // Parámetros:
        //   other: Colisionador del objeto que permanece dentro del volumen de activación (Trigger).
        // Salida:
        //   No devuelve ningún valor (void).
        private void OnTriggerStay(Collider other)
        {
            // Filtra exclusivamente por el vehículo del alumno y solo si aún no ha validado la parada
            if (other.CompareTag("Player") && !hasStopped)
            {
                // Umbral cinemático estricto: velocidad inferior a 0.1 km/h equivale a reposo absoluto
                if (vehicleSpeed != null && vehicleSpeed.Value < 0.1f)
                {
                    hasStopped = true;

                    // Emite el evento de éxito para avanzar de fase en el circuito de maniobras
                    if (onStopSuccessful != null) onStopSuccessful.Raise();
                    Debug.Log("<color=green>STOP CORRECTO:</color> Detención validada.");
                }
            }
        }

        // Audita el resultado de la maniobra en el instante en que el vehículo abandona el volumen de la señal.
        // Parámetros:
        //   other: Colisionador del objeto que sale del volumen de activación (Trigger).
        // Salida:
        //   No devuelve ningún valor (void).
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // Si el alumno abandona el área sin haber alcanzado el reposo absoluto, se sanciona la falta
                if (!hasStopped)
                {
                    if (infractionEvent != null) infractionEvent.Raise(stopInfraction);
                    Debug.Log("<color=red>DGT:</color> No te detuviste en el STOP.");
                }

                // Se restablece el estado para preparar el sensor ante futuras pasadas
                hasStopped = false;
            }
        }
    }
}