using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class StopSignSensor : MonoBehaviour
    {
        public FloatVariable vehicleSpeed;  
        public GameEvent onStopSuccessful; // El que avanza la fase
        public InfraccionSO stopInfraction;
        public GameEvent infractionEvent;

        private bool hasStopped = false;
        private bool isInside = false;

        // Usamos OnTriggerStay porque es lo que hace que tu Parking sí funcione
        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player") && !hasStopped)
            {
                isInside = true;
                // Si el coche se detiene
                if (vehicleSpeed.Value < 0.1f)
                {
                    hasStopped = true;
                    if (onStopSuccessful != null) onStopSuccessful.Raise();
                    Debug.Log("<color=green>STOP CORRECTO:</color> Enviando señal al Manager.");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // Si sale sin haber frenado -> MULTA
                if (!hasStopped && infractionEvent != null) 
                {
                    infractionEvent.Raise(stopInfraction);
                }
                // Reset para la próxima vez
                hasStopped = false;
                isInside = false;
            }
        }
    }
}