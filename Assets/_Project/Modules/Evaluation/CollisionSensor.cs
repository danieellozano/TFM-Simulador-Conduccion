using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class CollisionSensor : MonoBehaviour
    {
        [Header("Configuración")]
        public GameEvent infractionEvent; // Arrastra OnInfractionDetected.asset
        public InfraccionSO curbInfraction; // Arrastra MAN_BORDILLO.asset

        private void OnCollisionEnter(Collision collision)
        {
            // Verificamos si el objeto tiene el Tag "Curb"
            if (collision.gameObject.CompareTag("Curb"))
            {
                // "Gritamos" al sistema que ha habido una infracción
                if (infractionEvent != null && curbInfraction != null)
                {
                    infractionEvent.Raise(curbInfraction);
                }
            }
        }
    }
}