using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Este script ahora solo funciona como una señal de tráfico que inyecta el nuevo límite al sistema
    public class SpeedZoneSensor : MonoBehaviour
    {
        [Header("Configuración del Hito / Señal")]
        public float speedLimit = 30f; // El nuevo límite que se activa al pasar por este punto

        [Header("Referencias")]
        public FloatVariable currentLimitSO; // CurrentSpeedLimit.asset

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // Al cruzar la línea de la señal, actualizamos el límite global de forma permanente.
                // Este valor persistirá en el HUD y en el SpeedEvaluator hasta que crucemos otra señal.
                if (currentLimitSO != null)
                {
                    currentLimitSO.Value = speedLimit;
                    Debug.Log($"<color=yellow>DGT [Señal]:</color> Límite de velocidad del examen actualizado a {speedLimit} Km/h.");
                }
            }
        }
    }
}