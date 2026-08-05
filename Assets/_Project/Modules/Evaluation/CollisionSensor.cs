using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class CollisionSensor : MonoBehaviour
    {
        [Header("Referencias SOA")]
        public GameEvent infractionEvent;     // Arrastra OnInfractionDetected.asset
        public InfraccionSO curbInfraction;   // Arrastra MAN_BORDILLO.asset (Eliminatoria)

        [Header("Configuración")]
        public float minCollisionForce = 2f; // Para evitar que multen por rozar a 1km/h (opcional)

        private void OnCollisionEnter(Collision collision)
        {
            // Verificamos si el objeto chocado tiene el Tag "Bordillo"
            if (collision.gameObject.CompareTag("Bordillo"))
            {
                // Registramos la infracción
                if (infractionEvent != null)
                {
                    infractionEvent.Raise(curbInfraction);
                    Debug.Log("<color=red>DGT: Impacto contra bordillo detectado.</color>");
                }
            }
        }
    }
}