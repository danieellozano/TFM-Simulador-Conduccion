// using UnityEngine;
// using Simulador.Core;

// namespace Simulador.Evaluation
// {
//     public class CurveSpeedEvaluator : MonoBehaviour
//     {
//         [Header("Referencias SOA")]
//         public FloatVariable currentSpeed;    // CurrentSpeed.asset
//         public GameEvent infractionEvent;     // OnInfractionDetected.asset

//         [Header("Reglas por Gravedad")]
//         public InfraccionSO ruleLeve;
//         public InfraccionSO ruleDeficiente;
//         public InfraccionSO ruleEliminatoria;

//         [Header("Umbrales de Velocidad (Km/h)")]
//         [Tooltip("Si supera esto es Leve")]
//         public float limitLeve = 22f;
//         [Tooltip("Si supera esto es Deficiente")]
//         public float limitDeficiente = 30f;
//         [Tooltip("Si supera esto es Eliminatoria")]
//         public float limitEliminatoria = 38f;

//         private float maxSpeedInCurve = 0f;
//         private bool isPlayerInside = false;

//         // Variables añadidas para la detección de trayectoria y gestión de colisionadores
//         private Vector3 direccionEntrada;
//         private int collidersInsideCount = 0;
//         private const float ANGULO_MINIMO_GIRO = 35f; // Grados mínimos para considerarlo un giro

//         private void Update()
//         {
//             if (isPlayerInside)
//             {
//                 // Registramos la velocidad más alta alcanzada en la curva
//                 if (currentSpeed.Value > maxSpeedInCurve)
//                 {
//                     maxSpeedInCurve = currentSpeed.Value;
//                 }
//             }
//         }

//         private void OnTriggerEnter(Collider other)
//         {
//             if (other.CompareTag("Player"))
//             {
//                 collidersInsideCount++;

//                 // Solo inicializamos el monitoreo si es el primer colisionador del coche que entra
//                 if (collidersInsideCount == 1)
//                 {
//                     isPlayerInside = true;
//                     maxSpeedInCurve = 0f; // Reset al entrar

//                     // Guardamos la dirección hacia adelante (forward) del chasis principal del coche al entrar
//                     Transform rootVehicle = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
//                     direccionEntrada = rootVehicle.forward;

//                     Debug.Log("<color=white>SISTEMA:</color> Entrando en curva. Monitorizando velocidad...");
//                 }
//             }
//         }

//         private void OnTriggerExit(Collider other)
//         {
//             if (other.CompareTag("Player"))
//             {
//                 collidersInsideCount--;

//                 // Solo evaluamos cuando el coche haya salido por completo (todos sus colisionadores salieron)
//                 if (collidersInsideCount <= 0)
//                 {
//                     collidersInsideCount = 0; // Seguridad para evitar números negativos
//                     isPlayerInside = false;

//                     // Obtenemos la dirección hacia adelante del chasis al salir por completo
//                     Transform rootVehicle = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
//                     Vector3 direccionSalida = rootVehicle.forward;

//                     // Calculamos los grados de diferencia entre la entrada y la salida
//                     float anguloDeGiroEfectuado = Vector3.Angle(direccionEntrada, direccionSalida);

//                     // Si el giro real supera el ángulo mínimo, evaluamos la curva
//                     if (anguloDeGiroEfectuado >= ANGULO_MINIMO_GIRO)
//                     {
//                         Debug.Log($"<color=cyan>SISTEMA:</color> Giro detectado ({anguloDeGiroEfectuado:F1}°). Evaluando curva...");
//                         EvaluarVelocidadFinal();
//                     }
//                     else
//                     {
//                         Debug.Log($"<color=green>SISTEMA:</color> Trayectoria recta detectada ({anguloDeGiroEfectuado:F1}°). Omitiendo evaluación.");
//                     }
//                 }
//             }
//         }

//         private void EvaluarVelocidadFinal()
//         {
//             if (infractionEvent == null) return;

