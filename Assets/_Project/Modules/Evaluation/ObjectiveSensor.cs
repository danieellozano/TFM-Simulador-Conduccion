using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Delimita espacialmente los hitos de paso, puntos de control intermedios y metas de las misiones pedagógicas.
    // A diferencia de los sensores de evaluación normativa DGT, este componente se encarga de la orquestación del progreso,
    // detectando de forma unívoca la llegada del alumno y notificando al gestor de misiones correspondiente para avanzar de fase.
    public class ObjectiveSensor : MonoBehaviour
    {
        [Header("Eventos de Misión")]
        [Tooltip("Canal del bus de eventos que se dispara para notificar que el hito u objetivo ha sido completado con éxito.")]
        public GameEvent onObjectiveComplete; 

        // Detecta el ingreso del vehículo en el volumen de control del hito.
        // Parámetros:
        //   other: Colisionador entrante que intersecta con el volumen del disparador (Trigger).
        // Salida:
        //   No devuelve ningún valor (void).
        private void OnTriggerEnter(Collider other)
        {
            // Filtro de exclusividad: únicamente reacciona ante el vehículo del alumno, ignorando el tráfico de la IA
            if (other.CompareTag("Player"))
            {
                // Notificación reactiva al orquestador global de la misión para avanzar al siguiente objetivo
                if (onObjectiveComplete != null)
                {
                    onObjectiveComplete.Raise();
                }

                // Mecanismo de persistencia y consumo de hito: se desactiva inmediatamente tras el primer contacto
                // para garantizar que el punto de control sea consumido una única vez y evitar falsos avances redundantes
                gameObject.SetActive(false);
            }
        }
    }
}