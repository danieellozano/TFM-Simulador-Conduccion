using UnityEngine;
using Simulador.Core;

namespace Simulador.InputModule
{
    public class InputReader : MonoBehaviour
    {
        public InputDataSO inputData;
        public GameEvent restartEvent;
        private SimuladorInput controls;

        public GameEvent pauseEvent;
        public BoolVariable isAutomaticSO; // Canal para saber si la transmisión es automática

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

        private void OnEnable() => controls?.Enable();
        private void OnDisable() => controls?.Disable();

        private void Update()
        {
            if (inputData == null || controls == null) return;

            inputData.Throttle = controls.Driving.Throttle.ReadValue<float>();
            inputData.Breaking = controls.Driving.Breaking.ReadValue<float>();
            inputData.Clutch = controls.Driving.Clutch.ReadValue<float>();
            inputData.Steering = controls.Driving.Steering.ReadValue<float>(); 

            // --- RESTAURADOS CONTROLES DE CÁMARA (SOA) ---
            // Estas dos líneas corrigen el problema de la cámara inmóvil:
            inputData.Look = controls.Driving.Look.ReadValue<float>();
            inputData.LookReset = controls.Driving.LookReset.triggered; 
        }
    }
}