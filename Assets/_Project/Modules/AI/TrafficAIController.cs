using UnityEngine;
using UnityEngine.Splines;
using Simulador.Core;

namespace Simulador.AI
{
    // Controlador cinemático para los vehículos autónomos del tráfico urbano.
    // Gobierna el avance sobre curvas Spline, la percepción frontal mediante Raycasting,
    // la deceleración predictiva en curvas, el respeto a límites de velocidad y la
    // detención ante obstáculos o barreras de infraestructura vial (semáforos y STOPs).
    public class TrafficAIController : MonoBehaviour
    {
        [Header("Navegación")]
        [Tooltip("Spline sobre el que circula actualmente el vehículo.")]
        public SplineContainer currentSpline;
        [Tooltip("Velocidad máxima legal de la vía actual en km/h.")]
        public float currentRoadLimit = 30f; 

        [Header("Sensores de Percepción")]
        [Tooltip("Alcance máximo en metros del rayo frontal para detectar obstáculos.")]
        public float detectionDistance = 20f;
        [Tooltip("Distancia mínima de detención ante líneas de parada (semáforos o STOPs).")]
        public float stopDistanceAtLine = 2.3f;
        [Tooltip("Distancia de seguridad mínima que se mantendrá respecto al vehículo precedente.")]
        public float stopDistanceBehindCar = 6.0f;
        [Tooltip("Máscara de capas que define los elementos a detectar (Player, Traffic, AI_Blocker).")]
        public LayerMask obstacleLayers;

        [Header("Ajustes de Conducción")]
        [Tooltip("Factor de suavizado para dosificar la fuerza de frenada.")]
        public float brakeSmoothness = 5f;
        [Tooltip("Factor de suavizado para dosificar la aceleración del motor.")]
        public float accelerationSmoothness = 1.5f;

        [Header("Ajustes de Curva")]
        [Tooltip("Metros que la IA evalúa hacia adelante en el spline para anticipar curvas.")]
        public float lookAheadDistance = 4f; 
        [Tooltip("Multiplicador de velocidad mínima en curvas (ej. 0.5 = 50% de la velocidad de la vía).")]
        [Range(0.1f, 1f)] public float curveSpeedFactor = 0.5f;

        // Bandera para forzar la detención desde gestores externos (intersecciones o STOPs)
        [HideInInspector] public bool externalStop = false;

        // Coordenada normalizada de progreso en el spline actual [0.0 a 1.0]
        private float progress = 0f; 
        // Velocidad real calculada en km/h tras aplicar suavizado inercial
        private float currentActualSpeed = 0f;
        // Velocidad objetivo hacia la que tiende el vehículo
        private float targetSpeed = 0f;

        private void Start()
        {
            targetSpeed = currentRoadLimit;
            // Ubica el vehículo al inicio del spline asignado en la escena
            if (currentSpline != null)
                transform.position = (Vector3)currentSpline.EvaluatePosition(0f);
        }

        private void Update()
        {
            if (currentSpline == null) return;

            HandlePerception();
            HandleMovement();
        }

