using UnityEngine;

namespace Simulador.Core
{
    public enum LightState { Red, Amber, Green }

    public class TrafficLightController : MonoBehaviour
    {
        [Header("Estado Actual")]
        public LightState currentState = LightState.Red;
        
        [Header("Referencias Visuales")]
        public GameObject redLight;
        public GameObject amberLight;
        public GameObject greenLight;

        [Header("Interacción Técnica")]
        [Tooltip("El trigger de la IA en capa AI_Blocker")]
        public GameObject aiStopBarrier; 
        [Tooltip("El trigger del Alumno para detectar infracciones")]
        public GameObject detectionZone; 

        private void Start() => UpdateVisuals();

        public void SetState(LightState newState)
        {
            currentState = newState;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // 1. Lógica Visual
            if (redLight) redLight.SetActive(currentState == LightState.Red);
            if (amberLight) amberLight.SetActive(currentState == LightState.Amber);
            if (greenLight) greenLight.SetActive(currentState == LightState.Green);

            // 2. Lógica para la IA: El muro aparece en Rojo y Ámbar
            if (aiStopBarrier) 
                aiStopBarrier.SetActive(currentState != LightState.Green);

            // 3. Lógica de Parada Innecesaria (Tag Dinámico)
            if (detectionZone != null)
                detectionZone.tag = (currentState == LightState.Green) ? "Untagged" : "ValidStopZone";
        }
    }
}