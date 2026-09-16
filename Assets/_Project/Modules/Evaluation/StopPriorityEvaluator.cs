using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Evaluador de normativa DGT especializado en intersecciones urbanas complejas reguladas por STOP.
    // Supervisa de forma concurrente dos condiciones reglamentarias eliminatorias:
    // 1. La detención absoluta del vehículo del alumno (v < 0.1 km/h) dentro de la zona de influencia.
    // 2. La cesión efectiva de paso al tráfico transversal prioritario mediante análisis vectorial (Dot Product).
    public class StopPriorityEvaluator : MonoBehaviour
    {
        [Header("Referencias de Datos")]
        [Tooltip("Canal de telemetría que expone la velocidad instantánea del vehículo en km/h.")]
        public FloatVariable currentSpeed;

        [Tooltip("Canal del bus de eventos global para notificar infracciones a la arquitectura.")]
        public GameEvent infractionEvent;

        [Header("Reglas DGT")]
        [Tooltip("Activo normativo que define la falta eliminatoria por omisión de STOP (SIG-STOP).")]
        public InfraccionSO stopRule;

        [Tooltip("Activo normativo que define la falta eliminatoria por no ceder el paso a tráfico preferente (SIG-CEDER).")]
        public InfraccionSO yieldRule;

        [Header("Zonas de Peligro")]
        [Tooltip("Volúmenes de colisión que delimitan los carriles transversales con prioridad de paso.")]
        public BoxCollider[] dangerZones;

        [Tooltip("Máscara de capa utilizada para filtrar exclusivamente los vehículos del tráfico autónomo.")]
        public LayerMask trafficLayer;

        // Bandera que certifica si el alumno alcanzó el reposo absoluto en algún momento de su estancia
        private bool hasStoppedAtLeastOnce = false;

        // Bandera de presencia del vehículo del alumno dentro del área de control
        private bool isPlayerInside = false;

        // Monitoriza continuamente el velocímetro mientras el vehículo permanece dentro del área del cruce.
        private void Update()
        {
            // Solo evalúa si el alumno está dentro del cruce y aún no ha acreditado la parada completa
            if (isPlayerInside && !hasStoppedAtLeastOnce)
            {
                // Umbral cinemático estricto: velocidad inferior a 0.1 km/h equivale a reposo absoluto
                if (currentSpeed.Value < 0.1f)
                {
                    hasStoppedAtLeastOnce = true;
                    Debug.Log("<color=green>STOP:</color> Detención realizada. Esperando vía libre...");
                }
            }
        }

        // Inicializa el contexto de evaluación cuando el vehículo del alumno entra al área del STOP.
        // Parámetros:
        //   other: Colisionador del objeto que ingresa en el volumen de activación (Trigger).
        // Salida:
        //   No devuelve ningún valor (void).
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInside = true;
                hasStoppedAtLeastOnce = false; // Se resetea para exigir una detención dentro de este cruce específico
            }
        }

        // Audita el cumplimiento normativo en el instante exacto en que el vehículo abandona el cruce.
        // Parámetros:
        //   other: Colisionador del objeto que sale del volumen de activación (Trigger).
        // Salida:
        //   No devuelve ningún valor (void).
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // --- EVALUACIÓN 1: AUDITORÍA DE DETENCIÓN ABSOLUTA ---
                // Si abandonó el cruce sin haber detenido el coche por completo a 0 km/h en ningún frame
                if (!hasStoppedAtLeastOnce)
                {
                    infractionEvent.Raise(stopRule);
                    Debug.Log("<color=red>DGT ELIMINATORIA:</color> No se detuvo en el STOP.");
                }

                // --- EVALUACIÓN 2: AUDITORÍA DE CESIÓN DE PASO Y VIGILANCIA ---
                // Verifica si al reanudar la marcha e invadir el cruce existía una amenaza real aproximándose
                if (IsAnyThreatApproaching())
                {
                    infractionEvent.Raise(yieldRule);
                    Debug.Log("<color=red>DGT ELIMINATORIA:</color> No cedió el paso al reanudar la marcha.");
                }

                isPlayerInside = false;
            }
        }

        // Realiza un escaneo espacial y vectorial en las zonas de peligro para detectar vehículos preferentes en aproximación activa.
        // Parámetros:
        //   Ninguno.
        // Salida:
        //   True si detecta un vehículo con trayectoria orientada hacia la intersección; False si la vía está despejada o los coches se alejan.
        private bool IsAnyThreatApproaching()
        {
            foreach (var zone in dangerZones)
            {
                if (zone == null) continue;

                // Consulta de superposición de cajas para localizar vehículos de la IA dentro del volumen de conflicto
                Collider[] vehicles = Physics.OverlapBox(
                    zone.bounds.center, 
                    zone.bounds.extents, 
                    zone.transform.rotation, 
                    trafficLayer
                );
                
                foreach (var v in vehicles)
                {
                    // Vector de posición relativa desde el vehículo detectado hacia el punto de detención del STOP
                    Vector3 directionToIntersection = transform.position - v.transform.position;

                    // Producto escalar entre el vector hacia adelante del coche y la dirección normalizada al cruce
                    float approachCheck = Vector3.Dot(v.transform.forward, directionToIntersection.normalized);

                    // Si el producto escalar es positivo (> 0.1), el coche circula en sentido directo hacia el cruce (amenaza activa)
                    // Si es negativo, el vehículo ya cruzó la intersección o se está alejando, descartándose como falso positivo
                    if (approachCheck > 0.1f) return true;
                }
            }

            return false; // Vía libre de amenazas
        }
    }
}