using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class SpeedEvaluator : MonoBehaviour
    {
        public FloatVariable currentSpeedSO;      // CurrentSpeed.asset
        public FloatVariable currentSpeedLimitSO; // CurrentSpeedLimit.asset
        public GameEvent infractionEvent;         // OnInfractionDetected.asset
        public InfraccionSO speedInfraction;      // Arrastra VEL_GEN

        private float timerExceso = 0f;

        private void Update()
        {
            if (currentSpeedSO == null || currentSpeedLimitSO == null) return;

            // Si el alumno supera el límite actual + un margen de seguridad de 3km/h
            if (currentSpeedSO.Value > currentSpeedLimitSO.Value + 3f)
            {
                timerExceso += Time.deltaTime;

                // Si mantiene el exceso más de 2 segundos, lanzamos la multa
                if (timerExceso > 2f)
                {
                    infractionEvent.Raise(speedInfraction);
                    timerExceso = -3f; // Pausa de 3 seg para no saturar con 50 multas seguidas
                }
            }
            else
            {
                timerExceso = 0f;
            }
        }
    }
}