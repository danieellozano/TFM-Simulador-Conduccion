using UnityEngine;

namespace Simulador.Evaluation
{
    public class ConditionalStopZone : MonoBehaviour
    {
        [Header("Zonas")]
        [Tooltip("La zona donde el alumno espera antes de girar")]
        public GameObject waitingArea; 
        
        [Tooltip("El carril contrario que tiene prioridad")]
        public BoxCollider priorityLaneZone; 

        [Header("Configuración")]
        public LayerMask vehicleLayers; // Capa 'Traffic' y 'Player'

        private void Update()
        {
            if (priorityLaneZone == null || waitingArea == null) return;

            // 1. Detectamos si hay alguna IA (u otro vehículo) en el carril prioritario
            bool isTrafficComing = Physics.CheckBox(
                priorityLaneZone.bounds.center, 
                priorityLaneZone.bounds.extents, 
                priorityLaneZone.transform.rotation, 
                vehicleLayers
            );

            // 2. LÓGICA DINÁMICA DE TAGS
            if (isTrafficComing)
            {
                // Si viene alguien, permitimos que el alumno se detenga
                waitingArea.tag = "ValidStopZone";
                // Opcional: Debug.Log("Ceda el paso ACTIVO: Zona de parada permitida.");
            }
            else
            {
                // Si la calle está vacía, no hay excusa para pararse
                waitingArea.tag = "Untagged";
            }
        }
    }
}