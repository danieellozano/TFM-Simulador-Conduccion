using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class SpeedEvaluator : MonoBehaviour
    {
        [Header("Referencias de Datos")]
        public FloatVariable currentSpeedSO;      // CurrentSpeed.asset
        public FloatVariable currentSpeedLimitSO; // CurrentSpeedLimit.asset
        public GameEvent infractionEvent;         // OnInfractionDetected.asset

        [Header("Catálogo de Infracciones DGT")]
        public InfraccionSO speedInfractionLeve;         // VEL_GEN (Leve: Exceso > 10 a 20 Km/h)
        public InfraccionSO speedInfractionDeficiente;   // VEL_DEF (Deficiente: Exceso > 20 a 30 Km/h)
        public InfraccionSO speedInfractionEliminatoria; // VEL_MAX (Eliminatoria: Exceso > 30 Km/h)

        private float timerExceso = 0f;

        private void Update()
        {
            if (currentSpeedSO == null || currentSpeedLimitSO == null) return;

            // Calculamos la diferencia neta de velocidad sobre el límite
            float exceso = currentSpeedSO.Value - currentSpeedLimitSO.Value;

            // La DGT comienza a sancionar a partir de superar en más de 10 Km/h el límite
            if (exceso > 10f)
            {
                timerExceso += Time.deltaTime;

                // Si mantiene el exceso más de 2 segundos de forma continua, multa
                if (timerExceso > 2f)
                {
                    if (infractionEvent != null)
                    {
                        // Clasificación del tipo de falta según el exceso medido en Km/h
                        if (exceso > 30f)
                        {
                            infractionEvent.Raise(speedInfractionEliminatoria);
                        }
                        else if (exceso > 20f)
                        {
                            infractionEvent.Raise(speedInfractionDeficiente);
                        }
                        else
                        {
                            infractionEvent.Raise(speedInfractionLeve);
                        }
                    }
                    timerExceso = -3f; // Cooldown de 3 seg para no saturar el reporte
                }
            }
            else
            {
                timerExceso = 0f;
            }
        }
    }
}