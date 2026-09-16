// using UnityEngine;
// using Simulador.Core;

// namespace Simulador.Evaluation
// {
//     public class BlinkerMisuseEvaluator : MonoBehaviour
//     {
//         [Header("Referencias SOA")]
//         public InputDataSO inputData;      // Para ver el ActiveBlinker y Steering
//         public FloatVariable vehicleSpeed;  // Para calcular la distancia recorrida
//         public GameEvent infractionEvent;  // OnInfractionDetected.asset
//         public InfraccionSO blinkerInfraction; // Regla INT-IND

//         [Header("Configuración")]
//         [Tooltip("Metros que puede recorrer con el intermitente puesto sin girar")]
//         public float maxDistanceWithoutTurn = 50f; 
        
//         [Tooltip("Umbral de giro para considerar que ha iniciado una maniobra")]
//         public float steeringThreshold = 0.15f;

//         private float distanceCounter = 0f;

//         private void Update()
//         {
//             if (inputData == null || vehicleSpeed == null) return;

//             // REGLA: Si algún intermitente está puesto (-1 o 1)
//             if (inputData.ActiveBlinker != 0)
//             {
//                 // Comprobamos si el volante está "recto" (el alumno no está girando)
//                 if (Mathf.Abs(inputData.Steering) < steeringThreshold)
//                 {
//                     // Calculamos la distancia recorrida en este frame: (m/s * segundos)
//                     // velocidad.Value está en Km/h, dividimos por 3.6 para pasar a m/s
//                     float distanceThisFrame = (vehicleSpeed.Value / 3.6f) * Time.deltaTime;
//                     distanceCounter += distanceThisFrame;

//                     // Si supera los metros permitidos sin haber girado...
//                     if (distanceCounter >= maxDistanceWithoutTurn)
//                     {
//                         infractionEvent.Raise(blinkerInfraction);
//                         Debug.Log("<color=yellow>DGT: Señalización innecesaria detectada (Olvido/Error).</color>");
                        
//                         // Reseteamos el contador para no poner 50 multas por el mismo olvido
//                         distanceCounter = 0f; 
//                     }
//                 }
//                 else
//                 {
//                     // Si el alumno gira el volante de forma significativa, entendemos 
//                     // que SÍ está haciendo la maniobra y reseteamos el contador.
//                     distanceCounter = 0f;
//                 }
//             }
//             else
//             {
//                 // Si el intermitente está apagado (0), reseteamos
//                 distanceCounter = 0f;
//             }
//         }
//     }
// }


using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Evalúa el uso indebido o el olvido de los indicadores de dirección (intermitentes).
    // Implementa un algoritmo de odometría acumulada en línea recta que detecta si el conductor
    // circula una distancia excesiva con el intermitente activado sin ejecutar maniobra de giro,
    // emitiendo la infracción leve correspondiente (INT-IND) según el baremo de la DGT.
    public class BlinkerMisuseEvaluator : MonoBehaviour
    {
        [Header("Referencias SOA")]
        [Tooltip("Contrato de entradas que expone el estado del intermitente (ActiveBlinker) y el giro (Steering).")]
        public InputDataSO inputData;

        [Tooltip("Canal de datos de telemetría que suministra la velocidad lineal instantánea del vehículo en Km/h.")]
        public FloatVariable vehicleSpeed;

        [Tooltip("Canal de eventos reactivo utilizado para notificar la detección de una infracción hacia el evaluador.")]
        public GameEvent infractionEvent;

        [Tooltip("Activo ScriptableObject que define los metadatos y penalización de la falta INT-IND.")]
        public InfraccionSO blinkerInfraction;

        [Header("Configuración")]
        [Tooltip("Distancia máxima permitida en metros circulando en línea recta con el intermitente antes de sancionar.")]
        public float maxDistanceWithoutTurn = 50f; 
        
        [Tooltip("Umbral de deflexión del volante (|Steering|) para determinar que se ha iniciado un giro real.")]
        public float steeringThreshold = 0.15f;

        // Acumulador de distancia recorrida en metros manteniendo trayectoria rectilínea
        private float distanceCounter = 0f;

        // Bucle de evaluación continua por fotograma para calcular el avance lineal y vigilar la señalización
        private void Update()
        {
            if (inputData == null || vehicleSpeed == null) return;

            // Comprueba si existe algún indicador de dirección encendido (-1: Izquierdo, 1: Derecho)
            if (inputData.ActiveBlinker != 0)
            {
                // Se evalúa si el volante se mantiene dentro de la zona neutra de trayectoria recta
                if (Mathf.Abs(inputData.Steering) < steeringThreshold)
                {
                    // Conversión cinemática: de Km/h a m/s dividiendo entre 3.6, multiplicado por el diferencial de tiempo
                    float distanceThisFrame = (vehicleSpeed.Value / 3.6f) * Time.deltaTime;
                    distanceCounter += distanceThisFrame;

                    // Si la distancia acumulada en recta excede la tolerancia permitida, se valida la infracción
                    if (distanceCounter >= maxDistanceWithoutTurn)
                    {
                        if (infractionEvent != null && blinkerInfraction != null)
                        {
                            infractionEvent.Raise(blinkerInfraction);
                        }

                        Debug.Log("<color=yellow>DGT: Señalización innecesaria detectada (Olvido/Error).</color>");
                        
                        // Restablece el contador para evitar penalizaciones redundantes consecutivas por el mismo descuido
                        distanceCounter = 0f; 
                    }
                }
                else
                {
                    // Si el conductor gira el volante superando el umbral, se confirma la maniobra y se resetea la odometría
                    distanceCounter = 0f;
                }
            }
            else
            {
                // Si los intermitentes se encuentran apagados, el acumulador se mantiene en reposo
                distanceCounter = 0f;
            }
        }
    }
}