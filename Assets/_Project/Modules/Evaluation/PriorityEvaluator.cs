using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Audita el cumplimiento reglamentario de la cesión de paso y prioridad en giros a la izquierda.
    // Supervisa la ocupación simultánea de dos volúmenes espaciales mediante comprobaciones de cajas (Physics.CheckBox):
    // el área de invasión del giro del alumno y la zona del carril contrario con preferencia de paso.
    // Si se detecta invasión concurrente, sanciona la falta eliminatoria de no ceder el paso (SIG-CEDER).
    public class PriorityEvaluator : MonoBehaviour
    {
        [Header("Zonas de Control")]
        [Tooltip("Volumen físico de colisión que delimita la trayectoria de giro del alumno hacia el carril contrario (Caja A).")]
        public BoxCollider studentConflictZone; 
        
        [Tooltip("Volumen físico de colisión que monitoriza el carril contrario que cuenta con preferencia de paso (Caja B).")]
        public BoxCollider aiPriorityZone; 

        [Header("Referencias SOA")]
        [Tooltip("Canal global del bus de eventos para emitir la notificación de infracción.")]
        public GameEvent infractionEvent;

        [Tooltip("Infracción eliminatoria oficial por no ceder el paso en intersecciones (SIG-CEDER).")]
        public InfraccionSO yieldInfraction;

        [Tooltip("Máscara de capas utilizada para filtrar exclusivamente los vehículos del tráfico autónomo (Traffic).")]
        public LayerMask trafficLayer;

        // Bandera de control para evitar la duplicación de sanciones dentro de una misma maniobra de giro
        private bool infractionReported = false;

        // Ciclo de actualización que supervisa de forma continua la presencia simultánea de vehículos en ambas zonas de conflicto.
        private void Update()
        {
            if (studentConflictZone == null || aiPriorityZone == null) return;

            // --- FASE 1: DETECCIÓN DE INVASIÓN DEL GIRO POR EL ALUMNO ---
            // Consulta espacial tridimensional orientada para comprobar si el vehículo del jugador invade la zona de giro
            bool studentInvading = Physics.CheckBox(
                studentConflictZone.bounds.center, 
                studentConflictZone.bounds.extents, 
                studentConflictZone.transform.rotation, 
                1 << LayerMask.NameToLayer("Player")
            );

            if (studentInvading)
            {
                // --- FASE 2: DETECCIÓN DE TRÁFICO PREFERENTE EN EL CARRIL CONTRARIO ---
                // Comprueba si en ese mismo instante hay un vehículo de la IA circulando por la zona de prioridad
                bool aiInPriorityLane = Physics.CheckBox(
                    aiPriorityZone.bounds.center, 
                    aiPriorityZone.bounds.extents, 
                    aiPriorityZone.transform.rotation, 
                    trafficLayer
                );

                // --- FASE 3: AUDITORÍA Y DISPARO DE INFRACCIÓN ---
                // Si coinciden ambas condiciones y no se ha sancionado ya en este giro, se registra la infracción
                if (aiInPriorityLane && !infractionReported)
                {
                    if (infractionEvent != null && yieldInfraction != null)
                    {
                        infractionEvent.Raise(yieldInfraction);
                    }

                    infractionReported = true;
                    Debug.Log("<color=red>DGT ELIMINATORIA:</color> No ceder el paso al carril prioritario.");
                    
                    // Ventana de enfriamiento de 5 segundos para no reiterar la sanción durante la misma maniobra
                    Invoke("ResetInfraction", 5f);
                }
            }
            else
            {
                // Al salir de la zona de conflicto, se restablece el estado para futuras intersecciones
                infractionReported = false;
            }
        }

        // Restablece la bandera de control tras expirar el tiempo de enfriamiento, permitiendo nuevas evaluaciones.
        // Parámetros:
        //   Ninguno.
        // Salida:
        //   No devuelve ningún valor (void).
        private void ResetInfraction() => infractionReported = false;
    }
}