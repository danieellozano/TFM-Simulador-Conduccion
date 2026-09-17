using UnityEngine;
using Simulador.Core;
using TMPro; 

namespace Simulador.HUD
{
    // Gestor de pausa del juego y de la suspensión temporal de la simulación.
    // Interrumpe la escala temporal física (Time.timeScale), inhabilita el procesamiento de entradas 
    // y gestiona la visualización del menú de pausa o informe final según el modo de juego activo.
    public class PauseManager : MonoBehaviour
    {
        [Tooltip("Referencia al controlador principal de interfaz de misiones para solicitar el cierre o resumen.")]
        public MissionUI missionUI;
        [Tooltip("Panel contenedor de la interfaz de pausa o panel de resumen reutilizado.")]
        public GameObject pauseMenuPanel; 
        
        [SerializeField] private MonoBehaviour evaluatorObject;
        [Tooltip("Proveedor de la interfaz de evaluación DGT para verificar las reglas y modos de juego.")]
        public IEvaluacionProvider evaluator => evaluatorObject as IEvaluacionProvider;
        
        [Tooltip("Panel de instrucciones de la misión utilizado para bloquear la pausa durante el inicio.")]
        public GameObject urbanBriefingPanel; 
        [Tooltip("Gestor de entrada de hardware para inhabilitar el control durante la pausa.")]
        public GameObject inputManager;
        public GameObject summaryPanel;

        [Header("Título Dinámico (Para Reutilizar el Summary_Panel)")]
        [Tooltip("Opcional: Arrastra aquí el componente de texto del título de resultados (solo en Maniobras).")]
        public TextMeshProUGUI summaryTitleText; 

        // Estado interno de congelación/pausa de la simulación.
        private bool isPaused = false;
        // Registro del texto del título original para su posterior restauración.
        private string originalTitle;

        // Inicializa los estados de la simulación y almacena la configuración de texto por defecto.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
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

        // Procesa la solicitud de conmutación de estado (Pausar/Reanudar) enviada por el gestor de eventos o la entrada de usuario.
        // Parámetros:
        //   - data: Parámetro opcional de evento.
        // Salida: Ninguna.
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

        // Suspende la física de la simulación, deshabilita la entrada del usuario y despliega la interfaz de pausa.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        public void Pausar()
        {
            isPaused = true;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
            Time.timeScale = 0f;
            if (inputManager != null) inputManager.SetActive(false);

            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Reanuda la simulación física, reactiva el procesador de entradas y restablece los elementos visuales del juego.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
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

        // Restablece la velocidad del tiempo físico y solicita a la interfaz de misiones la generación del informe de evaluación.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
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
        
        // Finaliza la sesión del escenarios de maniobras.
        public void FinalizarManiobras()
        {
            Time.timeScale = 1f;
            isPaused = false;

            if (pauseMenuPanel != null) 
            {
                pauseMenuPanel.SetActive(false);
            }

            if (summaryPanel != null) summaryPanel.SetActive(true);


        }
    }
}