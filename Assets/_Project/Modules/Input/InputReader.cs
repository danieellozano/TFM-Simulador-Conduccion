using UnityEngine;
using Simulador.Core;

namespace Simulador.InputModule
{
    public class InputReader : MonoBehaviour
    {
        public InputDataSO inputData;
        public GameEvent restartEvent;
        private SimuladorInput controls;

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

            // CAMBIO DE MARCHAS (Solo con embrague pisado > 70%)
            controls.Driving.GearUp.started += ctx => {
                if (inputData.Clutch > 0.7f) inputData.CurrentGear = Mathf.Clamp(inputData.CurrentGear + 1, -1, 5);
            };
            controls.Driving.GearDown.started += ctx => {
                if (inputData.Clutch > 0.7f) inputData.CurrentGear = Mathf.Clamp(inputData.CurrentGear - 1, -1, 5);
            };

            // REINICIO DE MOTOR (Tecla R)
            controls.Driving.Restart.performed += ctx => {
                Debug.Log("<color=yellow>InputReader:</color> Tecla R pulsada");
                if(restartEvent != null) restartEvent.Raise();
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

            // --- CORRECCIÓN DE GIRO ---
            // Si al pulsar A gira a la derecha, QUITA el signo menos de abajo.
            // Si al pulsar A gira a la izquierda (pero el coche va al revés), PON el signo menos.
            inputData.Steering = controls.Driving.Steering.ReadValue<float>(); 
        }
    }
}