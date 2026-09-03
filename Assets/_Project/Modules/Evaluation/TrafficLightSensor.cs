using UnityEngine;
using Simulador.Core; // Importante: acceso a la interfaz y enumerado definidos en el Core

namespace Simulador.Evaluation
{
    public class TrafficLightSensor : MonoBehaviour
    {
        // Referencia a la interfaz abstracta (no a la clase concreta del controlador)
        // Esto mantiene el desacoplamiento entre Evaluation e Infrastructure
        public MonoBehaviour lightControllerObject; 
        private ILightSource lightController;

        public GameEvent infractionEvent;
        public InfraccionSO redLightInfraction;

        private void Awake()
        {
            // Obtenemos la interfaz desde el componente asignado
            lightController = lightControllerObject as ILightSource;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // Solo si el alumno ABANDONA la zona mientras sigue en rojo
                if (lightController != null && lightController.CurrentState == LightState.Red)
                {
                    if (infractionEvent != null)
                    {
                        infractionEvent.Raise(redLightInfraction);
                        Debug.Log("<color=red>DGT:</color> Semáforo en rojo rebasado.");
                    }
                }
            }
        }
    }
}