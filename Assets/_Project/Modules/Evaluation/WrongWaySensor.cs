using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class WrongWaySensor : MonoBehaviour
    {
        [Header("Referencias SOA")]
        public GameEvent infractionEvent;     // Arrastra OnInfractionDetected.asset
        public InfraccionSO wrongWayInfraction; // Arrastra SIG_SENT.asset (Eliminatoria)

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // Calculamos la dirección del coche respecto a la dirección del sensor
                // transform.forward del sensor debe apuntar HACIA AFUERA de la calle prohibida
                float alignment = Vector3.Dot(other.transform.forward, transform.forward);

                // Si el valor es negativo, significa que el coche y el sensor se miran de frente
                // (El alumno está intentando entrar en la dirección prohibida)
                if (alignment < -0.5f) 
                {
                    if (infractionEvent != null)
                    {
                        infractionEvent.Raise(wrongWayInfraction);
                        Debug.Log("<color=red>DGT: Entrada en sentido prohibido detectada.</color>");
                    }
                }
            }
        }
    }
}