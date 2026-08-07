using UnityEngine;

namespace Simulador.Evaluation
{
    public class AnticipationTrigger : MonoBehaviour 
    {
        public ParkingZone parentScript;

        private void OnTriggerEnter(Collider other) 
        {
            if (other.CompareTag("Player") && parentScript != null) 
                parentScript.RegistrarAnticipacion();
        }
    }
}