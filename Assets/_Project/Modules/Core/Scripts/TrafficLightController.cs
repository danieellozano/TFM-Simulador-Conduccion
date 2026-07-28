using UnityEngine;

namespace Simulador.Core
{
    // Definición global de estados para que otros scripts lo encuentren
    public enum LightState { Red, Amber, Green }

    public class TrafficLightController : MonoBehaviour
    {
        [Header("Estado Actual (Controlado por Manager)")]
        public LightState currentState = LightState.Red;
        
        [Header("Referencias Visuales")]
        public GameObject redLight;
        public GameObject amberLight;
        public GameObject greenLight;

        private void Start()
        {
            UpdateVisuals();
        }

        public void SetState(LightState newState)
        {
            currentState = newState;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (redLight != null) redLight.SetActive(currentState == LightState.Red);
            if (amberLight != null) amberLight.SetActive(currentState == LightState.Amber);
            if (greenLight != null) greenLight.SetActive(currentState == LightState.Green);
        }
    }
}