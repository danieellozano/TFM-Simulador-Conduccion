using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class StopSignSensor : MonoBehaviour
    {
        [Header("Referencias de Datos")]
        public FloatVariable vehicleSpeed;  
        public GameEvent onStopSuccessful; 
        public InfraccionSO stopInfraction;
        public GameEvent infractionEvent;  

        private bool hasStopped = false;

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player") && !hasStopped)
            {
                if (vehicleSpeed != null && vehicleSpeed.Value < 0.1f)
                {
                    hasStopped = true;
                    if (onStopSuccessful != null) onStopSuccessful.Raise();
                    Debug.Log("<color=green>STOP CORRECTO:</color> Detención validada.");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (!hasStopped) 
                {
                    if (infractionEvent != null) infractionEvent.Raise(stopInfraction);
                    Debug.Log("<color=red>DGT:</color> No te detuviste en el STOP.");
                }
                hasStopped = false;
            }
        }
    }
}