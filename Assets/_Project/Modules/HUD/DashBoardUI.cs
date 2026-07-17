using UnityEngine;
using TMPro;
using Simulador.Core;

namespace Simulador.HUD
{
    public class DashboardUI : MonoBehaviour
    {
        [Header("Fuentes de Datos")]
        public FloatVariable speedSO;
        public IntVariable gearSO;
        public FloatVariable limitSO;
        
        public FloatVariable rpmSO;


        [Header("Componentes Visuales")]
        public TextMeshProUGUI speedText;
        public TextMeshProUGUI gearText;
        public TextMeshProUGUI limitText;
        public TextMeshProUGUI rpmText;  

        [Header("Intermitentes")]
        public GameObject leftArrow;
        public GameObject rightArrow;     

        [Header("Vehicle Controller")]
        public Simulador.PhysicsModule.VehicleController vehicle;


        [Header("Ajustes de RPM")]
        public int rpmStep = 200; 

        private void Update()
        {
            // Actualizar Velocidad (Sin decimales)
            if (speedSO != null && speedText != null)
                speedText.text = Mathf.FloorToInt(speedSO.Value).ToString(); 

            // Actualizar Marcha (Traducción de números a letras)
            if (gearSO != null && gearText != null)
                gearText.text = FormatGear(gearSO.Value);

            // Actualizar Límite
            if (limitSO != null && limitText != null)
            {
                limitText.text = (limitSO.Value <= 0) ? "--" : limitSO.Value.ToString();
            }

            if (vehicle != null)
            {
                // Esto te dirá en la consola si el HUD está intentando encender las flechas
                if (vehicle.activeBlinker != 0) 
                {
                    Debug.Log($"Intermitente: {vehicle.activeBlinker} | Estado luz: {vehicle.blinkerState}");
                }

                leftArrow.SetActive(vehicle.activeBlinker == -1 && vehicle.blinkerState);
                rightArrow.SetActive(vehicle.activeBlinker == 1 && vehicle.blinkerState);
            }

            
            if (rpmSO != null && rpmText != null)
            {
                // Lógica de redondeo por intervalos:
                // 1. Dividimos el valor real entre el paso (ej: 854 / 200 = 4.27)
                // 2. Redondeamos al entero más cercano (4.27 -> 4)
                // 3. Multiplicamos de nuevo por el paso (4 * 200 = 800)
                int roundedRPM = Mathf.RoundToInt(rpmSO.Value / rpmStep) * rpmStep;

                // Si el motor está calado, forzamos el 0 para que no queden restos
                if (vehicle != null && vehicle.isStalled) roundedRPM = 0;

                rpmText.text = "RPM: " + roundedRPM.ToString();
                
                // Cambiar a rojo si supera el límite de seguridad (ej. 5500)
                rpmText.color = (roundedRPM >= 5500) ? Color.red : Color.white;
            }

        }

        private string FormatGear(int gear)
        {
            switch (gear)
            {
                case -1: return "R"; // Reverse
                case 0:  return "N"; // Neutral
                default: return gear.ToString();
            }
        }
    }
}