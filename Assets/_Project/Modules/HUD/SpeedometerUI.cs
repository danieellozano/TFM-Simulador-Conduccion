using UnityEngine;
using TMPro; 
using Simulador.Core; // <--- Ahora esto ya no dará error

namespace Simulador.HUD
{
    public class SpeedometerUI : MonoBehaviour
    {
        public FloatVariable currentSpeed; 
        public TextMeshProUGUI speedText;  

        private void Update()
        {
            if (currentSpeed != null && speedText != null)
            {
                speedText.text = Mathf.FloorToInt(currentSpeed.Value).ToString() + " Km/h";
            }
        }
    }
}