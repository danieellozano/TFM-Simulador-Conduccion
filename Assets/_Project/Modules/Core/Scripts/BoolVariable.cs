using UnityEngine;

namespace Simulador.Core
{
    [CreateAssetMenu(fileName = "New Bool Variable", menuName = "Simulador/Variables/Bool")]
    public class BoolVariable : ScriptableObject
    {
        public bool Value;
    }
}
