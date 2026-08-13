using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines; // Necesario para referenciar los carriles

namespace Simulador.AI
{
    public class PriorityIntersection : MonoBehaviour
    {
        [Header("Zonas de Control")]
        public BoxCollider yieldZone;   // El área de espera
        public BoxCollider dangerZone;  // El carril contrario con prioridad

        [Header("Filtro de Maniobra")]
        [Tooltip("Arrastra aquí los Splines que corresponden al GIRO A LA IZQUIERDA")]
        public List<SplineContainer> yieldingSplines; 

        [Header("Capas")]
        public LayerMask vehicleLayers;

        private List<TrafficAIController> waitingCars = new List<TrafficAIController>();

        private void Update()
        {
            // 1. ¿Viene alguien de frente?
            bool carComingOpposite = CheckForDanger();

            // 2. Revisar cada coche que está en el área de conflicto
            foreach (var ai in waitingCars)
            {
                if (ai != null)
                {
                    // LA CLAVE: ¿El carril actual de esta IA es uno de los que debe ceder el paso?
                    bool needsToYield = yieldingSplines.Contains(ai.currentSpline);

                    if (needsToYield)
                    {
                        // Si va a girar, obedece al peligro de frente
                        ai.externalStop = carComingOpposite;
                    }
                    else
                    {
                        // Si va recto, ignora el peligro de frente
                        ai.externalStop = false;
                    }
                }
            }
        }

        private bool CheckForDanger()
        {
            Collider[] obstacles = Physics.OverlapBox(dangerZone.bounds.center, dangerZone.bounds.extents, dangerZone.transform.rotation, vehicleLayers);
            foreach (var obs in obstacles)
            {
                // Usamos el Dot Product para ignorar coches que se alejan
                float directionCheck = Vector3.Dot(obs.transform.forward, transform.forward);
                if (directionCheck < -0.2f) return true;
            }
            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            var ai = other.GetComponent<TrafficAIController>();
            if (ai != null && !waitingCars.Contains(ai)) waitingCars.Add(ai);
        }

        private void OnTriggerExit(Collider other)
        {
            var ai = other.GetComponent<TrafficAIController>();
            if (ai != null && waitingCars.Contains(ai))
            {
                ai.externalStop = false;
                waitingCars.Remove(ai);
            }
        }
    }
}