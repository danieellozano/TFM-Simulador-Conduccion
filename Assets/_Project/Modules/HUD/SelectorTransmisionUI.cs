using UnityEngine;
using System;
using Simulador.Core;

namespace Simulador.HUD
{
    // Controlador de la interfaz para la selección del modo de transmisión (Manual o Automática).
    // Permite al usuario configurar el comportamiento del tren motriz antes de iniciar la prueba,
    // actualizando el estado reactivo mediante ScriptableObjects (SOA) y notificando al gestor de la escena.
    public class SelectorTransmisionUI : MonoBehaviour
    {
        [Header("Referencias de UI")]
        [Tooltip("Contenedor visual de la interfaz que alberga los botones de selección.")]
        public GameObject transmissionPanel; 
        
        [Header("Capa de Datos (SOA)")]
        [Tooltip("Variable booleana en ScriptableObject que define si el vehículo opera en modo automático (true) o manual (false).")]
        public BoolVariable isAutomaticSO;    

        // Evento (Callback) al que se suscribirá cualquier Briefing de cualquier escenario para reanudar el inicio del nivel.
        public event Action OnTransmissionSelected;

        // Inicializa el componente ocultando el panel de selección de forma preventiva.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void Awake()
        {
            // Nos aseguramos de que el panel inicie desactivado por defecto
            if (transmissionPanel != null) transmissionPanel.SetActive(false);
        }

        // Despliega la interfaz de usuario en pantalla para permitir la elección del tipo de cambio.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        public void MostrarSelector()
        {
            if (transmissionPanel != null) transmissionPanel.SetActive(true);
        }

        // Configura la transmisión en modo manual e inicie el flujo de finalización de selección.
        // Asignar al evento OnClick() del botón "Manual".
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        public void ElegirManual()
        {
            if (isAutomaticSO != null) isAutomaticSO.Value = false;
            FinalizarSeleccion();
        }

        // Configura la transmisión en modo automático e inicie el flujo de finalización de selección.
        // Asignar al evento OnClick() del botón "Automático".
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        public void ElegirAutomatica()
        {
            if (isAutomaticSO != null) isAutomaticSO.Value = true;
            FinalizarSeleccion();
        }

        // Ocasiona el cierre del panel visual e invoca el evento callback para ceder el control al gestor de la misión.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void FinalizarSeleccion()
        {
            if (transmissionPanel != null) transmissionPanel.SetActive(false);
            
            // Disparamos el evento para avisarle al briefing del escenario correspondiente que ya puede iniciar
            OnTransmissionSelected?.Invoke();
        }
    }
}