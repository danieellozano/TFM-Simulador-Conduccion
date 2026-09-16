using UnityEngine;
using UnityEngine.SceneManagement;
using Simulador.Core;     

namespace Simulador.HUD
{
    // Controlador de interfaz encargado de gestionar la fase de preparación previa (Briefing) al inicio del ejercicio.
    // Garantiza un arranque determinista congelando la escala temporal del motor de físicas, ocultando o mostrando
    // los contenedores visuales modales correspondientes, gestionando el bloqueo/liberación del cursor del ratón,
    // y coordinando la selección de transmisión (manual o automática) antes de transferir el control al conductor.
    public class BriefingUI : MonoBehaviour
    {
        [Header("Configuración de Inicio")]
        [Tooltip("Contenedor visual del panel de instrucciones pedagógicas y objetivos de la prueba.")]
        public GameObject briefingPanel;

        [Tooltip("Objeto orquestador de lectura de periféricos físicos (Input_Manager) a habilitar tras la confirmación.")]
        public GameObject inputManager;

        [Header("Selector de Transmisión Reutilizable")]
        [Tooltip("Referencia al componente de selección de modalidad de cambio (Manual o Automática).")]
        public SelectorTransmisionUI transmissionSelector;

        [Header("Evaluación DGT")]
        [Tooltip("Componente evaluador asignado en el Inspector que implementa la interfaz IEvaluacionProvider.")]
        [SerializeField] private MonoBehaviour dgtEvaluator;

        // Propiedad pública que expone el acceso desacoplado al evaluador mediante su contrato de interfaz
        public IEvaluacionProvider evaluator => dgtEvaluator as IEvaluacionProvider;

        // Inicialización temprana del ciclo de vida.
        // Establece el estado de pausa inicial de seguridad, libera el cursor para la interacción en menús
        // y configura el contexto evaluativo inicial correspondiente a la escena de maniobras.
        // Parámetros:
        //   Ninguno.
        // Salida:
        //   No devuelve ningún valor (void).
        private void Awake()
        {
            // Congela el tiempo físico de la simulación para impedir el avance del vehículo durante la lectura
            Time.timeScale = 0f;

            // Desactiva la lectura de mandos para evitar que pulsaciones accidentales muevan el coche
            if (inputManager != null) inputManager.SetActive(false);

            // Desbloquea y visibiliza el cursor del ratón para permitir la navegación por la interfaz
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Establece el modo de juego en el evaluador como circuito de maniobras
            if (evaluator != null)
            {
                evaluator.modoActual = ModoDeJuego.Maniobras;
            }
        }

        // Suscripción al delegado de eventos del selector de transmisión al arrancar la escena.
        // Parámetros:
        //   Ninguno.
        // Salida:
        //   No devuelve ningún valor (void).
        private void Start()
        {
            if (transmissionSelector != null)
            {
                // Se suscribe al callback emitido cuando el alumno elige entre modo manual o automático
                transmissionSelector.OnTransmissionSelected += AlConfirmarTransmision;
            }
        }

        // Desuscripción simétrica del delegado para prevenir fugas de memoria o suscriptores latentes al descargar la escena.
        // Parámetros:
        //   Ninguno.
        // Salida:
        //   No devuelve ningún valor (void).
        private void OnDestroy()
        {
            if (transmissionSelector != null)
            {
                transmissionSelector.OnTransmissionSelected -= AlConfirmarTransmision;
            }
        }

        // Método invocado por el evento OnClick() del botón 'Comenzar' en el panel de instrucciones.
        // Oculta el texto del briefing y despliega la selección de transmisión si está presente.
        // Parámetros:
        //   Ninguno.
        // Salida:
        //   No devuelve ningún valor (void).
        public void StartMission()
        {
            // Oculta el contenedor visual de texto explicativo
            if (briefingPanel != null) briefingPanel.SetActive(false);

            // Si se dispone del selector de transmisión, se abre para que el alumno decida el modo
            if (transmissionSelector != null)
            {
                transmissionSelector.MostrarSelector();
            }
            else
            {
                // Si no hay selector configurado, inicia directamente la simulación física
                ComenzarMisionFisicamente();
            }
        }

        // Manejador del callback invocado tras confirmarse la modalidad de transmisión seleccionada.
        // Parámetros:
        //   Ninguno.
        // Salida:
        //   No devuelve ningún valor (void).
        private void AlConfirmarTransmision()
        {
            ComenzarMisionFisicamente();
        }

        // Restaura el flujo temporal de la simulación, bloquea el puntero del ratón e inicializa el satélite de entrada.
        // Parámetros:
        //   Ninguno.
        // Salida:
        //   No devuelve ningún valor (void).
        private void ComenzarMisionFisicamente()
        {
            // Habilita el satélite InputManager para comenzar a recibir y transmitir las órdenes del alumno
            if (inputManager != null) inputManager.SetActive(true);

            // Restaura la escala temporal del motor a velocidad normal (1.0)
            Time.timeScale = 1f;

            // Oculta y bloquea el cursor en el centro de la pantalla para evitar distracciones en cabina
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("<color=green>SISTEMA:</color> Entrada de datos habilitada.");
        }
    }
}