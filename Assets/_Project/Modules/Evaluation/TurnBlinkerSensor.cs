using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Evaluador de señalización luminosa en maniobras de giro en intersecciones.
    // Audita si el alumno acciona correctamente los indicadores de dirección (intermitentes) al aproximarse y efectuar un giro en cruces urbanos.
    // Registra el vector de orientación frontal de entrada y el estado de los intermitentes al ingresar al área de control.
    // Al salir, calcula el ángulo con signo en el plano horizontal (Vector3.SignedAngle) para clasificar la trayectoria realizada
    // (giro a la derecha, izquierda o continuar recto) y sancionar la falta leve por omisión o mala señalización (INT-IND).
    public class SmartIntersectionEvaluator : MonoBehaviour
    {
        [Header("Referencias de Datos")]
        [Tooltip("Canal de datos de entrada normalizado del que se consulta el estado activo de los intermitentes (-1 izquierda, 0 apagado, 1 derecha).")]
        public InputDataSO inputData;

        [Tooltip("Canal del bus de eventos para emitir la infracción hacia el evaluador central de la DGT.")]
        public GameEvent infractionEvent;

        [Tooltip("Activo normativo que define la falta leve por omisión o mala señalización de una maniobra de giro (INT-IND).")]
        public InfraccionSO blinkerInfraction; // INT-IND (Leve)

        // Vector unitario que almacena la dirección hacia adelante (forward) del vehículo en el instante exacto de entrar a la intersección
        private Vector3 directionAtEntry;

        // Estado del intermitente registrado en el momento de la entrada (-1: izquierdo, 0: ninguno, 1: derecho)
        private int blinkerAtEntry;

        // Bandera de control que indica si actualmente se está auditando una maniobra de cruce activa
        private bool isEvaluating = false;

        // Inicia la monitorización espacial de la maniobra al detectar la entrada del vehículo en el área de la intersección.
        // Captura la orientación previa del utilitario y la señalización activa que presentaba el alumno antes del cruce.
        // Parámetros:
        //   other: Colisionador del objeto que ingresa al volumen de activación (Trigger).
        // Salida:
        //   No devuelve ningún valor (void).
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // Guardamos la dirección frontal del chasis al iniciar la aproximación
                directionAtEntry = other.transform.forward;

                // Capturamos el intermitente que el alumno tenía puesto al entrar al cruce
                blinkerAtEntry = inputData.ActiveBlinker;
                isEvaluating = true;
                
                Debug.Log("<color=white>DGT:</color> Evaluando maniobra en intersección...");
            }
        }

        // Concluye la auditoría cinemática en el instante en que el vehículo abandona el volumen físico de la intersección.
        // Calcula la diferencia angular entre la entrada y la salida para determinar la maniobra real ejecutada.
        // Parámetros:
        //   other: Colisionador del objeto que abandona el volumen de activación (Trigger).
        // Salida:
        //   No devuelve ningún valor (void).
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && isEvaluating)
            {
                // Vector hacia adelante que presenta el vehículo al salir de la zona de control
                Vector3 directionAtExit = other.transform.forward;
                
                // Cálculo del ángulo de desviación en grados sobre el plano horizontal (eje Y / Vector3.up).
                // Un ángulo positivo representa un viraje hacia la derecha, mientras que un ángulo negativo representa un viraje a la izquierda
                float turnAngle = Vector3.SignedAngle(directionAtEntry, directionAtExit, Vector3.up);

                // Evalúa si el intermitente inicial coincidía con la trayectoria final
                ValidarManiobra(turnAngle);
                isEvaluating = false;
            }
        }

        // Clasifica matemáticamente el giro efectuado y comprueba si la señalización luminosa coincide con la maniobra real.
        // Si el alumno omitió el indicador o activó el lado contrario al giro, se dispara la falta reglamentaria.
        // Parámetros:
        //   angle: Ángulo con signo en grados resultante de la comparación vectorial entre la entrada y la salida.
        // Salida:
        //   No devuelve ningún valor (void).
        private void ValidarManiobra(float angle)
        {
            int requiredBlinker = 0; // Valor neutro: continuar en línea recta no requiere señalización

            // Se establece un umbral de tolerancia de 45 grados para discriminar maniobras de giro reales frente a correcciones de carril
            if (angle > 45f) 
            {
                requiredBlinker = 1; // Giro significativo a la derecha: exige intermitente derecho (+1)
            }
            else if (angle < -45f) 
            {
                requiredBlinker = -1; // Giro significativo a la izquierda: exige intermitente izquierdo (-1)
            }
            else 
            {
                requiredBlinker = 0; // Desviaciones menores a 45 grados se consideran continuación recta
            }

            // Se compara la señal luminosa exigida por el giro real frente a la que el alumno activó al entrar
            if (blinkerAtEntry != requiredBlinker)
            {
                // Dispara la infracción leve por maniobra no señalizada o señalizada incorrectamente (INT-IND)
                infractionEvent.Raise(blinkerInfraction);
                string maniobra = requiredBlinker == 1 ? "Derecha" : (requiredBlinker == -1 ? "Izquierda" : "Recto");
                Debug.Log($"<color=red>DGT:</color> Maniobra de giro a la {maniobra} mal señalizada.");
            }
            else
            {
                Debug.Log("<color=green>DGT:</color> Maniobra señalizada correctamente.");
            }
        }
    }
}