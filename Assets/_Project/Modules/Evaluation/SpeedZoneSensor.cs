using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class SpeedZoneSensor : MonoBehaviour
    {
        [Header("Configuración del Tramo")]
        public float speedLimit = 30f; // El límite de esta calle específica
        public InfraccionSO speedInfraction; // Arrastra VEL_GEN (Leve) o VEL_MAX (Eliminatoria)

        [Header("Referencias")]
        public FloatVariable vehicleSpeedSO; // CurrentSpeed.asset
        public FloatVariable currentLimitSO; // CurrentSpeedLimit.asset
        public GameEvent infractionEvent;    // OnInfractionDetected.asset

        private bool isVehicleInside = false;
        private bool infractionAlreadySent = false;

        private void Update()
        {
            if (isVehicleInside)
            {
                // Si la velocidad del coche supera el límite del tramo
                if (vehicleSpeedSO.Value > speedLimit + 2f) // Margen de 2km/h
                {
                    if (!infractionAlreadySent)
                    {
                        infractionEvent.Raise(speedInfraction);
                        infractionAlreadySent = true; 
                        Debug.Log($"<color=red>DGT: Exceso de velocidad en zona {speedLimit}</color>");
                    }
                }
                else
                {
                    // Si vuelve a bajar del límite, permitimos detectar otra vez 
                    // (Opcional, según lo estricto que quieras ser)
                    infractionAlreadySent = false; 
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isVehicleInside = true;
                infractionAlreadySent = false;
                
                // Actualizamos el límite global para que el HUD/Evaluador lo sepan
                if (currentLimitSO != null) currentLimitSO.Value = speedLimit;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isVehicleInside = false;
            }
        }
    }
}