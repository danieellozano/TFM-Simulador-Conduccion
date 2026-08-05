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
                // Si abandonas la zona y el semáforo sigue en rojo -> Multa Eliminatoria
                if (lightController.currentState == LightState.Red)
                {
                    if (infractionEvent != null) infractionEvent.Raise(redLightInfraction);
                    Debug.Log("<color=red>DGT: Semáforo en rojo rebasado.</color>");
                }
            }
        }
    }
}