using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Sensor de supervisión para intersecciones reguladas por semáforos.
    // Audita el paso del alumno mediante la detección de salida del área de control (OnTriggerExit) situada tras la línea de detención.
    // Consulta el estado del semáforo a través de la interfaz desacoplada ILightSource, emitiendo una infracción eliminatoria
    // (SIG-ROJ) si el vehículo cruza o invade el cruce mientras la fase luminosa permanece en rojo.
    public class TrafficLightSensor : MonoBehaviour
    {
        [Header("Contrato de Infraestructura")]
        [Tooltip("Objeto que contiene el componente controlador del semáforo. Se asigna como MonoBehaviour para permitir la inyección de dependencias en el Inspector.")]
        public MonoBehaviour lightControllerObject; 

        // Referencia en caché a la interfaz abstracta que expone el estado de las luces, evitando el acoplamiento directo con el módulo Infrastructure
        private ILightSource lightController;

        [Header("Reglas y Eventos")]
        [Tooltip("Canal del bus de eventos para notificar la falta al evaluador central de la DGT.")]
        public GameEvent infractionEvent;

        [Tooltip("Activo normativo que define la infracción eliminatoria por rebase de semáforo en rojo (SIG-ROJ).")]
        public InfraccionSO redLightInfraction;

        // Inicializa el componente y resuelve polimórficamente la interfaz ILightSource desde el objeto asignado.
        // Parámetros:
        //   Ninguno.
        // Salida:
        //   No devuelve ningún valor (void).
        private void Awake()
        {
            // Vinculación mediante casting a la interfaz para garantizar el desacoplamiento entre Evaluation e Infrastructure
            lightController = lightControllerObject as ILightSource;

            if (lightController == null && lightControllerObject != null)
            {
                Debug.LogError($"<color=red>TrafficLightSensor:</color> El objeto asignado en {gameObject.name} no implementa la interfaz ILightSource.");
            }
        }

        // Audita el rebase del cruce en el instante exacto en que el vehículo abandona el volumen de control.
        // Esta estrategia por salida permite que el alumno pueda frenar dentro del área sin ser sancionado de inmediato,
        // penalizándolo únicamente si continúa la marcha y cruza la intersección con la fase en rojo activa.
        // Parámetros:
        //   other: Colisionador del objeto que sale del volumen de activación (Trigger).
        // Salida:
        //   No devuelve ningún valor (void).
        private void OnTriggerExit(Collider other)
        {
            // Filtra exclusivamente el paso del vehículo del alumno
            if (other.CompareTag("Player"))
            {
                // Si el semáforo se encuentra en fase prohibitiva (rojo) al momento de abandonar el trigger, se valida la infracción
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