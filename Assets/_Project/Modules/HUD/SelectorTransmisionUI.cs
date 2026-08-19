using UnityEngine;
using System;
using Simulador.Core;

namespace Simulador.HUD
{
    public class SelectorTransmisionUI : MonoBehaviour
    {
        [Header("Referencias de UI")]
        public GameObject transmissionPanel; 
        [Header("Capa de Datos (SOA)")]
        public BoolVariable isAutomaticSO;    

        // Evento (Callback) al que se suscribirá cualquier Briefing de cualquier escenario
        public event Action OnTransmissionSelected;

        private void Awake()
        {
            // Nos aseguramos de que el panel inicie desactivado por defecto
            if (transmissionPanel != null) transmissionPanel.SetActive(false);
        }

        public void MostrarSelector()
        {
            if (transmissionPanel != null) transmissionPanel.SetActive(true);
        }

        // Asignar al OnClick() del botón "Manual"
        public void ElegirManual()
        {
            if (isAutomaticSO != null) isAutomaticSO.Value = false;
            FinalizarSeleccion();
        }

        // Asignar al OnClick() del botón "Automático"
        public void ElegirAutomatica()
        {
            if (isAutomaticSO != null) isAutomaticSO.Value = true;
            FinalizarSeleccion();
        }

        private void FinalizarSeleccion()
        {
            if (transmissionPanel != null) transmissionPanel.SetActive(false);
            
            // Disparamos el evento para avisarle al briefing del escenario correspondiente que ya puede iniciar
            OnTransmissionSelected?.Invoke();
        }
    }
}