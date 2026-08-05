using UnityEngine;
public class AnticipationTrigger : MonoBehaviour {
    public Simulador.Evaluation.ParkingZone parentScript;
    private void OnTriggerEnter(Collider other) {
        if(other.CompareTag("Player")) parentScript.RegistrarAnticipacion();
    }
}