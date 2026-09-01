using UnityEngine;
using TMPro;
using Simulador.Core;

namespace Simulador.HUD
{
    public class DashboardUI : MonoBehaviour
    {
        [Header("Fuentes de Datos (SOA)")]
        public FloatVariable speedSO;
        public IntVariable gearSO;
        public FloatVariable limitSO;
        public FloatVariable rpmSO;
        public InputDataSO inputData;       // Referencia al contrato de entrada del Core
        public BoolVariable isStalledSO;    // Referencia al calado del Core

        [Header("Componentes Visuales")]
        public TextMeshProUGUI speedText;
        public TextMeshProUGUI gearText;
        public TextMeshProUGUI limitText;
        public TextMeshProUGUI rpmText;  

        [Header("Indicadores")]
        public GameObject leftArrow;
        public GameObject rightArrow; 
        public GameObject handbrakeIcon;    

        [Header("Ajustes")]
        public int rpmStep = 200; 

        private void Update()
        {
            // 1. Actualizar Velocidad
            if (speedSO != null && speedText != null)
                speedText.text = Mathf.FloorToInt(speedSO.Value).ToString(); 

            // 2. Actualizar Marcha
            if (gearSO != null && gearText != null)
                gearText.text = FormatGear(gearSO.Value);

            // 3. Actualizar Límite
            if (limitSO != null && limitText != null)
                limitText.text = (limitSO.Value <= 0) ? "--" : limitSO.Value.ToString();

            // 4. Actualizar Intermitentes (Lectura desde el InputData del Core)
            if (leftArrow != null && rightArrow != null && inputData != null)
            {
                // Simulamos el parpadeo basándonos en el tiempo
                bool blinkState = (Time.time % 1.0f) < 0.5f;
                leftArrow.SetActive(inputData.ActiveBlinker == -1 && blinkState);
                rightArrow.SetActive(inputData.ActiveBlinker == 1 && blinkState);
            }

            // 5. Actualizar RPM
            if (rpmSO != null && rpmText != null)
            {
                int roundedRPM = Mathf.RoundToInt(rpmSO.Value / rpmStep) * rpmStep;
                
                // Lectura del estado de calado desde el SO del Core
                if (isStalledSO != null && isStalledSO.Value) roundedRPM = 0;

                rpmText.text = "RPM: " + roundedRPM.ToString();
                rpmText.color = (roundedRPM >= 5500) ? Color.red : Color.white;
            }

            // 6. Actualizar Freno de Mano (Lectura desde el InputData del Core)
            if (handbrakeIcon != null && inputData != null)
            {
                handbrakeIcon.SetActive(inputData.Handbrake);
            }
        }

        private string FormatGear(int gear)
        {
            switch (gear)
            {
                case -1: return "R";
                case 0:  return "N";
                default: return gear.ToString();
            }
        }
    }
}