        // Realiza un barrido frontal continuo mediante Raycasting para detectar amenazas u obstáculos.
        // Diferencia entre colisiones contra vehículos y barreras lógicas de infraestructura (AI_Blocker).
        private void HandlePerception()
        {
            RaycastHit hit;
            // Proyección del rayo a 0.5 metros del suelo para coincidir con la altura del parachoques
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
                // Prioridad absoluta: orden de detención forzada por un gestor de cruce
                if (externalStop)
                {
                    targetSpeed = 0f;
                }
                else
                {
                    // LÓGICA DINÁMICA DE DISTANCIA
                    // Si es una barrera invisible de tráfico, se aproxima más que si es un vehículo
                    int aiBlockerLayer = LayerMask.NameToLayer("AI_Blocker");
                    float currentRequiredStopDistance = (hit.collider.gameObject.layer == aiBlockerLayer) 
                        ? stopDistanceAtLine 
                        : stopDistanceBehindCar;

                    if (hit.distance > currentRequiredStopDistance)
                    {
                        // Frenada progresiva proporcional al porcentaje de distancia restante
                        targetSpeed = (hit.distance / detectionDistance) * currentRoadLimit;
                    }
                    else
                    {
                        // Inmovilización total al alcanzar la distancia mínima de seguridad
                        targetSpeed = 0f;
                    }
                }
                // Debug visual: rayo rojo cuando detecta un elemento que obliga a frenar
                Debug.DrawLine(transform.position + Vector3.up * 0.5f, hit.point, Color.red);
            }
            else
            {
                // Vía despejada: calcula la velocidad óptima en función del trazado de la carretera
                targetSpeed = CalculateCurveSpeed();
                // Debug visual: rayo verde cuando la trayectoria está libre de amenazas
                Debug.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * detectionDistance, Color.green);
            }
        }
        
        // Evalúa la curvatura geométrica del trazado mediante la comparación vectorial de tangentes,
        // reduciendo la velocidad si el ángulo de giro excede el umbral de tolerancia.
        // Salida:
        //   Velocidad máxima recomendada para el tramo en km/h.
        private float CalculateCurveSpeed()
        {
            float length = currentSpline.CalculateLength();
            // Proyecta un punto en el spline a una distancia definida por delante de la posición actual
            float lookAheadProgress = progress + (lookAheadDistance / length);
            
            if (lookAheadProgress > 1.0f) lookAheadProgress = 1.0f;

            // Vectores tangentes de la trayectoria actual y de la posición anticipada
            Vector3 currentTangent = (Vector3)currentSpline.EvaluateTangent(progress);
            Vector3 futureTangent = (Vector3)currentSpline.EvaluateTangent(lookAheadProgress);

            // Ángulo de desviación en grados entre la dirección presente y la futura
            float angle = Vector3.Angle(currentTangent, futureTangent);

            // Si el trazado gira más de 5 grados, aplica una reducción lineal inversa de velocidad
            if (angle > 5f)
            {
                float reduction = Mathf.Clamp(1.0f - (angle / 90f), curveSpeedFactor, 1.0f);
                return currentRoadLimit * reduction;
            }

            // Trazado rectilíneo: velocidad legal completa de la vía
            return currentRoadLimit;
        }

        // Aplica el modelo cinemático de traslación y rotación sobre el Spline,
        // simulando inercia de aceleración y frenado mediante interpolación lineal.
        private void HandleMovement()
        {
            // Aplica factores asimétricos para que la frenada sea más enérgica que la aceleración
            float lerpSpeed = (targetSpeed > currentActualSpeed) ? accelerationSmoothness : brakeSmoothness;
            currentActualSpeed = Mathf.Lerp(currentActualSpeed, targetSpeed, Time.deltaTime * lerpSpeed);

            // Umbral mínimo de movimiento para evitar oscilaciones residuales en parada
            if (currentActualSpeed > 0.1f)
            {
                float length = currentSpline.CalculateLength();
                // Conversión de km/h a m/s dividiendo por 3.6 para normalizar el progreso temporal
                progress += (currentActualSpeed / 3.6f) * Time.deltaTime / length;

                // Al alcanzar el final del carril actual, conmuta al siguiente tramo
                if (progress >= 1.0f)
                {
                    ChangeToNextSpline();
                }

                // Sincroniza la posición y orientación física con la curva paramétrica
                transform.position = (Vector3)currentSpline.EvaluatePosition(progress);
                Vector3 tangent = (Vector3)currentSpline.EvaluateTangent(progress);
                
                if (tangent != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(tangent);
            }
        }

        // Gestiona la transición hacia un nuevo tramo de carril consultando el componente SplineLink.
        // Selecciona aleatoriamente entre los carriles de salida disponibles y reinicia el progreso a cero.
        private void ChangeToNextSpline()
        {
            SplineLink link = currentSpline.GetComponent<SplineLink>();
            if (link != null && link.nextSplines.Count > 0)
            {
                currentSpline = link.nextSplines[Random.Range(0, link.nextSplines.Count)];
                progress = 0f;
            }
        }

        // Captura el paso por señales de tráfico verticales para actualizar dinámicamente el límite legal.
        // Parámetros:
        //   other: Colisionador del volumen de la señal atravesada.
        private void OnTriggerEnter(Collider other)
        {
            // Lectura desacoplada del nuevo límite de velocidad mediante interfaz
            ISpeedLimitProvider speedLimitProvider = other.GetComponent<ISpeedLimitProvider>(); 
            if (speedLimitProvider != null)
            {
                currentRoadLimit = speedLimitProvider.GetSpeedLimit();
            }
        }

        // Expone la velocidad actual del vehículo para su consulta por sensores externos y gestores de colas.
        // Salida:
        //   Velocidad lineal instantánea en km/h.
        public float GetCurrentSpeed()
        {
            return currentActualSpeed;
        }
    }
}