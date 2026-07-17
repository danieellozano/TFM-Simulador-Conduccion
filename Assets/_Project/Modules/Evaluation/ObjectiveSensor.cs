using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class ObjectiveSensor : MonoBehaviour
    {
        public GameEvent onObjectiveComplete; // Arrastra OnEslalonComplete

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (onObjectiveComplete != null) onObjectiveComplete.Raise();
                gameObject.SetActive(false); // Se apaga para no repetir
            }
        }
    }
}