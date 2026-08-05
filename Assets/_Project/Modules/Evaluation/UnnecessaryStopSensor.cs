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
        private bool isInsideValidZone = false;
        private bool infractionReported = false;

        private void Update()
        {
            // Verificamos si el motor está calado mediante el ScriptableObject
            if (isStalledSO != null && isStalledSO.Value == true) 
            {
                stopTimer = 0f;
                return;
            }

            // REGLA: Velocidad < 0.1 Y No estoy en zona válida
            if (vehicleSpeed.Value < 0.1f && !isInsideValidZone)
            {
                stopTimer += Time.deltaTime;

                if (stopTimer >= timeAllowedToStop && !infractionReported)
                {
                    infractionEvent.Raise(stopInfraction);
                    infractionReported = true;
                    Debug.Log("<color=red>DGT: Parada innecesaria detectada.</color>");
                }
            }
            else
            {
                stopTimer = 0f;
                infractionReported = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("ValidStopZone"))
            {
                isInsideValidZone = true;
                stopTimer = 0f;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("ValidStopZone"))
            {
                isInsideValidZone = false;
            }
        }
    }
}