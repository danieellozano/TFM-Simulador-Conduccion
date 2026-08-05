using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class BlinkerMisuseEvaluator : MonoBehaviour
    {
        [Header("Referencias SOA")]
        public InputDataSO inputData;      // Para ver el ActiveBlinker y Steering
        public FloatVariable vehicleSpeed;  // Para calcular la distancia recorrida
        public GameEvent infractionEvent;  // OnInfractionDetected.asset
        public InfraccionSO blinkerInfraction; // Regla INT-IND

        [Header("Configuración")]
        [Tooltip("Metros que puede recorrer con el intermitente puesto sin girar")]
        public float maxDistanceWithoutTurn = 50f; 
        
        [Tooltip("Umbral de giro para considerar que ha iniciado una maniobra")]
        public float steeringThreshold = 0.15f;

        private float distanceCounter = 0f;

        private void Update()
        {
            if (inputData == null || vehicleSpeed == null) return;

            // REGLA: Si algún intermitente está puesto (-1 o 1)
            if (inputData.ActiveBlinker != 0)
            {
                // Comprobamos si el volante está "recto" (el alumno no está girando)
                if (Mathf.Abs(inputData.Steering) < steeringThreshold)
                {
                    // Calculamos la distancia recorrida en este frame: (m/s * segundos)
                    // velocidad.Value está en Km/h, dividimos por 3.6 para pasar a m/s
                    float distanceThisFrame = (vehicleSpeed.Value / 3.6f) * Time.deltaTime;
                    distanceCounter += distanceThisFrame;

                    // Si supera los metros permitidos sin haber girado...
                    if (distanceCounter >= maxDistanceWithoutTurn)
                    {
                        infractionEvent.Raise(blinkerInfraction);
                        Debug.Log("<color=yellow>DGT: Señalización innecesaria detectada (Olvido/Error).</color>");
                        
                        // Reseteamos el contador para no poner 50 multas por el mismo olvido
                        distanceCounter = 0f; 
                    }
                }
                else
                {
                    // Si el alumno gira el volante de forma significativa, entendemos 
                    // que SÍ está haciendo la maniobra y reseteamos el contador.
                    distanceCounter = 0f;
                }
            }
            else
            {
                // Si el intermitente está apagado (0), reseteamos
                distanceCounter = 0f;
            }
        }
    }
}