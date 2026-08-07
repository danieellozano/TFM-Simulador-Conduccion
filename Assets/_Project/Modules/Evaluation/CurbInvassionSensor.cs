using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class CurbInvasionSensor : MonoBehaviour
    {
        public GameEvent infractionEvent;
        public InfraccionSO curbInfraction; // MAN_BORDILLO
        public float detectionDistance = 0.5f;

        private void Update()
        {
            RaycastHit hit;
            // Lanzamos un rayo grueso desde la rueda hacia abajo
            if (Physics.SphereCast(transform.position, 0.2f, Vector3.down, out hit, detectionDistance))
            {
                // Si lo que hay debajo de la rueda es un Bordillo...
                if (hit.collider.CompareTag("Bordillo") || hit.collider.CompareTag("Curb"))
                {
                    infractionEvent.Raise(curbInfraction);
                    Debug.Log("<color=red>DGT:</color> Rueda sobre bordillo detectada.");
                }
            }
        }
    }
}