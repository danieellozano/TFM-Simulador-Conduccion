using UnityEngine;
using UnityEngine.SceneManagement;

namespace Simulador.HUD
{
    public class BriefingUI : MonoBehaviour
    {
        [Header("Configuración de Inicio")]
        public GameObject briefingPanel; // El Panel de instrucciones
        public GameObject inputManager;  // El objeto Input_Manager de la jerarquía

        private void Awake()
        {
            // 1. Pausamos el simulador
            Time.timeScale = 0f;
            
            // 2. Desactivamos el teclado por completo
            if (inputManager != null) inputManager.SetActive(false);

            // 3. Liberamos el ratón
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void StartMission()
        {
            if (briefingPanel != null)
            {
                // 1. Quitamos el panel
                briefingPanel.SetActive(false);

                // 2. ACTIVAMOS EL TECLADO (Ahora ya puede arrancar el motor)
                if (inputManager != null) inputManager.SetActive(true);

                // 3. Reanudamos el tiempo
                Time.timeScale = 1f;

                // 4. Bloqueamos el ratón para conducir
                Cursor.lockState = CursorLockMode.Locked; 
                Cursor.visible = false;

                Debug.Log("<color=green>SISTEMA:</color> Entrada de datos habilitada.");
            }
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