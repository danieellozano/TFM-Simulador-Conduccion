// using UnityEngine;

// namespace Simulador.Evaluation
// {
//     public class ConditionalStopZone : MonoBehaviour
//     {
//         [Header("Zonas")]
//         [Tooltip("La zona donde el alumno espera antes de girar")]
//         public GameObject waitingArea; 
        
//         [Tooltip("El carril contrario que tiene prioridad")]
//         public BoxCollider priorityLaneZone; 

//         [Header("Configuración")]
//         public LayerMask vehicleLayers; // Capa 'Traffic' y 'Player'

//         private void Update()
//         {
//             if (priorityLaneZone == null || waitingArea == null) return;

//             // 1. Detectamos si hay alguna IA (u otro vehículo) en el carril prioritario
//             bool isTrafficComing = Physics.CheckBox(
//                 priorityLaneZone.bounds.center, 
//                 priorityLaneZone.bounds.extents, 
//                 priorityLaneZone.transform.rotation, 
//                 vehicleLayers
//             );

//             // 2. LÓGICA DINÁMICA DE TAGS
//             if (isTrafficComing)
//             {
//                 // Si viene alguien, permitimos que el alumno se detenga
//                 waitingArea.tag = "ValidStopZone";
//                 // Opcional: Debug.Log("Ceda el paso ACTIVO: Zona de parada permitida.");
//             }
//             else
//             {
//                 // Si la calle está vacía, no hay excusa para pararse
//                 waitingArea.tag = "Untagged";
//             }
//         }
//     }
// }

using UnityEngine;

namespace Simulador.Evaluation
{
    // Coordina la evaluación de detenciones condicionales en maniobras de giro sin prioridad (ej. giros a la izquierda).
    // Modifica dinámicamente la etiqueta (Tag) del área de espera en la calzada según la presencia de tráfico de frente,
    // permitiendo al sensor de paradas innecesarias (UnnecessaryStopSensor) distinguir entre una detención justificada
    // para ceder el paso ("ValidStopZone") y una obstrucción indebida del tráfico si la vía está vacía ("Untagged").
    public class ConditionalStopZone : MonoBehaviour
    {
        [Header("Zonas de Control Espacial")]
        [Tooltip("Volumen físico de espera en la calzada donde el alumno debe detenerse para ceder el paso antes de girar.")]
        public GameObject waitingArea; 
        
        [Tooltip("Colisionador tridimensional que supervisa el carril contrario con prioridad de paso.")]
        public BoxCollider priorityLaneZone; 

        [Header("Configuración de Capas")]
        [Tooltip("Máscara de capas que filtra las entidades a supervisar en el carril prioritario (Traffic y Player).")]
        public LayerMask vehicleLayers;

        // Bucle de actualización donde se realiza el escaneo espacial continuo del carril prioritario
        private void Update()
        {
            // Verificación de seguridad para evitar excepciones por referencias nulas en la escena
            if (priorityLaneZone == null || waitingArea == null) return;

            // 1. ESCANEO FÍSICO DE AMENAZAS EN EL CARRIL PRIORITARIO
            // Evalúa mediante una caja orientada si existe algún vehículo invadiendo la zona de peligro
            bool isTrafficComing = Physics.CheckBox(
                priorityLaneZone.bounds.center, 
                priorityLaneZone.bounds.extents, 
                priorityLaneZone.transform.rotation, 
                vehicleLayers
            );

            // 2. GESTIÓN DINÁMICA DEL CONTEXTO NORMATIVO MEDIANTE ETIQUETAS (TAGS)
            if (isTrafficComing)
            {
                // Si hay tráfico aproximándose de frente, detenerse está reglamentariamente justificado:
                // Se marca el área como protegida para que el sistema exima al alumno de sanciones por detención
                waitingArea.tag = "ValidStopZone";
            }
            else
            {
                // Si el carril contrario está completamente despejado, no existe motivo para detener el utilitario:
                // Se retira la etiqueta protectora; detenerse aquí durante más de 3 segundos se computará como parada injustificada (CON-PAR)
                waitingArea.tag = "Untagged";
            }
        }
    }
}