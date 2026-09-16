using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Simulador.AI
{
    // Gestiona de forma centralizada la detención obligatoria y la cesión de paso de los vehículos
    // de la Inteligencia Artificial en intersecciones reguladas por señales de STOP.
    // Implementa un modelo de colas FIFO, una barrera física en la capa AI_Blocker y un bucle
    // de observación limpia condicionado por análisis vectorial de amenazas en carriles transversales.
    public class AIStopManager : MonoBehaviour
    {
        [Header("Infraestructura de Bloqueo")]
        [Tooltip("Barrera física en la capa AI_Blocker que retiene a los vehículos en cola tras la línea de detención.")]
        public GameObject stopBar;

        [Header("Parámetros de Observación")]
        [Tooltip("Tiempo mínimo acumulativo (en segundos) de vía despejada requerido antes de reiniciar la marcha.")]
        public float mandatoryWaitTime = 3.0f;

        [Header("Percepción y Detección")]
        [Tooltip("Colisionadores espaciales que delimitan los carriles preferentes con riesgo de conflicto.")]
        public BoxCollider[] dangerZones;

        [Tooltip("Máscara de capas que filtra los vehículos dinámicos a supervisar (Player y Traffic).")]
        public LayerMask vehicleLayers;

        // Cola secuencial First-In, First-Out (FIFO) de agentes de IA esperando turno en el STOP
        private List<TrafficAIController> queue = new List<TrafficAIController>();

        // Bandera de control para evitar instancias concurrentes de la corrutina de gestión de la cola
        private bool isProcessingQueue = false;

        // Registra a los agentes de tráfico que ingresan en el área de aproximación a la señal de STOP.
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Traffic"))
            {
                TrafficAIController ai = other.GetComponent<TrafficAIController>();
                if (ai != null && !queue.Contains(ai))
                {
                    queue.Add(ai);

                    // Si no hay un ciclo de procesamiento activo, inicia la gestión de la cola
                    if (!isProcessingQueue)
                    {
                        StartCoroutine(ProcessQueue());
                    }
                }
            }
        }

        // Corrutina secuencial que orquesta las cuatro fases del protocolo de detención:
        // verificación de reposo, ventana de observación limpia, apertura de barrera y liberación del cruce.
        private IEnumerator ProcessQueue()
        {
            isProcessingQueue = true;

            while (queue.Count > 0)
            {
                TrafficAIController currentCar = queue[0];

                // Comprobación de seguridad en caso de que el objeto haya sido destruido durante la espera
                if (currentCar == null)
                {
                    queue.RemoveAt(0);
                    continue;
                }

                // Activa la barrera de retención para ordenar a los coches en cola detrás
                if (stopBar != null) stopBar.SetActive(true);

                // --- FASE 1: VERIFICACIÓN DE DETENCIÓN FÍSICA COMPLETA ---
                // Espera a que el vehículo de cabeza reduzca su velocidad por debajo del umbral de reposo (0.2 m/s)
                while (currentCar != null && currentCar.GetCurrentSpeed() > 0.2f)
                {
                    yield return null;
                }

                if (currentCar == null) continue;

                Debug.Log($"<color=cyan>IA STOP:</color> {currentCar.name} detenido. Iniciando protocolo de seguridad.");

                // --- FASE 2: BUCLE DE VIGILANCIA DINÁMICA Y OBSERVACIÓN LIMPIA ---
                float timer = 0f;

                // El bucle se prolonga hasta completar los segundos obligatorios ininterrumpidos de vía libre
                while (timer < mandatoryWaitTime || IsAnyThreatApproaching(currentCar.transform))
                {
                    // El temporizador ÚNICAMENTE incrementa si no se detecta ninguna amenaza aproximándose
                    if (!IsAnyThreatApproaching(currentCar.transform))
                    {
                        timer += Time.deltaTime;
                    }
                    else
                    {
                        // Si se aproxima un vehículo preferente, se congela el tiempo,
                        // forzando a la IA a reiniciar la observación cuando la vía quede despejada
                        timer = 0f;
                    }

                    yield return null; // Evaluación en cada fotograma
                }

                // --- FASE 3: APERTURA DE BARRERA Y REANUDACIÓN DE LA MARCHA ---
                if (stopBar != null) stopBar.SetActive(false);
                Debug.Log($"<color=green>IA STOP:</color> Vía libre para {currentCar.name}.");

                // --- FASE 4: VACIADO DE LA INTERSECCIÓN Y TIEMPO DE CORTESÍA ---
                // Espera a que el vehículo abandone el área del STOP o se alcance un margen de seguridad de 5 segundos
                float timeout = 0f;
                while (queue.Count > 0 && queue[0] == currentCar && timeout < 5.0f)
                {
                    timeout += Time.deltaTime;
                    yield return null;
                }

                // Vuelve a desplegar la barrera para retener al siguiente vehículo de la cola
                if (stopBar != null) stopBar.SetActive(true);
            }

            isProcessingQueue = false;
        }

        // Retira al vehículo de la cola cuando abandona físicamente el volumen del disparador.
        private void OnTriggerExit(Collider other)
        {
            TrafficAIController ai = other.GetComponent<TrafficAIController>();
            if (ai != null && queue.Contains(ai))
            {
                queue.Remove(ai);
            }
        }

        // Evalúa las zonas de conflicto transversal mediante barridos espaciales y producto escalar,
        // discriminando si los vehículos detectados se aproximan hacia el cruce o se alejan de él.
        // Parámetros:
        //   currentCar: Transform del vehículo detenido en la línea de parada.
        // Salida:
        //   True si existe una amenaza activa aproximándose; False en caso contrario.
        private bool IsAnyThreatApproaching(Transform currentCar)
        {
            foreach (var zone in dangerZones)
            {
                if (zone == null) continue;

                // Consulta de superposición de cajas sobre la capa de vehículos (Player y Traffic)
                Collider[] vehicles = Physics.OverlapBox(
                    zone.bounds.center,
                    zone.bounds.extents,
                    zone.transform.rotation,
                    vehicleLayers
                );

                foreach (var v in vehicles)
                {
                    // Ignora el propio vehículo evaluado
                    if (v.transform.root == currentCar.root) continue;

                    if (v.CompareTag("Player") || v.CompareTag("Traffic"))
                    {
                        // Vector de dirección desde el vehículo detectado hacia el punto de detención del STOP
                        Vector3 directionToStop = transform.position - v.transform.position;

                        // Producto escalar entre el vector de avance frontal del vehículo y la dirección hacia el cruce
                        float approachCheck = Vector3.Dot(v.transform.forward, directionToStop.normalized);

                        // Si el producto escalar es positivo (> 0.1), el vehículo se aproxima de forma activa hacia la intersección
                        if (approachCheck > 0.1f)
                        {
                            return true; // Amenaza confirmada
                        }
                        // Si es negativo o cercano a cero, el vehículo circula en sentido opuesto o se está alejando
                    }
                }
            }

            return false; // Vía transversal libre de amenazas
        }
    }
}