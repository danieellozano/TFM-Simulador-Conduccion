using UnityEngine;
using TMPro;
using Simulador.Core;

namespace Simulador.HUD
{
    // Componente de la interfaz de usuario (HUD) encargado del renderizado en tiempo real del cuadro de instrumentos.
    // Consume datos desvinculados del núcleo del juego mediante ScriptableObjects (SOA), garantizando una
    // arquitectura reactiva que desacopla la presentación visual de la simulación de físicas e inyección de entradas.
    public class DashboardUI : MonoBehaviour
    {
        [Header("Fuentes de Datos (SOA)")]
        [Tooltip("Contenedor reactivo con la velocidad actual del vehículo cinemático.")]
        public FloatVariable speedSO;
        [Tooltip("Contenedor reactivo con la marcha o relación de cambio seleccionada.")]
        public IntVariable gearSO;
        [Tooltip("Contenedor reactivo con el límite de velocidad máximo de la vía actual.")]
        public FloatVariable limitSO;
        [Tooltip("Contenedor reactivo con el régimen dinámico de revoluciones por minuto (RPM).")]
        public FloatVariable rpmSO;
        [Tooltip("Contrato y buffer de entrada normalizado del sistema HAL.")]
        public InputDataSO inputData;       // Referencia al contrato de entrada del Core
        [Tooltip("Estado reactivo de la condición de calado del motor térmico.")]
        public BoolVariable isStalledSO;    // Referencia al calado del Core

        [Header("Componentes Visuales")]
        [Tooltip("Elemento de texto TMP para desplegar la velocidad lineal instantánea.")]
        public TextMeshProUGUI speedText;
        [Tooltip("Elemento de texto TMP para la marcha acoplada (R, N, 1, 2...).")]
        public TextMeshProUGUI gearText;
        [Tooltip("Elemento de texto TMP para el límite de velocidad normativo.")]
        public TextMeshProUGUI limitText;
        [Tooltip("Elemento de texto TMP para desplegar el régimen de RPM.")]
        public TextMeshProUGUI rpmText;  

        [Header("Indicadores Visuales")]
        [Tooltip("Icono indicador de señalización de maniobra hacia la izquierda.")]
        public GameObject leftArrow;
        [Tooltip("Icono indicador de señalización de maniobra hacia la derecha.")]
        public GameObject rightArrow; 
        [Tooltip("Testigo luminoso del estado del freno de estacionamiento/mano.")]
        public GameObject handbrakeIcon;    

        [Header("Ajustes de Renderizado")]
        [Tooltip("Pasos de redondeo de RPM para estabilizar la oscilación visual de la aguja/texto.")]
        public int rpmStep = 200; 

        // Sincroniza frame a frame la representación visual del HUD con el estado numérico del simulador.
        // Realiza comprobaciones de nulos para garantizar robustez frente a deserialización o desconexión de canales SOA.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void Update()
        {
            // 1. Renderizado de Velocidad Instantánea (Truncado entero para evitar flicker numérico)
            if (speedSO != null && speedText != null)
                speedText.text = Mathf.FloorToInt(speedSO.Value).ToString(); 

            // 2. Mapeo Sintáctico de la Transmisión
            if (gearSO != null && gearText != null)
                gearText.text = FormatGear(gearSO.Value);

            // 3. Renderizado de Señalización y Límite de Velocidad Normativo
            if (limitSO != null && limitText != null)
                limitText.text = (limitSO.Value <= 0) ? "--" : limitSO.Value.ToString();

            // 4. Actualización de Intermitentes con Modulación Temporal (Blinking)
            // Se realiza la lectura directa desde el buffer normalizado de la HAL (InputDataSO)
            if (leftArrow != null && rightArrow != null && inputData != null)
            {
                // Cálculo de la frecuencia de parpadeo (50% de ciclo de trabajo a 1Hz)
                bool blinkState = (Time.time % 1.0f) < 0.5f;
                leftArrow.SetActive(inputData.ActiveBlinker == -1 && blinkState);
                rightArrow.SetActive(inputData.ActiveBlinker == 1 && blinkState);
            }

            // 5. Renderizado del Régimen de RPM del Motor
            if (rpmSO != null && rpmText != null)
            {
                // Redondeo por escalones para simular la inercia analógica del tacómetro
                int roundedRPM = Mathf.RoundToInt(rpmSO.Value / rpmStep) * rpmStep;
                
                // Forzado de régimen a 0 RPM en caso de que el motor haya calado por fallo del conductor
                if (isStalledSO != null && isStalledSO.Value) roundedRPM = 0;

                rpmText.text = "RPM: " + roundedRPM.ToString();
                
                // Cambio dinámico del color del texto como retroalimentación de corte de inyección o sobre-régimen
                rpmText.color = (roundedRPM >= 5500) ? Color.red : Color.white;
            }

            // 6. Testigo Luminoso del Freno de Estacionamiento
            if (handbrakeIcon != null && inputData != null)
            {
                handbrakeIcon.SetActive(inputData.Handbrake);
            }
        }

        // Traduce el valor numérico entero de la marcha a una nomenclatura estándar de automoción.
        // Parámetros:
        //   - gear: Índices numéricos de la marcha (-1 para marcha atrás, 0 para punto muerto, >0 para avance).
        // Salida: Cadena de texto formateada ("R", "N", o el número de relación correspondiente).
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