using UnityEngine;
using Simulador.Core;
using UnityEngine.SceneManagement;

namespace Simulador.HUD
{
    // Controlador de la interfaz inicial (Briefing) para entornos de simulación urbana.
    // Gestiona la selección de modos de juego (Examen Urbano o Práctica Libre), carga los perfiles
    // de misión correspondientes y coordina la transición hacia el selector de transmisión y el gestor de misiones.
    public class UrbanBriefingUI : MonoBehaviour
    {
        [Header("Referencias de UI")]
        [Tooltip("Contenedor del panel inicial de briefing e instrucciones del entorno urbano.")]
        public GameObject urbanBriefingPanel;
        [Tooltip("Referencia al selector de tipo de transmisión (Manual / Automática).")]
        public SelectorTransmisionUI transmissionSelector;

        [Header("Referencias de Sistemas")]
        [Tooltip("Gestor del flujo y progresión de tareas en la misión urbana.")]
        public UrbanMissionManager urbanManager;
        [Tooltip("Gestor de entradas para inhabilitar o habilitar la lectura de periféricos.")]
        public GameObject inputManager;

        [Header("Perfiles de Misión")]
        [Tooltip("Perfil de misión estructurado para la evaluación formal de Examen Urbano.")]
        public UrbanMissionProfileSO perfilExamen;
        [Tooltip("Perfil de misión configurado para la modalidad de Práctica Libre.")]
        public UrbanMissionProfileSO perfilLibre;
        
        [SerializeField] private MonoBehaviour dgtEvaluator; // Solo para el Inspector
        [Tooltip("Proveedor de la interfaz de evaluación DGT para la gestión del modo de juego.")]
        public IEvaluacionProvider evaluator => dgtEvaluator as IEvaluacionProvider; // Acceso limpio
        
        // Perfil de misión seleccionado activamente para dar comienzo a la simulación.
        private UrbanMissionProfileSO perfilSeleccionado;

        // Inicia el estado congelando el tiempo físico, ocultando el cursor y desactivando las entradas de hardware.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void Awake()
        {
            Time.timeScale = 0f;
            if (inputManager != null) inputManager.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Suscribe el gestor al evento de confirmación de transmisión.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void Start()
        {
            if (transmissionSelector != null)
            {
                transmissionSelector.OnTransmissionSelected += AlConfirmarTransmision;
            }
        }

        // Desuscribe los eventos de la interfaz para prevenir fugas de memoria o llamadas huérfanas.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void OnDestroy()
        {
            if (transmissionSelector != null)
            {
                transmissionSelector.OnTransmissionSelected -= AlConfirmarTransmision;
            }
        }

        // Configura el evaluador en modo Examen Urbano y solicita la preparación del perfil oficial.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        public void SeleccionarExamen() 
        { 
            if (evaluator != null) evaluator.modoActual = ModoDeJuego.ExamenUrbano;
            PrepararInicio(perfilExamen); 
        }

        // Configura el evaluador en modo Práctica Urbana y solicita la preparación del perfil de entrenamiento.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        public void SeleccionarLibre() 
        { 
            if (evaluator != null) evaluator.modoActual = ModoDeJuego.PracticaUrbana;
            PrepararInicio(perfilLibre); 
        }

        // Registra el perfil elegido, oculta el panel de briefing y despliega el selector de transmisión si está asignado.
        // Parámetros:
        //   - perfil: Objeto ScriptableObject con las tareas y objetivos de la prueba urbana.
        // Salida: Ninguna.
        private void PrepararInicio(UrbanMissionProfileSO perfil)
        {
            perfilSeleccionado = perfil;
            if (urbanBriefingPanel != null) urbanBriefingPanel.SetActive(false);

            if (transmissionSelector != null)
            {
                transmissionSelector.MostrarSelector();
            }
            else
            {
                Empezar(perfilSeleccionado);
            }
        }

        // Método callback que responde a la confirmación de la transmisión para continuar con el arranque de la misión.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void AlConfirmarTransmision()
        {
            Empezar(perfilSeleccionado);
        }

        // Reanuda la escala temporal física, bloquea el cursor en la pantalla, habilita entradas e inicia la secuencia en el gestor de misiones.
        // Parámetros:
        //   - perfil: Objeto ScriptableObject con las tareas configuradas para la sesión active.
        // Salida: Ninguna.
        private void Empezar(UrbanMissionProfileSO perfil)
        {
            if (urbanManager == null) { Debug.LogError("Falta asignar el Urban_Manager en el HUD_Manager"); return; }
            if (inputManager != null) inputManager.SetActive(true);
            
            Time.timeScale = 1f; 
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            urbanManager.IniciarExamen(perfil); 
        }

        // Reanuda la escala de tiempo y redirige la ejecución al menú principal del simulador.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        public void VolverAlMenu()
        {
            Time.timeScale = 1f; 
            SceneManager.LoadScene("0_Menu_Principal"); 
        }
    }
}