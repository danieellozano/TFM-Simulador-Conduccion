using UnityEngine;
using Simulador.Core;

namespace Simulador.InputModule
{
    // Módulo de lectura de hardware e inyección de datos para la Capa de Abstracción (HAL).
    // Captura los comandos analógicos y discretos procedentes del periférico (volante, pedales o teclado)
    // a través del Input System de Unity y los escribe en un ScriptableObject centralizado (InputDataSO)
    // para desacoplar la simulación de físicas y los sistemas de interfaz del dispositivo físico.
    public class InputReader : MonoBehaviour
    {
        [Tooltip("Contrato y buffer de datos normalizados para la comunicación desacoplada con el Core.")]
        public InputDataSO inputData;
        [Tooltip("Evento del bus SOA que se emite para solicitar el reinicio de la posición/estado del vehículo.")]
        public GameEvent restartEvent;
        // Instancia generada automáticamente por el Input System con el mapa de acciones de entrada.
        private SimuladorInput controls;

        [Tooltip("Evento del bus SOA que solicita la conmutación del estado de pausa del juego.")]
        public GameEvent pauseEvent;
        [Tooltip("Variable booleana en ScriptableObject que define si la transmisión seleccionada es automática.")]
        public BoolVariable isAutomaticSO; // Canal para saber si la transmisión es automática

        // Inicializa el mapa de acciones de entrada y configura las llamadas por eventos (callbacks) para cada comando.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void Awake()
        {
            controls = new SimuladorInput();

            // INTERMITENTES
            controls.Driving.Blinkers.started += ctx =>
            {
                float val = ctx.ReadValue<float>(); 
                if (val < -0.5f)
                    inputData.ActiveBlinker = (inputData.ActiveBlinker == -1) ? 0 : -1; 
                else if (val > 0.5f)            
                    inputData.ActiveBlinker = (inputData.ActiveBlinker == 1) ? 0 : 1;   
            };

            // CAMBIO DE MARCHAS (Manual / Automático con bypass de embrague)
            controls.Driving.GearUp.started += ctx => {
                bool esAutomatico = isAutomaticSO != null && isAutomaticSO.Value;

                if (esAutomatico)
                {
                    // En automático no hace falta embrague. Pasamos de R (-1) a N (0) o de N (0) a D (1)
                    if (inputData.CurrentGear == -1) inputData.CurrentGear = 0;
                    else if (inputData.CurrentGear == 0) inputData.CurrentGear = 1;
                }
                else
                {
                    // En manual requiere obligatoriamente embrague > 70%
                    if (inputData.Clutch > 0.7f) inputData.CurrentGear = Mathf.Clamp(inputData.CurrentGear + 1, -1, 5);
                }
            };

            controls.Driving.GearDown.started += ctx => {
                bool esAutomatico = isAutomaticSO != null && isAutomaticSO.Value;

                if (esAutomatico)
                {
                    // En automático no hace falta embrague. Pasamos de cualquier marcha Drive (1-5) a N (0) o de N (0) a R (-1)
                    if (inputData.CurrentGear > 0) inputData.CurrentGear = 0;
                    else if (inputData.CurrentGear == 0) inputData.CurrentGear = -1;
                }
                else
                {
                    // En manual requiere obligatoriamente embrague > 70%
                    if (inputData.Clutch > 0.7f) inputData.CurrentGear = Mathf.Clamp(inputData.CurrentGear - 1, -1, 5);
                }
            };

            // REINICIO DE MOTOR (Tecla R)
            controls.Driving.Restart.performed += ctx => {
                Debug.Log("<color=yellow>InputReader:</color> Tecla R pulsada");
                if(restartEvent != null) restartEvent.Raise();
            };

            controls.Driving.Handbrake.performed += ctx => {
                if(inputData != null) {
                    inputData.Handbrake = !inputData.Handbrake; // Funciona como un interruptor (Toggle)
                    Debug.Log("Freno de mano: " + (inputData.Handbrake ? "PUESTO" : "QUITADO"));
                }
            };

            controls.Driving.Pause.performed += ctx => {
                if(pauseEvent != null) pauseEvent.Raise();
            };
        }

        // Habilita la escucha activa del mapa de entradas de la simulación al activar el componente.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void OnEnable() => controls?.Enable();

        // Deshabilita la escucha del mapa de entradas al desactivar el componente para prevenir filtraciones de control.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void OnDisable() => controls?.Disable();

        // Muestrea frame a frame las magnitudes analógicas de pedales, volante y ejes de cámara inyectándolas en la HAL.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void Update()
        {
            if (inputData == null || controls == null) return;

            inputData.Throttle = controls.Driving.Throttle.ReadValue<float>();
            inputData.Breaking = controls.Driving.Breaking.ReadValue<float>();
            inputData.Clutch = controls.Driving.Clutch.ReadValue<float>();
            inputData.Steering = controls.Driving.Steering.ReadValue<float>(); 

            // --- RESTAURADOS CONTROLES DE CÁMARA (SOA) ---
            inputData.Look = controls.Driving.Look.ReadValue<float>();
            inputData.LookReset = controls.Driving.LookReset.triggered; 
        }
    }
}
