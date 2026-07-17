using UnityEngine;

namespace Simulador.Core
{
    [CreateAssetMenu(fileName = "New Int Variable", menuName = "Simulador/Variables/Int")]
    public class IntVariable : ScriptableObject
    {
        [Tooltip("Valor entero compartido (ej. Marcha actual)")]
        public int Value;

        public void SetValue(int newValue) => Value = newValue;
    }
}