using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class ParkingZone : MonoBehaviour
    {
        [Header("Configuración de Maniobra")]
        public bool isFinalGoal = false;
        public int requiredBlinkerSide; // -1 Izq, 1 Der
        public float timeToConfirm = 2.0f;

        [Header("Referencias SOA")]
        public InputDataSO inputData;
        public FloatVariable vehicleSpeed;
        public GameEvent onManiobraSuccess;
        public GameEvent onMissionComplete;
        public GameEvent infractionEvent;
        public InfraccionSO blinkerInfraction;

        // ESTADOS LÓGICOS INTERNOS
        private bool hasSignaledInTime = false;
        private bool isInsideFinalSpot = false;
        private bool isParked = false;
        private float stopTimer = 0f;

        // 1. LLAMADO POR EL HIJO "ZONA ANTICIPACION"
        public void RegistrarAnticipacion()
        {
            // Verificamos el intermitente en el momento de aproximación
            hasSignaledInTime = (inputData.ActiveBlinker == requiredBlinkerSide);
            
            if (hasSignaledInTime) Debug.Log("<color=cyan>INFO:</color> Aproximación señalizada.");
        }

        // 2. LLAMADO POR EL HIJO "ZONA APARCAMIENTO"
        public void SetInsideFinalSpot(bool inside)
        {
            isInsideFinalSpot = inside;
            if (!inside) stopTimer = 0f; // Reset del cronómetro si el coche se sale
        }

        private void Update()
        {
            // LA CLAVE: Solo evaluamos si el coche está físicamente en el hueco final
            if (isInsideFinalSpot && !isParked)
            {
                if (vehicleSpeed.Value < 0.1f)
                {
                    stopTimer += Time.deltaTime;
                    if (stopTimer >= timeToConfirm)
                    {
                        isParked = true;
                        EjecutarValidacionFinal();
                    }
                }
                else
                {
                    stopTimer = 0f;
                }
            }
        }

        private void EjecutarValidacionFinal()
        {
            // Solo llegamos aquí si el usuario se ha DETENIDO 2 SEGUNDOS en la plaza.
            // Es aquí donde comprobamos si avisó antes.
            if (!hasSignaledInTime)
            {
                if (infractionEvent != null) infractionEvent.Raise(blinkerInfraction);
                Debug.Log("<color=red>DGT:</color> Estacionamiento completado sin señalización previa.");
            }

            // Procesar el éxito de la misión
            if (isFinalGoal) onMissionComplete?.Raise();
            else onManiobraSuccess?.Raise();
        }
    }
}