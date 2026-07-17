using UnityEngine;

namespace Simulador.Core
{
    [CreateAssetMenu(fileName = "New String Variable", menuName = "Simulador/Variables/String")]
    public class StringVariable : ScriptableObject
    {
        [TextArea]
        public string Value;

        public void SetValue(string newValue) => Value = newValue;
    }
}