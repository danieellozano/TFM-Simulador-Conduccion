using UnityEngine;
using Simulador.Core;

namespace Simulador.Infrastructure
{
    // Controlador individual de un dispositivo semafórico urbano.
    // Sincroniza la representación lumínica gráfica (Rojo, Ámbar, Verde) con los sistemas físicos del entorno:
    // activa barreras invisibles para la IA de tráfico y conmuta dinámicamente las etiquetas de la zona de detención
    // para alimentar el motor de evaluación de infracciones DGT.
    public class TrafficLightController : MonoBehaviour, ILightSource
    {
        [Header("Estado Actual")]
        [Tooltip("Estado óptico actual del semáforo (Red, Amber, Green).")]
        public LightState currentState = LightState.Red;
        [Tooltip("Propiedad de acceso público para la lectura del estado semafórico.")]
        public LightState CurrentState => currentState;
        
        [Header("Referencias Visuales")]
        [Tooltip("Objeto o luz correspondiente a la fase Roja.")]
        public GameObject redLight;
        [Tooltip("Objeto o luz correspondiente a la fase Ámbar.")]
        public GameObject amberLight;
        [Tooltip("Objeto o luz correspondiente a la fase Verde.")]
        public GameObject greenLight;

        [Header("Interacción Técnica")]
        [Tooltip("El trigger de la IA en capa AI_Blocker.")]
        public GameObject aiStopBarrier; 
        [Tooltip("El trigger del Alumno para detectar infracciones.")]
        public GameObject detectionZone; 

        // Inicializa el semáforo aplicando la representación gráfica correspondiente al estado por defecto.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void Start() => UpdateVisuals();

        // Actualiza el estado lógico del semáforo y dispara la sincronización de elementos visuales y físicos.
        // Parámetros:
        //   - newState: Nuevo estado semafórico (Red, Amber, Green) asignado.
        // Salida: Ninguna.
        public void SetState(LightState newState)
        {
            currentState = newState;
            UpdateVisuals();
        }

        // Alterna la visibilidad de los emisores lumínicos, gestiona la barrera de colisión para la IA
        // y reconfigura las etiquetas (Tags) de detección para auditar paradas innecesarias o saltos de semáforo.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
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