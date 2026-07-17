using UnityEngine;
using Simulador.Core; // Para acceder a FloatVariable

namespace Simulador.Core
{
    public class SpeedLimitSign : MonoBehaviour
    {
        [Header("Configuración de la Señal")]
        [Tooltip("El valor que esta señal inyectará en el sistema")]
        public float speedLimitValue; 
        
        [Header("Canal de Salida")]
        public FloatVariable currentLimitSO; // Arrastra aquí el asset CurrentSpeedLimit

        private void OnTriggerEnter(Collider other)
        {
            // Solo si lo que atraviesa la señal es el coche del alumno
            if (other.CompareTag("Player"))
            {
                if (currentLimitSO != null)
                {
                    currentLimitSO.Value = speedLimitValue;
                    Debug.Log($"<color=cyan>ENTORNO:</color> Límite de velocidad actualizado a {speedLimitValue} Km/h");
                }
            }
        }
    }
}