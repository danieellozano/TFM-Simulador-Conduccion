// using UnityEngine;
// using Simulador.Core;
// using Simulador.Evaluation;

// namespace Simulador.HUD
// {
//     public class PauseManager : MonoBehaviour
//     {
//         public MissionUI missionUI;
//         public GameObject pauseMenuPanel;
//         public DGTEvaluator evaluator;
//         public GameObject urbanBriefingPanel; 
//         public GameObject inputManager;

//         private bool isPaused = false;

//         private void Start()
//         {
//             // Aseguramos que el script empiece en estado limpio
//             isPaused = false;
//             if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
//         }

//         public void OnTogglePauseRequested(object data = null)
//         {
//             // REGLA DE ORO: Si el briefing está puesto, ignoramos la pausa
//             if (urbanBriefingPanel != null && urbanBriefingPanel.activeSelf) return;

//             // Solo permitimos pausa en MODO PRÁCTICA
//             if (evaluator != null && evaluator.modoActual == ModoDeJuego.PracticaUrbana)
//             {
//                 if (isPaused) Continuar();
//                 else Pausar();
//             }
//         }

//         public void Pausar()
//         {
//             isPaused = true;
//             pauseMenuPanel.SetActive(true);
//             Time.timeScale = 0f;
//             if (inputManager != null) inputManager.SetActive(false);
            
//             Cursor.lockState = CursorLockMode.None;
//             Cursor.visible = true;
//         }

//         public void Continuar()
//         {
//             isPaused = false;
//             pauseMenuPanel.SetActive(false);
//             Time.timeScale = 1f; // Reanudamos la física
            
//             // Reanudamos el teclado
//             if (inputManager != null) inputManager.SetActive(true);

//             Cursor.lockState = CursorLockMode.Locked;
//             Cursor.visible = false;
//         }

//         public void FinalizarYVerInforme()
//         {
//             Time.timeScale = 1f; 
//             isPaused = false;
//             if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
//             missionUI.FinalizarSesion();
//         }
//     }
// }


using UnityEngine;
using Simulador.Core;
using Simulador.Evaluation;
using TMPro; // <--- NUEVO: Namespace para TextMeshPro

namespace Simulador.HUD
{
    public class PauseManager : MonoBehaviour
    {
        public MissionUI missionUI;
        public GameObject pauseMenuPanel; // En Maniobras, arrastra aquí el Summary_Panel
        public DGTEvaluator evaluator;
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
            // 1. Reanudamos temporalmente el tiempo flotante para permitir transiciones de interfaz
            Time.timeScale = 1f; 
            isPaused = false;

            // 2. Restauramos el título original de resultados (ej: "SESIÓN FINALIZADA")
            // Esto asegura que al abrirse de nuevo el panel muestre su texto original "tal cual"
            if (summaryTitleText != null)
            {
                summaryTitleText.text = originalTitle;
            }

            // 3. Ocultamos el panel de pausa (que es el Summary_Panel en modo pausa)
            if (pauseMenuPanel != null) 
            {
                pauseMenuPanel.SetActive(false);
            }

            // 4. Llamamos a la finalización oficial de la sesión para que MissionUI lo abra 
            // de forma limpia como pantalla de resultados definitiva de la práctica
            if (missionUI != null)
            {
                missionUI.FinalizarSesion();
            }
        }
    }
}