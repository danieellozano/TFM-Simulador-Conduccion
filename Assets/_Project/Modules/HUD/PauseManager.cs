using UnityEngine;
using Simulador.Core;
using Simulador.Evaluation;

namespace Simulador.HUD
{
    public class PauseManager : MonoBehaviour
    {
        public MissionUI missionUI;
        public GameObject pauseMenuPanel;
        public DGTEvaluator evaluator;
        public GameObject urbanBriefingPanel; // <--- NUEVA REFERENCIA
        public GameObject inputManager;

        private bool isPaused = false;

        private void Start()
        {
            // Aseguramos que el script empiece en estado limpio
            isPaused = false;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        }

        public void OnTogglePauseRequested(object data = null)
        {
            // REGLA DE ORO: Si el briefing está puesto, ignoramos la pausa
            if (urbanBriefingPanel != null && urbanBriefingPanel.activeSelf) return;

            // Solo permitimos pausa en MODO PRÁCTICA
            if (evaluator != null && evaluator.modoActual == ModoDeJuego.PracticaUrbana)
            {
                if (isPaused) Continuar();
                else Pausar();
            }
        }

        public void Pausar()
        {
            isPaused = true;
            pauseMenuPanel.SetActive(true);
            Time.timeScale = 0f;
            if (inputManager != null) inputManager.SetActive(false);
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Continuar()
        {
            isPaused = false;
            pauseMenuPanel.SetActive(false);
            Time.timeScale = 1f; // Reanudamos la física
            
            // Reanudamos el teclado
            if (inputManager != null) inputManager.SetActive(true);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void FinalizarYVerInforme()
        {
            Time.timeScale = 1f; 
            isPaused = false;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            missionUI.FinalizarSesion();
        }
    }
}