//             // Comparamos de mayor a menor gravedad
//             if (maxSpeedInCurve >= limitEliminatoria)
//             {
//                 if (ruleEliminatoria != null) infractionEvent.Raise(ruleEliminatoria);
//                 Debug.Log($"<color=red>DGT ELIMINATORIA:</color> Velocidad peligrosa en curva ({maxSpeedInCurve} km/h)");
//             }
//             else if (maxSpeedInCurve >= limitDeficiente)
//             {
//                 if (ruleDeficiente != null) infractionEvent.Raise(ruleDeficiente);
//                 Debug.Log($"<color=orange>DGT DEFICIENTE:</color> Velocidad inadecuada en curva ({maxSpeedInCurve} km/h)");
//             }
//             else if (maxSpeedInCurve >= limitLeve)
//             {
//                 if (ruleLeve != null) infractionEvent.Raise(ruleLeve);
//                 Debug.Log($"<color=yellow>DGT LEVE:</color> Velocidad algo superior a la aconsejable ({maxSpeedInCurve} km/h)");
//             }
//         }
//     }
// }


using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Delimita y audita espacialmente el paso por curva del vehículo del alumno mediante volúmenes disparadores (Triggers).
    // Implementa un sistema de gestión multi-colisionador para evitar falsas entradas/salidas provocadas por las ruedas y el chasis,
    // calcula la desviación angular de la trayectoria mediante álgebra vectorial (Vector3.Angle) para descartar tramos rectos,
    // registra el pico máximo de velocidad alcanzado y aplica el baremo de gravedad de la DGT (VEL-AL, VEL-AD, VEL-AE).
    public class CurveSpeedEvaluator : MonoBehaviour
    {
        [Header("Referencias SOA")]
        [Tooltip("Canal de telemetría desacoplada que almacena la velocidad lineal instantánea del vehículo en Km/h.")]
        public FloatVariable currentSpeed;

        [Tooltip("Canal del bus de eventos globales invocado al registrar una infracción de tráfico.")]
        public GameEvent infractionEvent;

        [Header("Reglas por Gravedad")]
        [Tooltip("Falta Leve (VEL-AL): Velocidad ligeramente superior a la recomendada en curva.")]
        public InfraccionSO ruleLeve;

        [Tooltip("Falta Deficiente (VEL-AD): Velocidad inadecuada en curva que genera riesgo de derrape leve.")]
        public InfraccionSO ruleDeficiente;

        [Tooltip("Falta Eliminatoria (VEL-AE): Velocidad peligrosa en curva con riesgo inminente de vuelco o salida de vía.")]
        public InfraccionSO ruleEliminatoria;

        [Header("Umbrales de Velocidad (Km/h)")]
        [Tooltip("Límite inferior para considerar falta Leve (calibrado en 22 Km/h).")]
        public float limitLeve = 22f;

        [Tooltip("Límite intermedio para considerar falta Deficiente (calibrado en 30 Km/h).")]
        public float limitDeficiente = 30f;

        [Tooltip("Límite superior a partir del cual se considera falta Eliminatoria (calibrado en 38 Km/h).")]
        public float limitEliminatoria = 38f;

        // Registra el valor de velocidad más alto alcanzado mientras el vehículo permanece dentro del volumen de la curva
        private float maxSpeedInCurve = 0f;

        // Bandera de control para indicar si el vehículo del alumno se encuentra actualmente dentro del área de la curva
        private bool isPlayerInside = false;

        // Vector de avance frontal (forward) del chasis en el momento exacto de ingresar a la curva
        private Vector3 direccionEntrada;

        // Contador de colisionadores del coche dentro del volumen (chasis + 4 ruedas), evitando falsos OnTriggerExit
        private int collidersInsideCount = 0;

        // Umbral angular mínimo (en grados) entre la entrada y la salida para certificar que el vehículo realizó un giro real
        private const float ANGULO_MINIMO_GIRO = 35f;

        // Monitoriza continuamente la velocidad del vehículo mientras permanece en la curva para registrar el pico máximo
        private void Update()
        {
            if (isPlayerInside && currentSpeed != null)
            {
                // Almacena el valor máximo histórico alcanzado durante la trayectoria del viraje
                if (currentSpeed.Value > maxSpeedInCurve)
                {
                    maxSpeedInCurve = currentSpeed.Value;
                }
            }
        }

        // Detecta el ingreso del vehículo en el volumen de control de la curva.
        // Parámetros:
        //   other: Colisionador que entra en contacto con el trigger de la curva.
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                collidersInsideCount++;

                // Solo inicializa el monitoreo cuando entra el PRIMER colisionador del utilitario
                if (collidersInsideCount == 1)
                {
                    isPlayerInside = true;
                    maxSpeedInCurve = 0f; // Reinicia el registro de velocidad pico

                    // Obtiene la dirección hacia adelante (forward) del cuerpo principal del vehículo al entrar
                    Transform rootVehicle = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
                    direccionEntrada = rootVehicle.forward;

                    Debug.Log("<color=white>SISTEMA:</color> Entrando en curva. Monitorizando velocidad...");
                }
            }
        }

        // Detecta el abandono del vehículo del área de la curva y ejecuta la auditoría vectorial y cinemática.
        // Parámetros:
        //   other: Colisionador que abandona el volumen del trigger.
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                collidersInsideCount--;

                // Solo ejecuta la evaluación cuando TODOS los colisionadores del coche hayan abandonado el volumen
                if (collidersInsideCount <= 0)
                {
                    collidersInsideCount = 0; // Previene valores negativos residuales
                    isPlayerInside = false;

                    // Captura la dirección de avance frontal del coche en el instante de salida
                    Transform rootVehicle = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
                    Vector3 direccionSalida = rootVehicle.forward;

                    // 1. FILTRADO VECTORIAL DE TRAYECTORIA
                    // Calcula los grados de diferencia angular entre el vector de entrada y el de salida
                    float anguloDeGiroEfectuado = Vector3.Angle(direccionEntrada, direccionSalida);

                    // Si la desviación angular supera el umbral (>= 35°), confirma que se trata de una maniobra de giro real
                    if (anguloDeGiroEfectuado >= ANGULO_MINIMO_GIRO)
                    {
                        Debug.Log($"<color=cyan>SISTEMA:</color> Giro detectado ({anguloDeGiroEfectuado:F1}°). Evaluando curva...");
                        EvaluarVelocidadFinal();
                    }
                    else
                    {
                        // Si el ángulo es menor, el alumno cruzó en línea recta (ej. intersección en cruz); se omite la sanción
                        Debug.Log($"<color=green>SISTEMA:</color> Trayectoria recta detectada ({anguloDeGiroEfectuado:F1}°). Omitiendo evaluación.");
                    }
                }
            }
        }

        // Compara la velocidad máxima registrada frente al baremo de gravedad de la DGT y emite la falta correspondiente.
        private void EvaluarVelocidadFinal()
        {
            if (infractionEvent == null) return;

            // Se evalúa en cascada jerárquica de mayor a menor gravedad

            // CASO 1: Falta Eliminatoria (VEL-AE) -> Exceso crítico de velocidad con riesgo de pérdida de control
            if (maxSpeedInCurve >= limitEliminatoria)
            {
                if (ruleEliminatoria != null) infractionEvent.Raise(ruleEliminatoria);
                Debug.Log($"<color=red>DGT ELIMINATORIA:</color> Velocidad peligrosa en curva ({maxSpeedInCurve} km/h)");
            }
            // CASO 2: Falta Deficiente (VEL-AD) -> Velocidad inadecuada que compromete la estabilidad lateral
            else if (maxSpeedInCurve >= limitDeficiente)
            {
                if (ruleDeficiente != null) infractionEvent.Raise(ruleDeficiente);
                Debug.Log($"<color=orange>DGT DEFICIENTE:</color> Velocidad inadecuada en curva ({maxSpeedInCurve} km/h)");
            }
            // CASO 3: Falta Leve (VEL-AL) -> Velocidad ligeramente superior a la aconsejada para el trazado
            else if (maxSpeedInCurve >= limitLeve)
            {
                if (ruleLeve != null) infractionEvent.Raise(ruleLeve);
                Debug.Log($"<color=yellow>DGT LEVE:</color> Velocidad algo superior a la aconsejable ({maxSpeedInCurve} km/h)");
            }
        }
    }
}