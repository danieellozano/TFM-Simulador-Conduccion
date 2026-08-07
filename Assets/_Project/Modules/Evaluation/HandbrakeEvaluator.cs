using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class HandbrakeEvaluator : MonoBehaviour
    {
        public InputDataSO inputData;
        public FloatVariable currentSpeed;
        public GameEvent infractionEvent;

        [Header("Reglas DGT")]
        public InfraccionSO ruleLeve;        // CON-MAN-L
        public InfraccionSO ruleDeficiente;  // CON-MAN-D
        public InfraccionSO ruleEliminatoria;// CON-MAN-E

        public float timeForDeficiente = 5.0f; 
        private float handbrakeTimer = 0f;
        private bool deficienteReported = false;
        private bool wasHandbrakeActive = false;

        private void Update()
        {
            if (inputData == null || currentSpeed == null) return;

            // CASO 1: ELIMINATORIA (Activar freno de mano a alta velocidad)
            if (inputData.Handbrake && !wasHandbrakeActive && currentSpeed.Value > 20f)
            {
                infractionEvent.Raise(ruleEliminatoria);
                Debug.Log("<color=red>DGT ELIMINATORIA:</color> Freno de mano en marcha.");
            }

            // CASO 2: LEVE O DEFICIENTE (Intentar salir con el freno puesto)
            if (inputData.Handbrake && inputData.Throttle > 0.2f)
            {
                handbrakeTimer += Time.deltaTime;

                // Si llega a 5 segundos acelerando -> DEFICIENTE (salta al momento)
                if (handbrakeTimer >= timeForDeficiente && !deficienteReported)
                {
                    infractionEvent.Raise(ruleDeficiente);
                    deficienteReported = true; 
                    Debug.Log("<color=orange>DGT DEFICIENTE:</color> Circulación prolongada con freno de mano.");
                }
            }
            else
            {
                // Si el alumno suelta el freno ANTES de los 5 segundos -> LEVE
                if (wasHandbrakeActive && !inputData.Handbrake && handbrakeTimer > 0.5f && !deficienteReported)
                {
                    infractionEvent.Raise(ruleLeve);
                    Debug.Log("<color=yellow>DGT LEVE:</color> Inicio de marcha con freno de mano (rectificado).");
                }

                // Reset al soltar acelerador o freno
                handbrakeTimer = 0f;
                deficienteReported = false;
            }

            wasHandbrakeActive = inputData.Handbrake;
        }
    }
}