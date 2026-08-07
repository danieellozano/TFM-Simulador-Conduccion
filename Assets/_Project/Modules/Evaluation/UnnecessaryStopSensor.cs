using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class UnnecessaryStopSensor : MonoBehaviour
    {
        [Header("Referencias SOA")]
        public FloatVariable vehicleSpeed;     
        public BoolVariable isStalledSO;       
        public GameEvent infractionEvent;      
        public InfraccionSO stopInfraction;    

        [Header("Configuración")]
        public float timeAllowedToStop = 3.0f; 
        
        private float stopTimer = 0f;
        private bool infractionReported = false;

        // Cambiamos el bool por un contador para manejar zonas solapadas
        [SerializeField] private int zonesCount = 0; 

        private void Update()
        {
            // 1. Si el motor está calado, la parada está justificada. No contamos.
            if (isStalledSO != null && isStalledSO.Value == true) 
            {
                stopTimer = 0f;
                return;
            }

            // 2. REGLA: Velocidad < 0.1 Y no estamos tocando NINGUNA zona válida (zonesCount == 0)
            if (vehicleSpeed.Value < 0.1f && zonesCount <= 0)
            {
                stopTimer += Time.deltaTime;

                if (stopTimer >= timeAllowedToStop && !infractionReported)
                {
                    infractionEvent.Raise(stopInfraction);
                    infractionReported = true;
                    Debug.Log("<color=red><b>[DGT]</b></color> Parada innecesaria fuera de zona legal.");
                }
            }
            else
            {
                // Si el coche se mueve o entra en al menos una zona válida, reseteamos el cronómetro
                stopTimer = 0f;
                infractionReported = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Comprobamos si el objeto que tocamos es una zona de parada legal
            if (other.CompareTag("ValidStopZone") || other.CompareTag("Parking") || other.CompareTag("Parking Meta"))
            {
                zonesCount++;
                // Debug.Log("Entrando en zona válida. Conteo actual: " + zonesCount);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Al salir, restamos uno al contador
            if (other.CompareTag("ValidStopZone") || other.CompareTag("Parking") || other.CompareTag("Parking Meta"))
            {
                zonesCount = Mathf.Max(0, zonesCount - 1);
                // Debug.Log("Saliendo de zona válida. Conteo actual: " + zonesCount);
            }
        }
    }
}