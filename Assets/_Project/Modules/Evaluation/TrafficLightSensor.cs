using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class TrafficLightSensor : MonoBehaviour
    {
        public TrafficLightController lightController; 
        public GameEvent infractionEvent;
        public InfraccionSO redLightInfraction; 

        private void OnTriggerEnter(Collider other)
        {
            // PASO 1: ¿Algo ha tocado el sensor?
            Debug.Log($"<color=white>Semaforo:</color> Algo ha entrado en el sensor: {other.name}");

            // PASO 2: ¿Ese algo es el Player?
            if (other.CompareTag("Player"))
            {
                Debug.Log("<color=white>Semaforo:</color> Es el jugador. Estado actual: " + lightController.currentState);

                // PASO 3: ¿El semáforo está en rojo?
                if (lightController.currentState == LightState.Red)
                {
                    Debug.Log("<color=orange>Semaforo:</color> ¡Lanzando evento de infracción!");
                    if (infractionEvent != null) infractionEvent.Raise(redLightInfraction);
                }
            }
            else
            {
                Debug.Log("<color=yellow>Semaforo:</color> Ignorado. El objeto no tiene el Tag 'Player'.");
            }
        }
    }
}