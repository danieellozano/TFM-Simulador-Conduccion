using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class CurveSpeedEvaluator : MonoBehaviour
    {
        [Header("Referencias SOA")]
        public FloatVariable currentSpeed;    // CurrentSpeed.asset
        public GameEvent infractionEvent;     // OnInfractionDetected.asset

        [Header("Reglas por Gravedad")]
        public InfraccionSO ruleLeve;
        public InfraccionSO ruleDeficiente;
        public InfraccionSO ruleEliminatoria;

        [Header("Umbrales de Velocidad (Km/h)")]
        [Tooltip("Si supera esto es Leve")]
        public float limitLeve = 22f;
        [Tooltip("Si supera esto es Deficiente")]
        public float limitDeficiente = 30f;
        [Tooltip("Si supera esto es Eliminatoria")]
        public float limitEliminatoria = 38f;

        private float maxSpeedInCurve = 0f;
        private bool isPlayerInside = false;

        private void Update()
        {
            if (isPlayerInside)
            {
                // Registramos la velocidad más alta alcanzada en la curva
                if (currentSpeed.Value > maxSpeedInCurve)
                {
                    maxSpeedInCurve = currentSpeed.Value;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInside = true;
                maxSpeedInCurve = 0f; // Reset al entrar
                Debug.Log("<color=white>SISTEMA:</color> Entrando en curva. Monitorizando velocidad...");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInside = false;
                EvaluarVelocidadFinal();
            }
        }

        private void EvaluarVelocidadFinal()
        {
            // Comparamos de mayor a menor gravedad
            if (maxSpeedInCurve >= limitEliminatoria)
            {
                infractionEvent.Raise(ruleEliminatoria);
                Debug.Log($"<color=red>DGT ELIMINATORIA:</color> Velocidad peligrosa en curva ({maxSpeedInCurve} km/h)");
            }
            else if (maxSpeedInCurve >= limitDeficiente)
            {
                infractionEvent.Raise(ruleDeficiente);
                Debug.Log($"<color=orange>DGT DEFICIENTE:</color> Velocidad inadecuada en curva ({maxSpeedInCurve} km/h)");
            }
            else if (maxSpeedInCurve >= limitLeve)
            {
                infractionEvent.Raise(ruleLeve);
                Debug.Log($"<color=yellow>DGT LEVE:</color> Velocidad algo superior a la aconsejable ({maxSpeedInCurve} km/h)");
            }
        }
    }
}