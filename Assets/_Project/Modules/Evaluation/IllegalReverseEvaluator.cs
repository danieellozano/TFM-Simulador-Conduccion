using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class IllegalReverseEvaluator : MonoBehaviour
    {
        [Header("Referencias SOA")]
        public InputDataSO inputData;      
        public FloatVariable vehicleSpeed;  
        public GameEvent infractionEvent; 
        public InfraccionSO reverseInfraction; 

        [Header("Estado")]
        [SerializeField] private bool isInsideSafeZone = false;

        private void Update()
        {
            if (inputData == null || vehicleSpeed == null) return;

            // REGLA: Si el coche está en marcha atrás (Gear -1) y moviéndose (> 0.1 Km/h)
            if (inputData.CurrentGear == -1 && vehicleSpeed.Value > 0.1f)
            {
                // Si NO estamos en una zona segura (Parking), la multa es instantánea
                if (!isInsideSafeZone)
                {
                    RegistrarInfraccionInstantanea();
                }
            }
        }

        private void RegistrarInfraccionInstantanea()
        {
            if (infractionEvent != null)
            {
                infractionEvent.Raise(reverseInfraction);
                Debug.Log("<color=red><b>[DGT]</b></color> Marcha atrás no permitida en vía pública.");
                
                // Desactivamos el script brevemente para evitar saturar la consola de mensajes
                this.enabled = false;
                Invoke("ReactivarSensor", 2f);
            }
        }

        private void ReactivarSensor() => this.enabled = true;

        // --- DETECCIÓN DE CONTEXTO ---
        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Parking") || other.CompareTag("Parking Meta") || other.CompareTag("ValidStopZone"))
            {
                isInsideSafeZone = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Parking") || other.CompareTag("Parking Meta") || other.CompareTag("ValidStopZone"))
            {
                isInsideSafeZone = false;
                Debug.Log("<color=white>SISTEMA:</color> Saliendo de zona segura.");
            }
        }
    }
}