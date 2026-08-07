using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class CollisionEvaluator : MonoBehaviour
    {
        [Header("Referencias SOA")]
        public GameEvent infractionEvent;     
        public InfraccionSO curbInfraction;    // MAN_BORDILLO
        public InfraccionSO objectInfraction;  // SIG_OBSTACULO

        [Header("Configuración Física")]
        public float impulseThreshold = 100f; 

        private void OnCollisionEnter(Collision collision)
        {
            float impactForce = collision.impulse.magnitude / Time.fixedDeltaTime;
            if (impactForce < impulseThreshold) return;

            // CASO A: Impacto con Bordillo
            if (collision.gameObject.CompareTag("Bordillo"))
            {
                RegistrarFalta(curbInfraction);
                
                // Avisamos a la zona de parking si el choque fue por no frenar bien
                ParkingZone activeZone = Object.FindFirstObjectByType<ParkingZone>();
                if (activeZone != null) activeZone.RegistrarColisionEnZona();
            }
            // CASO B: Impacto con mobiliario urbano
            else if (collision.gameObject.CompareTag("Obstacle"))
            {
                RegistrarFalta(objectInfraction);
            }
        }

        // Este es el método que te faltaba
        private void RegistrarFalta(InfraccionSO info)
        {
            if (infractionEvent != null && info != null)
            {
                infractionEvent.Raise(info);
                Debug.Log($"<color=red><b>[DGT COLISIÓN]</b></color> {info.descripcion}");
            }
        }
    }
}