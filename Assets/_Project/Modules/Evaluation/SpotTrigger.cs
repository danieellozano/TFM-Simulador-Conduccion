using UnityEngine;
public class SpotTrigger : MonoBehaviour {
    public Simulador.Evaluation.ParkingZone parentScript;
    private void OnTriggerEnter(Collider other) {
        if(other.CompareTag("Player")) parentScript.SetInsideFinalSpot(true);
    }
    private void OnTriggerExit(Collider other) {
        if(other.CompareTag("Player")) parentScript.SetInsideFinalSpot(false);
    }
}