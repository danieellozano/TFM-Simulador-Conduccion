using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class TrafficLightSensor : MonoBehaviour
    {
        public TrafficLightController lightController; 
        public GameEvent infractionEvent;
        public InfraccionSO redLightInfraction; 

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // Solo si el alumno ABANDONA la zona mientras sigue en rojo
                if (lightController.currentState == LightState.Red)
                {
                    if (infractionEvent != null) infractionEvent.Raise(redLightInfraction);
                    Debug.Log("<color=red>DGT:</color> Semáforo en rojo rebasado.");
                }
            }
        }
    }
}