using UnityEngine;

namespace Simulador.Core
{
    [CreateAssetMenu(fileName = "New Float Variable", menuName = "Simulador/Variables/Float")]
    public class FloatVariable : ScriptableObject
    {
        [Tooltip("Valor actual de esta variable compartida")]
        public float Value;

        // Opcional: Método para resetear el valor al iniciar la aplicación
        public void SetValue(float newValue) => Value = newValue;
    }
}