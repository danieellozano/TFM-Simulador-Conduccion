using UnityEngine;
using UnityEngine.Splines;
using Simulador.Core;

namespace Simulador.AI
{
    public class TrafficAIController : MonoBehaviour
    {
        [Header("Navegación")]
        public SplineContainer currentSpline;
        public float currentRoadLimit = 30f; 

        [Header("Sensores de Percepción")]
        public float detectionDistance = 20f;
        public float stopDistanceAtLine = 2.3f;  // Distancia para Semáforo/STOP
        public float stopDistanceBehindCar = 6.0f; // Distancia de seguridad tras otro coche
        public LayerMask obstacleLayers;

        [Header("Ajustes de Conducción")]
        public float brakeSmoothness = 5f;
        public float accelerationSmoothness = 1.5f;

        [Header("Ajustes de Curva")]
        [Tooltip("Metros que la IA mira hacia adelante para detectar curvas")]
        public float lookAheadDistance = 4f; 
        [Tooltip("Multiplicador de velocidad mínima en curvas (0.3 = 30% de la velocidad)")]
        [Range(0.1f, 1f)] public float curveSpeedFactor = 0.5f;

        [HideInInspector] public bool externalStop = false;

        private float progress = 0f; 
        private float currentActualSpeed = 0f;
        private float targetSpeed = 0f;

        private void Start()
        {
            targetSpeed = currentRoadLimit;
            if (currentSpline != null)
                transform.position = (Vector3)currentSpline.EvaluatePosition(0f);
        }

        private void Update()
        {
            if (currentSpline == null) return;

            HandlePerception();
            HandleMovement();
        }

        private void HandlePerception()
        {
            RaycastHit hit;
            bool hitDetected = Physics.Raycast(
                transform.position + Vector3.up * 0.5f, 
                transform.forward, 
                out hit, 
                detectionDistance, 
                obstacleLayers, 
                QueryTriggerInteraction.Collide 
            );

            if (hitDetected || externalStop)
            {
                if (externalStop)
                {
                    targetSpeed = 0f;
                }
                else
                {
                    // --- LÓGICA DINÁMICA DE DISTANCIA ---
                    // Detectamos si el objeto hit es un bloqueador de IA (Capa AI_Blocker)
                    int aiBlockerLayer = LayerMask.NameToLayer("AI_Blocker");
                    float currentRequiredStopDistance = (hit.collider.gameObject.layer == aiBlockerLayer) 
                        ? stopDistanceAtLine 
                        : stopDistanceBehindCar;

                    if (hit.distance > currentRequiredStopDistance)
                    {
                        // Frenada progresiva
                        targetSpeed = (hit.distance / detectionDistance) * currentRoadLimit;
                    }
                    else
                    {
                        targetSpeed = 0f;
                    }
                }
                Debug.DrawLine(transform.position + Vector3.up * 0.5f, hit.point, Color.red);
            }
            else
            {
                targetSpeed = CalculateCurveSpeed();
                Debug.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * detectionDistance, Color.green);
            }
        }
        
        private float CalculateCurveSpeed()
        {
            float length = currentSpline.CalculateLength();
            // Calculamos un punto unos metros por delante en el spline
            float lookAheadProgress = progress + (lookAheadDistance / length);
            
            if (lookAheadProgress > 1.0f) lookAheadProgress = 1.0f;

            Vector3 currentTangent = (Vector3)currentSpline.EvaluateTangent(progress);
            Vector3 futureTangent = (Vector3)currentSpline.EvaluateTangent(lookAheadProgress);

            // Calculamos el ángulo de giro entre nuestra posición y el futuro
            float angle = Vector3.Angle(currentTangent, futureTangent);

            // Si el ángulo es mayor de 5 grados, reducimos la velocidad
            if (angle > 5f)
            {
                // A más ángulo, menos velocidad (mínimo el curveSpeedFactor)
                float reduction = Mathf.Clamp(1.0f - (angle / 90f), curveSpeedFactor, 1.0f);
                return currentRoadLimit * reduction;
            }

            return currentRoadLimit;
        }

        private void HandleMovement()
        {
            float lerpSpeed = (targetSpeed > currentActualSpeed) ? accelerationSmoothness : brakeSmoothness;
            currentActualSpeed = Mathf.Lerp(currentActualSpeed, targetSpeed, Time.deltaTime * lerpSpeed);

            if (currentActualSpeed > 0.1f)
            {
                float length = currentSpline.CalculateLength();
                progress += (currentActualSpeed / 3.6f) * Time.deltaTime / length;

                if (progress >= 1.0f)
                {
                    ChangeToNextSpline();
                }

                transform.position = (Vector3)currentSpline.EvaluatePosition(progress);
                Vector3 tangent = (Vector3)currentSpline.EvaluateTangent(progress);
                
                if (tangent != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(tangent);
            }
        }

        private void ChangeToNextSpline()
        {
            SplineLink link = currentSpline.GetComponent<SplineLink>();
            if (link != null && link.nextSplines.Count > 0)
            {
                currentSpline = link.nextSplines[Random.Range(0, link.nextSplines.Count)];
                progress = 0f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Lectura dinámica de señales de velocidad
            ISpeedLimitProvider speedLimitProvider = other.GetComponent<ISpeedLimitProvider>(); 
            if (speedLimitProvider != null)
            {
                currentRoadLimit = speedLimitProvider.GetSpeedLimit();
            }
        }

        public float GetCurrentSpeed()
        {
            return currentActualSpeed;
        }
    }
}