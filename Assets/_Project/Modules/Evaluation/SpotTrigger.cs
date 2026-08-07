using UnityEngine;

namespace Simulador.Evaluation
{
    public class SpotTrigger : MonoBehaviour 
    {
        public ParkingZone parentScript;

        private void OnTriggerEnter(Collider other) 
        {
            if (other.CompareTag("Player") && parentScript != null) 
                parentScript.SetInsideFinalSpot(true);
        }

        private void OnTriggerExit(Collider other) 
        {
            if (other.CompareTag("Player") && parentScript != null) 
                parentScript.SetInsideFinalSpot(false);
        }
    }
}