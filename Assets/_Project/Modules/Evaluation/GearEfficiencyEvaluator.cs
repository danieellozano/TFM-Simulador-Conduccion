using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class GearEfficiencyEvaluator : MonoBehaviour
    {
        [Header("Referencias SOA")]
        public FloatVariable engineRPM;        // EngineRPM.asset
        public IntVariable currentGear;        // CurrentGear.asset
        public GameEvent infractionEvent;      // OnInfractionDetected.asset
        public InfraccionSO gearInfraction;    // CON_MARCHAS.asset

        [Header("Configuración de Límites")]
        public float highRPMThreshold = 4000f;
        public float lowRPMThreshold = 1000f;
        public float maxTimeOutOfRange = 5.0f; // Tiempo de tolerancia

        private float highRPMTimer = 0f;
        private float lowRPMTimer = 0f;

        private void Update()
        {
            if (engineRPM == null || currentGear == null) return;

            // 1. EVALUACIÓN DE REVOLUCIONES ALTAS (No subir marcha)
            // Solo penalizamos si estamos en 1ª, 2ª, 3ª o 4ª (en 5ª no se puede subir más)
            if (currentGear.Value >= 1 && currentGear.Value < 5 && engineRPM.Value > highRPMThreshold)
            {
                highRPMTimer += Time.deltaTime;
                if (highRPMTimer >= maxTimeOutOfRange)
                {
                    RegistrarFalta("Motor revolucionado. Suba de marcha.");
                    highRPMTimer = -10f; // Pausa para no repetir la multa inmediatamente
                }
            }
            else
            {
                highRPMTimer = 0f;
            }

            // 2. EVALUACIÓN DE REVOLUCIONES BAJAS (No bajar marcha)
            // Solo penalizamos si estamos en 2ª o superior y el motor "sufre"
            if (currentGear.Value > 1 && engineRPM.Value < lowRPMThreshold)
            {
                lowRPMTimer += Time.deltaTime;
                if (lowRPMTimer >= maxTimeOutOfRange)
                {
                    RegistrarFalta("Régimen demasiado bajo. Reduzca marcha.");
                    lowRPMTimer = -10f;
                }
            }
            else
            {
                lowRPMTimer = 0f;
            }
        }

        private void RegistrarFalta(string mensaje)
        {
            if (infractionEvent != null)
            {
                infractionEvent.Raise(gearInfraction);
                Debug.Log($"<color=yellow><b>[DGT EFICIENCIA]</b></color> {mensaje}");
            }
        }
    }
}