using UnityEngine;
using Simulador.Core; // Para acceder a FloatVariable

namespace Simulador.Core
{
    // Elemento de infraestructura vial que actúa como proveedor del límite de velocidad normativo.
    // Inyecta el valor de la señalización en la capa de datos global mediante ScriptableObject (SOA)
    // al ser atravesado por el vehículo del jugador, permitiendo la evaluación del cumplimiento del reglamento DGT.
    public class SpeedLimitSign : MonoBehaviour, ISpeedLimitProvider
    {
        [Header("Configuración de la Señal")]
        [Tooltip("El valor que esta señal inyectará en el sistema")]
        public float speedLimitValue; 
        
        [Header("Canal de Salida")]
        [Tooltip("Canal de datos SOA donde se escribe el límite de velocidad activo de la vía.")]
        public FloatVariable currentLimitSO; // Arrastra aquí el asset CurrentSpeedLimit

        // Devuelve el valor numérico del límite de velocidad configurado en este elemento.
        // Parámetros: Ninguno.
        // Salida: Valor flotante con la velocidad máxima permitida en km/h.
        public float GetSpeedLimit() => speedLimitValue;

        // Detecta el cruce del vehículo del jugador por la zona de influencia de la señal e inyecta la nueva velocidad límite.
        // Parámetros:
        //   - other: Collider del objeto físico que entra en el volumen disparador (Trigger).
        // Salida: Ninguna.
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