// using UnityEngine;
// using Simulador.Core;

// namespace Simulador.Evaluation
// {
//     public class DistanceEvaluator : MonoBehaviour
//     {
//         [Header("Referencias SOA")]
//         public FloatVariable currentSpeed;    // CurrentSpeed.asset
//         public GameEvent infractionEvent;     // OnInfractionDetected
//         public InfraccionSO distanceRule;     // INT-DIST (Deficiente)
//         public LayerMask trafficLayer;        // Capa 'Traffic'

//         [Header("Configuración")]
//         public float safetyTimeSeconds = 2.0f; // Regla de los 2 segundos
//         private float violationTimer = 0f;

//         private void Update()
//         {
//             RaycastHit hit;
//             // Lanzamos un rayo hacia adelante de longitud fija (30 metros)
//             if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, 30f, trafficLayer))
//             {
//                 // Calculamos la distancia de seguridad necesaria: V(m/s) * tiempo
//                 float velocityMS = currentSpeed.Value / 3.6f;
//                 float requiredDistance = velocityMS * safetyTimeSeconds;

//                 if (hit.distance < requiredDistance)
//                 {
//                     violationTimer += Time.deltaTime;
//                     // Si el acoso persiste más de 3 segundos pegado al coche de delante
//                     if (violationTimer > 3.0f)
//                     {
//                         infractionEvent.Raise(distanceRule);
//                         Debug.Log("<color=orange>DGT DEFICIENTE:</color> Distancia de seguridad insuficiente.");
//                         violationTimer = -5f; // Cooldown de 5 segundos para evitar spam
//                     }
//                 }
//                 else 
//                 { 
//                     violationTimer = 0f; 
//                 }
//             }
//             else
//             {
//                 violationTimer = 0f;
//             }
//         }
//     }
// }


using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Supervisa continuamente la separación longitudinal del utilitario respecto al vehículo precedente.
    // Implementa la regla oficial de los 2 segundos de la DGT mediante proyección de rayos (Raycasting) frontal,
    // calcula la distancia de frenado dinámica en función de la velocidad cinemática en tiempo real
    // y aplica un temporizador de persistencia (acoso / tailgating) de 3 segundos para sancionar la falta deficiente (INT-DIS).
    public class DistanceEvaluator : MonoBehaviour
    {
        [Header("Referencias SOA")]
        [Tooltip("Canal de telemetría que expone la velocidad instantánea del vehículo en Km/h.")]
        public FloatVariable currentSpeed;

        [Tooltip("Canal del bus de eventos globales para notificar la detección de la infracción.")]
        public GameEvent infractionEvent;

        [Tooltip("Activo de datos que define la falta Deficiente por distancia de seguridad insuficiente (INT-DIS).")]
        public InfraccionSO distanceRule;

        [Tooltip("Máscara de capas que restringe la detección exclusivamente a los vehículos del tráfico (Traffic).")]
        public LayerMask trafficLayer;

        [Header("Configuración")]
        [Tooltip("Constante temporal exigida por la DGT para garantizar un tiempo de reacción seguro (2.0 segundos).")]
        public float safetyTimeSeconds = 2.0f;

        // Cronómetro acumulativo que contabiliza el tiempo continuo circulando por debajo de la distancia reglamentaria
        private float violationTimer = 0f;

        // Bucle de actualización física que realiza el muestreo frontal de proximidad en cada fotograma
        private void Update()
        {
            if (currentSpeed == null) return;

            RaycastHit hit;

            // Lanza un rayo de detección frontal continuo desde una cota de 0.5m sobre el capó
            // con un alcance máximo de 30 metros, filtrando colisiones contra la capa de tráfico
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, 30f, trafficLayer))
            {
                // 1. CÁLCULO ANALÍTICO DE LA DISTANCIA DE SEGURIDAD REQUERIDA (Regla de los 2 segundos)
                // Convierte la velocidad instantánea de Km/h a metros por segundo (m/s)
                float velocityMS = currentSpeed.Value / 3.6f;

                // Distancia mínima en metros requerida: d = v * t
                float requiredDistance = velocityMS * safetyTimeSeconds;

                // 2. AUDITORÍA DE SEPARACIÓN RESPECTO AL VEHÍCULO DELANTERO
                // hit.distance proporciona la distancia física euclídea exacta en metros hacia el vehículo precedente
                if (hit.distance < requiredDistance)
                {
                    // Si el conductor se encuentra a menor distancia de la exigida, acumula tiempo de infracción
                    violationTimer += Time.deltaTime;

                    // Si la conducta de acoso persiste de manera ininterrumpida por más de 3.0 segundos
                    if (violationTimer > 3.0f)
                    {
                        if (infractionEvent != null && distanceRule != null)
                        {
                            infractionEvent.Raise(distanceRule);
                        }

                        Debug.Log("<color=orange>DGT DEFICIENTE:</color> Distancia de seguridad insuficiente.");

                        // Aplica un periodo de enfriamiento (cooldown) de 5 segundos fijando el temporizador a -5s
                        // para evitar la emisión masiva de sanciones mientras el usuario corrige la separación
                        violationTimer = -5f;
                    }
                }
                else 
                { 
                    // Si la distancia vuelve a ser segura, se restablece el cronómetro inmediatamente
                    violationTimer = 0f; 
                }
            }
            else
            {
                // Si el rayo no impacta con ningún vehículo por delante, la vía está despejada
                violationTimer = 0f;
            }
        }
    }
}