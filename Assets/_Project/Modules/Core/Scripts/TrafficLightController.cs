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

        [Header("Referencia al Sensor")]
        [Tooltip("Arrastra aquí el objeto hijo que tiene el Box Collider/Trigger")]
        public GameObject detectionZone; 

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

            if (detectionZone != null)
            {
                // Si está en verde, no es un sitio válido para quedarse parado
                // Si está en Rojo o Ámbar, el alumno TIENE que poder parar sin ser multado
                detectionZone.tag = (currentState == LightState.Green) ? "Untagged" : "ValidStopZone";
            }
        }
    }
}