using UnityEngine;

namespace Simulador.Core
{
    [CreateAssetMenu(fileName = "New Vector3 Variable", menuName = "Simulador/Variables/Vector3")]
    public class Vector3Variable : ScriptableObject
    {
        public Vector3 Value;
    }
}