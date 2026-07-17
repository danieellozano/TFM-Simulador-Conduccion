using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class ParkingZone : MonoBehaviour
    {
        [Header("Configuración")]
        public bool isFinalGoal = false;
        public float timeToConfirm = 2.0f; // Segundos que debe estar quieto
        
        [Header("Referencias")]
        public FloatVariable vehicleSpeed;
        public GameEvent onMissionComplete;
        public GameEvent onManiobraSuccess;

        private float stopTimer = 0f;
        private bool isParked = false;

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player") && !isParked)
            {
                // Si el coche está casi parado (< 0.1 Km/h)
                if (vehicleSpeed.Value < 0.1f)
                {
                    stopTimer += Time.deltaTime; // Empezamos a contar

                    if (stopTimer >= timeToConfirm)
                    {
                        isParked = true;
                        ProcesarExito();
                    }
                }
                else
                {
                    stopTimer = 0f; // Si se mueve, reseteamos el tiempo
                }
            }
        }

        private void ProcesarExito()
        {
            if (isFinalGoal) {
                if (onMissionComplete != null) onMissionComplete.Raise();
            } else {
                if (onManiobraSuccess != null) onManiobraSuccess.Raise();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isParked = false;
                stopTimer = 0f;
            }
        }
    }
}