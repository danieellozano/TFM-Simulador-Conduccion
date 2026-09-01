using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class PriorityEvaluator : MonoBehaviour
    {
        [Header("Zonas de Control")]
        [Tooltip("El área donde el ALUMNO invade al girar (Caja A)")]
        public BoxCollider studentConflictZone; 
        
        [Tooltip("El carril que viene de frente y tiene PRIORIDAD (Caja B)")]
        public BoxCollider aiPriorityZone; 

        [Header("Referencias SOA")]
        public GameEvent infractionEvent;
        public InfraccionSO yieldInfraction; // SIG-CEDER
        public LayerMask trafficLayer; // Capa 'Traffic'

        private bool infractionReported = false;

        private void Update()
        {
            // 1. ¿Está el alumno invadiendo el giro?
            bool studentInvading = Physics.CheckBox(
                studentConflictZone.bounds.center, 
                studentConflictZone.bounds.extents, 
                studentConflictZone.transform.rotation, 
                1 << LayerMask.NameToLayer("Player")
            );

            if (studentInvading)
            {
                // 2. ¿Hay alguna IA en el carril prioritario?
                bool aiInPriorityLane = Physics.CheckBox(
                    aiPriorityZone.bounds.center, 
                    aiPriorityZone.bounds.extents, 
                    aiPriorityZone.transform.rotation, 
                    trafficLayer
                );

                if (aiInPriorityLane && !infractionReported)
                {
                    // ¡COLISIÓN LÓGICA DE PRIORIDAD!
                    infractionEvent.Raise(yieldInfraction);
                    infractionReported = true;
                    Debug.Log("<color=red>DGT ELIMINATORIA:</color> No ceder el paso al carril prioritario.");
                    
                    // Cooldown para no repetir la multa en el mismo giro
                    Invoke("ResetInfraction", 5f);
                }
            }
            else
            {
                infractionReported = false;
            }
        }

        private void ResetInfraction() => infractionReported = false;
    }
}