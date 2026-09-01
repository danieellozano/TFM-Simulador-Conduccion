using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class DistanceEvaluator : MonoBehaviour
    {
        [Header("Referencias SOA")]
        public FloatVariable currentSpeed;    // CurrentSpeed.asset
        public GameEvent infractionEvent;     // OnInfractionDetected
        public InfraccionSO distanceRule;     // INT-DIST (Deficiente)
        public LayerMask trafficLayer;        // Capa 'Traffic'

        [Header("Configuración")]
        public float safetyTimeSeconds = 2.0f; // Regla de los 2 segundos
        private float violationTimer = 0f;

        private void Update()
        {
            RaycastHit hit;
            // Lanzamos un rayo hacia adelante de longitud fija (30 metros)
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, 30f, trafficLayer))
            {
                // Calculamos la distancia de seguridad necesaria: V(m/s) * tiempo
                float velocityMS = currentSpeed.Value / 3.6f;
                float requiredDistance = velocityMS * safetyTimeSeconds;

                if (hit.distance < requiredDistance)
                {
                    violationTimer += Time.deltaTime;
                    // Si el acoso persiste más de 3 segundos pegado al coche de delante
                    if (violationTimer > 3.0f)
                    {
                        infractionEvent.Raise(distanceRule);
                        Debug.Log("<color=orange>DGT DEFICIENTE:</color> Distancia de seguridad insuficiente.");
                        violationTimer = -5f; // Cooldown de 5 segundos para evitar spam
                    }
                }
                else 
                { 
                    violationTimer = 0f; 
                }
            }
            else
            {
                violationTimer = 0f;
            }
        }
    }
}