using UnityEngine;
using UnityEngine.SceneManagement;

namespace Simulador.HUD
{
    public class BriefingUI : MonoBehaviour
    {
        [Header("Configuración de Inicio")]
        public GameObject briefingPanel; // El Panel de instrucciones
        public GameObject inputManager;  // El objeto Input_Manager de la jerarquía

        [Header("Selector de Transmisión Reutilizable")]
        public SelectorTransmisionUI transmissionSelector; // <--- NUEVO: Arrastra el script del panel

        private void Awake()
        {
            Time.timeScale = 0f;
            if (inputManager != null) inputManager.SetActive(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Start()
        {
            // NUEVO: Nos suscribimos al evento del selector independiente
            if (transmissionSelector != null)
            {
                transmissionSelector.OnTransmissionSelected += AlConfirmarTransmision;
            }
        }

        private void OnDestroy()
        {
            // NUEVO: Nos desuscribimos por seguridad al destruir el objeto
            if (transmissionSelector != null)
            {
                transmissionSelector.OnTransmissionSelected -= AlConfirmarTransmision;
            }
        }

        public void StartMission()
        {
            if (briefingPanel != null) briefingPanel.SetActive(false);

            // NUEVO: En lugar de iniciar el juego inmediatamente, llamamos al selector de transmisión
            if (transmissionSelector != null)
            {
                transmissionSelector.MostrarSelector();
            }
            else
            {
                // Fallback si olvidas asignar el selector en alguna escena
                ComenzarMisionFisicamente();
            }
        }

        private void AlConfirmarTransmision()
        {
            // Se ejecuta de forma automática tras pulsar "Manual" o "Automático"
            ComenzarMisionFisicamente();
        }

        private void ComenzarMisionFisicamente()
        {
            if (inputManager != null) inputManager.SetActive(true);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked; 
            Cursor.visible = false;
            Debug.Log("<color=green>SISTEMA:</color> Entrada de datos habilitada.");
        }

        public void BotonSalir()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false; 
            #else
                Application.Quit(); 
            #endif
        }
    }
}