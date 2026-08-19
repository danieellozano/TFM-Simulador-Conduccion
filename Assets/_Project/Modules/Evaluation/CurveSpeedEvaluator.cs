using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class CurveSpeedEvaluator : MonoBehaviour
    {
        [Header("Referencias SOA")]
        public FloatVariable currentSpeed;    // CurrentSpeed.asset
        public GameEvent infractionEvent;     // OnInfractionDetected.asset

        [Header("Reglas por Gravedad")]
        public InfraccionSO ruleLeve;
        public InfraccionSO ruleDeficiente;
        public InfraccionSO ruleEliminatoria;

        [Header("Umbrales de Velocidad (Km/h)")]
        [Tooltip("Si supera esto es Leve")]
        public float limitLeve = 22f;
        [Tooltip("Si supera esto es Deficiente")]
        public float limitDeficiente = 30f;
        [Tooltip("Si supera esto es Eliminatoria")]
        public float limitEliminatoria = 38f;

        private float maxSpeedInCurve = 0f;
        private bool isPlayerInside = false;

        // Variables añadidas para la detección de trayectoria y gestión de colisionadores
        private Vector3 direccionEntrada;
        private int collidersInsideCount = 0;
        private const float ANGULO_MINIMO_GIRO = 35f; // Grados mínimos para considerarlo un giro

        private void Update()
        {
            if (isPlayerInside)
            {
                // Registramos la velocidad más alta alcanzada en la curva
                if (currentSpeed.Value > maxSpeedInCurve)
                {
                    maxSpeedInCurve = currentSpeed.Value;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                collidersInsideCount++;

                // Solo inicializamos el monitoreo si es el primer colisionador del coche que entra
                if (collidersInsideCount == 1)
                {
                    isPlayerInside = true;
                    maxSpeedInCurve = 0f; // Reset al entrar

                    // Guardamos la dirección hacia adelante (forward) del chasis principal del coche al entrar
                    Transform rootVehicle = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
                    direccionEntrada = rootVehicle.forward;

                    Debug.Log("<color=white>SISTEMA:</color> Entrando en curva. Monitorizando velocidad...");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                collidersInsideCount--;

                // Solo evaluamos cuando el coche haya salido por completo (todos sus colisionadores salieron)
                if (collidersInsideCount <= 0)
                {
                    collidersInsideCount = 0; // Seguridad para evitar números negativos
                    isPlayerInside = false;

                    // Obtenemos la dirección hacia adelante del chasis al salir por completo
                    Transform rootVehicle = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
                    Vector3 direccionSalida = rootVehicle.forward;

                    // Calculamos los grados de diferencia entre la entrada y la salida
                    float anguloDeGiroEfectuado = Vector3.Angle(direccionEntrada, direccionSalida);

                    // Si el giro real supera el ángulo mínimo, evaluamos la curva
                    if (anguloDeGiroEfectuado >= ANGULO_MINIMO_GIRO)
                    {
                        Debug.Log($"<color=cyan>SISTEMA:</color> Giro detectado ({anguloDeGiroEfectuado:F1}°). Evaluando curva...");
                        EvaluarVelocidadFinal();
                    }
                    else
                    {
                        Debug.Log($"<color=green>SISTEMA:</color> Trayectoria recta detectada ({anguloDeGiroEfectuado:F1}°). Omitiendo evaluación.");
                    }
                }
            }
        }

        private void EvaluarVelocidadFinal()
        {
            if (infractionEvent == null) return;

            // Comparamos de mayor a menor gravedad
            if (maxSpeedInCurve >= limitEliminatoria)
            {
                if (ruleEliminatoria != null) infractionEvent.Raise(ruleEliminatoria);
                Debug.Log($"<color=red>DGT ELIMINATORIA:</color> Velocidad peligrosa en curva ({maxSpeedInCurve} km/h)");
            }
            else if (maxSpeedInCurve >= limitDeficiente)
            {
                if (ruleDeficiente != null) infractionEvent.Raise(ruleDeficiente);
                Debug.Log($"<color=orange>DGT DEFICIENTE:</color> Velocidad inadecuada en curva ({maxSpeedInCurve} km/h)");
            }
            else if (maxSpeedInCurve >= limitLeve)
            {
                if (ruleLeve != null) infractionEvent.Raise(ruleLeve);
                Debug.Log($"<color=yellow>DGT LEVE:</color> Velocidad algo superior a la aconsejable ({maxSpeedInCurve} km/h)");
            }
        }
    }
}