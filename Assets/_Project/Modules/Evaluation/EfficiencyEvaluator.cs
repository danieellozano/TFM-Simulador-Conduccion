using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class EfficiencyEvaluator : MonoBehaviour
    {
        public FloatVariable currentSpeed;
        public GameEvent infractionEvent;
        public InfraccionSO efficiencyRule; // Asset CON-EFIC

        private float lastSpeed;
        public float accelerationThreshold = 15f; // Umbral de "pisotón"

        void Update()
        {
            if (currentSpeed == null) return;

            // Calculamos la aceleración: (V_actual - V_anterior) / tiempo
            float currentAcceleration = (currentSpeed.Value - lastSpeed) / Time.deltaTime;

            if (currentAcceleration > accelerationThreshold)
            {
                infractionEvent.Raise(efficiencyRule);
                Debug.Log("<color=yellow>DGT:</color> Conducción ineficiente (Aceleración brusca).");
            }
            lastSpeed = currentSpeed.Value;
        }
    }
}