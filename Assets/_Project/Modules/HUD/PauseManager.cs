using UnityEngine;
using Simulador.Core;
using TMPro; 

namespace Simulador.HUD
{
    public class PauseManager : MonoBehaviour
    {
        public MissionUI missionUI;
        public GameObject pauseMenuPanel; // En Maniobras, arrastra aquí el Summary_Panel
        [SerializeField] private MonoBehaviour evaluatorObject;
        public IEvaluacionProvider evaluator => evaluatorObject as IEvaluacionProvider;
        public GameObject urbanBriefingPanel; // En Maniobras, arrastra el Briefing_Panel local
        public GameObject inputManager;

        [Header("Título Dinámico (Para Reutilizar el Summary_Panel)")]
        [Tooltip("Opcional: Arrastra aquí el componente de texto del título de resultados (solo en Maniobras).")]
        public TextMeshProUGUI summaryTitleText; 

        private bool isPaused = false;
        private string originalTitle;

        private void Start()
        {
            isPaused = false;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

            // NUEVO: Guardamos el título original (ej: "SESIÓN FINALIZADA") para poder restaurarlo después
            if (summaryTitleText != null)
            {
                originalTitle = summaryTitleText.text;
            }
        }

        public void OnTogglePauseRequested(object data = null)
        {
            // REGLA DE ORO: Si el briefing está puesto, ignoramos la pausa
            if (urbanBriefingPanel != null && urbanBriefingPanel.activeSelf) return;

            // NUEVO: Permitimos pausar tanto en MODO PRÁCTICA como en MODO MANIOBRAS
            bool esModoPausable = evaluator != null && 
                (evaluator.modoActual == ModoDeJuego.PracticaUrbana || evaluator.modoActual == ModoDeJuego.Maniobras);

            if (esModoPausable)
            {
                if (isPaused) Continuar();
                else Pausar();
            }
        }

        public void Pausar()
        {
            isPaused = true;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
            Time.timeScale = 0f;
            if (inputManager != null) inputManager.SetActive(false);

            // NUEVO: Si estamos reutilizando el Summary_Panel, cambiamos su título a "PAUSA"
            if (summaryTitleText != null)
            {
                summaryTitleText.text = "PAUSA";
            }
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Continuar()
        {
            isPaused = false;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            Time.timeScale = 1f; // Reanudamos la física
            
            if (inputManager != null) inputManager.SetActive(true);

            // NUEVO: Restauramos el título original de resultados al reanudar
            if (summaryTitleText != null)
            {
                summaryTitleText.text = originalTitle;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void FinalizarYVerInforme()
        {
            Time.timeScale = 1f;
            isPaused = false;
            
            // 1. Forzamos la actualización de textos ANTES de mostrar el panel
            if (missionUI != null)
            {
                // Esto asegura que MissionUI actualice sus textos internos antes de mostrar el panel
                missionUI.FinalizarSesion(); 
            }

            // 2. Ocultamos el panel de pausa
            if (pauseMenuPanel != null) 
            {
                pauseMenuPanel.SetActive(false);
            }
        }
    }
}