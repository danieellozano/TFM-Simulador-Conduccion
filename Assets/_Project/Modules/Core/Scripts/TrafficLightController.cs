using UnityEngine;
using System.Collections;

namespace Simulador.Core
{
    public enum LightState { Red, Amber, Green }

    public class TrafficLightController : MonoBehaviour
    {
        [Header("Configuración de Tiempos")]
        public float greenTime = 7f;
        public float amberTime = 3f;
        public float redTime = 7f;

        [Header("Estado Actual")]
        public LightState currentState = LightState.Red;
        
        [Header("Referencias Visuales")]
        public GameObject redLight;
        public GameObject amberLight;
        public GameObject greenLight;

        private void Start()
        {
            // Iniciamos el ciclo automático al arrancar el simulador
            StartCoroutine(TrafficCycle());
        }

        private IEnumerator TrafficCycle()
        {
            while (true) // Bucle infinito
            {
                // VERDE
                currentState = LightState.Green;
                UpdateVisuals();
                yield return new WaitForSeconds(greenTime);

                // ÁMBAR
                currentState = LightState.Amber;
                UpdateVisuals();
                yield return new WaitForSeconds(amberTime);

                // ROJO
                currentState = LightState.Red;
                UpdateVisuals();
                yield return new WaitForSeconds(redTime);
            }
        }

        private void UpdateVisuals()
        {
            if (redLight != null) redLight.SetActive(currentState == LightState.Red);
            if (amberLight != null) amberLight.SetActive(currentState == LightState.Amber);
            if (greenLight != null) greenLight.SetActive(currentState == LightState.Green);
        }
    }
}