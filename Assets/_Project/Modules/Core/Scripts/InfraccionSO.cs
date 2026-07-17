using UnityEngine;

namespace Simulador.Core
{
    [CreateAssetMenu(fileName = "NuevaInfraccion", menuName = "Simulador/Reglas/Infraccion")]
    public class InfraccionSO : ScriptableObject
    {
        public string codigo; // Ej: SIG-STOP
        public string descripcion;
        public enum Gravedad { Leve, Deficiente, Eliminatoria }
        public Gravedad tipo;
        public int puntosPenalizacion;
    }
}