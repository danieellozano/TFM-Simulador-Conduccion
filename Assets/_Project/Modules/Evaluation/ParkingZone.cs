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

        [Header("Gestión de Tráfico IA")]
        [Tooltip("Cubo invisible en capa AI_Blocker situado detrás del parking")]
        public GameObject aiCourtesyBlocker; 

        [Header("Referencias SOA")]
        public InputDataSO inputData;
        public FloatVariable vehicleSpeed;
        public GameEvent onManiobraSuccess;
        public GameEvent onMissionComplete;
        public GameEvent infractionEvent;
        
        [Header("Reglas DGT")]
        public InfraccionSO blinkerInfraction;
        public InfraccionSO noHandbrakeInfraction;
        public InfraccionSO ruleEliminatoria;

        // ESTADOS LÓGICOS INTERNOS
        private bool hasSignaledInTime = false;
        private bool isInsideFinalSpot = false;
        private bool isManiobraEvaluated = false;
        private bool isVehicleNear = false; // ¿Está el alumno en la zona de influencia?
        private float stopTimer = 0f;

        // --- MÉTODOS LLAMADOS POR LOS HIJOS ---

        public void RegistrarAnticipacion()
        {
            if (isManiobraEvaluated) return;
            isVehicleNear = true; // El alumno ha entrado en la zona de aproximación
            hasSignaledInTime = (inputData.ActiveBlinker == requiredBlinkerSide);
        }

        public void SetInsideFinalSpot(bool inside)
        {
            isInsideFinalSpot = inside;
            isVehicleNear = inside; // Si está dentro de la plaza, también está "cerca"
            if (!inside) stopTimer = 0f; 
        }

        private void Update()
        {
            // 1. GESTIÓN DEL MURO DE CORTESÍA PARA LA IA
            if (aiCourtesyBlocker != null)
            {
                // El muro se activa si el alumno está cerca Y tiene el intermitente puesto
                // Y por supuesto, si la maniobra aún no ha terminado.
                bool shouldBlockAI = isVehicleNear && 
                                     !isManiobraEvaluated && 
                                     inputData.ActiveBlinker == requiredBlinkerSide;

                aiCourtesyBlocker.SetActive(shouldBlockAI);
            }

            // 2. LÓGICA DE EVALUACIÓN DE PARADA (Igual que antes)
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

        public void RegistrarColisionEnZona()
        {
            if (isInsideFinalSpot && !inputData.Handbrake && !isManiobraEvaluated)
            {
                if (infractionEvent != null && ruleEliminatoria != null)
                    infractionEvent.Raise(ruleEliminatoria);
            }
        }

        private void FinalizarMision(bool pusoFrenoMano)
        {
            isManiobraEvaluated = true;

            // Desactivamos el muro de cortesía inmediatamente para dejar pasar el tráfico
            if (aiCourtesyBlocker != null) aiCourtesyBlocker.SetActive(false);

            if (!hasSignaledInTime) infractionEvent?.Raise(blinkerInfraction);
            if (!pusoFrenoMano) infractionEvent?.Raise(noHandbrakeInfraction);

            if (isFinalGoal) onMissionComplete?.Raise();
            else onManiobraSuccess?.Raise();
        }

        // Si el alumno se va sin aparcar, liberamos el tráfico
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isVehicleNear = false;
                isInsideFinalSpot = false;
                if (aiCourtesyBlocker != null) aiCourtesyBlocker.SetActive(false);
            }
        }
    }
}