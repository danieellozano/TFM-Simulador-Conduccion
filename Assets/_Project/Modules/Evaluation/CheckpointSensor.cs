using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class CheckpointSensor : MonoBehaviour
    {
        public GameEvent onReachedEvent; // El evento que "tarea.eventoCompletado" escucha

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (onReachedEvent != null) onReachedEvent.Raise();
                gameObject.SetActive(false); // Se apaga para no repetir
            }
        }
    }
}