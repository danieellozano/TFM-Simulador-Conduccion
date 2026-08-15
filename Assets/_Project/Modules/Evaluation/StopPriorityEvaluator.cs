using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class StopPriorityEvaluator : MonoBehaviour
    {
        [Header("Referencias de Datos")]
        public FloatVariable currentSpeed;    // CurrentSpeed.asset
        public GameEvent infractionEvent;     // OnInfractionDetected.asset

        [Header("Reglas DGT")]
        public InfraccionSO stopRule;         // SIG-STOP (No frenar a 0)
        public InfraccionSO yieldRule;        // SIG-CEDER (Cortar el paso)

        [Header("Zonas de Peligro")]
        [Tooltip("Los carriles de la calle transversal que tienen prioridad")]
        public BoxCollider[] dangerZones; 
        public LayerMask trafficLayer;        // Capa 'Traffic'

        private bool hasStoppedAtLeastOnce = false;
        private bool isPlayerInside = false;

        private void Update()
        {
            if (isPlayerInside && !hasStoppedAtLeastOnce)
            {
                // Validación de la parada obligatoria
                if (currentSpeed.Value < 0.1f)
                {
                    hasStoppedAtLeastOnce = true;
                    Debug.Log("<color=green>STOP:</color> Detención realizada. Esperando vía libre...");
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInside = true;
                hasStoppedAtLeastOnce = false;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // AL SALIR, EL JUEZ DICTA SENTENCIA:
                
                // 1. ¿Llegó a frenar a 0 en algún momento?
                if (!hasStoppedAtLeastOnce)
                {
                    infractionEvent.Raise(stopRule);
                    Debug.Log("<color=red>DGT ELIMINATORIA:</color> No se detuvo en el STOP.");
                }

                // 2. ¿Había tráfico en la zona de peligro en el momento de salir?
                if (IsAnyThreatApproaching())
                {
                    infractionEvent.Raise(yieldRule);
                    Debug.Log("<color=red>DGT ELIMINATORIA:</color> No cedió el paso al reanudar la marcha.");
                }

                isPlayerInside = false;
            }
        }

        private bool IsAnyThreatApproaching()
        {
            foreach (var zone in dangerZones)
            {
                // Buscamos coches de la IA en los carriles transversales
                Collider[] vehicles = Physics.OverlapBox(zone.bounds.center, zone.bounds.extents, zone.transform.rotation, trafficLayer);
                
                foreach (var v in vehicles)
                {
                    // Usamos el Dot Product para ver si el coche viene HACIA el cruce
                    Vector3 directionToIntersection = transform.position - v.transform.position;
                    float approachCheck = Vector3.Dot(v.transform.forward, directionToIntersection.normalized);

                    if (approachCheck > 0.1f) return true; // Amenaza detectada
                }
            }
            return false;
        }
    }
}