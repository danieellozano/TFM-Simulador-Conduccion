using UnityEngine;

namespace Simulador.Core
{
    [CreateAssetMenu(fileName = "New Input Data", menuName = "Simulador/Variables/Input Data")]
    public class InputDataSO : ScriptableObject
    {
        [Header("Canales de Entrada")]
        public float Throttle;  // Valor de la W (0 a 1)
        public float Steering;  // Valor de A/D (-1 a 1)
        public float Breaking;  // Valor de la S (0 a 1)
        public float Clutch; // Shift (Embrague: 0 suelto, 1 pisado)
        public int CurrentGear; // -1 Reversa, 0 Neutral, 1-5 Marchas
        public int ActiveBlinker; // -1 Izq, 0 Off, 1 Der

        public void ResetData()
        {
            Throttle = 0;
            Steering = 0;
            Breaking = 0;
            Clutch = 0;
            CurrentGear = 0;
            ActiveBlinker = 0;
        }
    }
}