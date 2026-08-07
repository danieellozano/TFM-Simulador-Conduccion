using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class ParkingZone : MonoBehaviour
    {
        [Header("Configuración de Maniobra")]
        public bool isFinalGoal = false;
        public int requiredBlinkerSide; // -1 Izq, 1 Der
        public float waitTimeBeforePenalty = 5.0f;

        [Header("Referencias SOA")]
        public InputDataSO inputData;
        public FloatVariable vehicleSpeed;
        public GameEvent onManiobraSuccess;
        public GameEvent onMissionComplete;
        public GameEvent infractionEvent;
        
        [Header("Reglas DGT")]
        public InfraccionSO blinkerInfraction;
        public InfraccionSO noHandbrakeInfraction;
        public InfraccionSO ruleEliminatoria; // <--- ESTO FALTABA

        private bool hasSignaledInTime = false;
        private bool isInsideFinalSpot = false;
        private bool isManiobraEvaluated = false;
        private float stopTimer = 0f;

        public void RegistrarAnticipacion()
        {
            if (isManiobraEvaluated) return;
            hasSignaledInTime = (inputData.ActiveBlinker == requiredBlinkerSide);
        }

        public void SetInsideFinalSpot(bool inside)
        {
            isInsideFinalSpot = inside;
            if (!inside) stopTimer = 0f; 
        }

        private void Update()
        {
            if (!isInsideFinalSpot || isManiobraEvaluated) return;

            if (vehicleSpeed.Value < 0.1f)
            {
                stopTimer += Time.deltaTime;
                if (inputData.Handbrake)
                {
                    FinalizarMision(true);
                }
                else if (stopTimer >= waitTimeBeforePenalty)
                {
                    FinalizarMision(false);
                }
            }
            else { stopTimer = 0f; }
        }

        // Este es el método que llama el CollisionEvaluator
        public void RegistrarColisionEnZona()
        {
            if (isInsideFinalSpot && !inputData.Handbrake && !isManiobraEvaluated)
            {
                if (infractionEvent != null && ruleEliminatoria != null)
                {
                    infractionEvent.Raise(ruleEliminatoria);
                    Debug.Log("<color=red>DGT ELIMINATORIA:</color> Colisión por falta de inmovilización.");
                }
            }
        }

        private void FinalizarMision(bool pusoFrenoMano)
        {
            isManiobraEvaluated = true;
            if (!hasSignaledInTime) infractionEvent?.Raise(blinkerInfraction);
            if (!pusoFrenoMano) infractionEvent?.Raise(noHandbrakeInfraction);

            if (isFinalGoal) onMissionComplete?.Raise();
            else onManiobraSuccess?.Raise();
        }
    }
}