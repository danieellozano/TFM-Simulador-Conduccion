using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines; 

namespace Simulador.AI
{
    // Gestiona la prioridad de paso en intersecciones urbanas no semaforizadas.
    // Permite que los vehículos de la Inteligencia Artificial que realizan giros conflictivos
    // (como giros a la izquierda) cedan el paso de forma autónoma ante vehículos que circulan
    // en sentido opuesto con preferencia, empleando filtrado espacial y producto escalar.
    public class PriorityIntersection : MonoBehaviour
    {
        [Header("Zonas de Control")]
        [Tooltip("Volumen físico trigger donde esperan los vehículos que deben ceder el paso.")]
        public BoxCollider yieldZone;

        [Tooltip("Volumen físico que monitoriza el carril con prioridad de circulación en sentido contrario.")]
        public BoxCollider dangerZone;

        [Header("Filtro de Maniobra")]
        [Tooltip("Lista de Splines correspondientes a trayectorias sin prioridad (ej. giro a la izquierda).")]
        public List<SplineContainer> yieldingSplines; 

        [Header("Capas")]
        [Tooltip("Máscara de capas para detectar vehículos en la zona de conflicto (Player y Traffic).")]
        public LayerMask vehicleLayers;

        // Lista de vehículos de la IA que se encuentran actualmente dentro del área de espera
        private List<TrafficAIController> waitingCars = new List<TrafficAIController>();

        // Ciclo principal de actualización: evalúa el peligro en el carril prioritario
        // y aplica la orden de detención únicamente a los agentes cuyas rutas lo requieran.
        private void Update()
        {
            // 1. Comprobar si existe alguna amenaza activa aproximándose de frente por el carril prioritario
            bool carComingOpposite = CheckForDanger();

            // 2. Revisar cada coche de la IA presente en la zona de espera
            foreach (var ai in waitingCars)
            {
                if (ai != null)
                {
                    // Comprobar si el carril actual del vehículo corresponde a una maniobra de cesión de paso
                    bool needsToYield = yieldingSplines.Contains(ai.currentSpline);

                    if (needsToYield)
                    {
                        // Si el vehículo va a girar, detiene su marcha mientras la vía contraria esté ocupada
                        ai.externalStop = carComingOpposite;
                    }
                    else
                    {
                        // Si continúa en línea recta, no tiene conflicto y mantiene su avance libre
                        ai.externalStop = false;
                    }
                }
            }
        }

        // Evalúa la presencia de vehículos en la zona de peligro y utiliza el Producto Escalar
        // para discriminar si circulan de frente hacia la intersección o se están alejando.
        // Salida:
        //   True si se detecta un vehículo aproximándose en sentido opuesto; False en caso contrario.
        private bool CheckForDanger()
        {
            // Consulta espacial de superposición de cajas sobre el volumen del carril prioritario
            Collider[] obstacles = Physics.OverlapBox(
                dangerZone.bounds.center, 
                dangerZone.bounds.extents, 
                dangerZone.transform.rotation, 
                vehicleLayers
            );

            foreach (var obs in obstacles)
            {
                // Producto escalar entre el vector frontal del obstáculo y la orientación de la intersección
                float directionCheck = Vector3.Dot(obs.transform.forward, transform.forward);

                // Si el producto escalar es negativo (< -0.2), los vectores se oponen,
                // confirmando que el vehículo circula de frente en dirección al cruce (amenaza activa)
                if (directionCheck < -0.2f) return true;
            }

            return false; // Carril prioritario despejado
        }

        // Registra al vehículo de la IA en la lista de espera al entrar en el área de conflicto.
        // Parámetros:
        //   other: Colisionador del objeto que ingresa en el disparador.
        private void OnTriggerEnter(Collider other)
        {
            var ai = other.GetComponent<TrafficAIController>();
            if (ai != null && !waitingCars.Contains(ai))
            {
                waitingCars.Add(ai);
            }
        }

        // Libera la detención del vehículo y lo retira de la lista de espera al abandonar la intersección.
        // Parámetros:
        //   other: Colisionador del objeto que abandona el disparador.
        private void OnTriggerExit(Collider other)
        {
            var ai = other.GetComponent<TrafficAIController>();
            if (ai != null && waitingCars.Contains(ai))
            {
                ai.externalStop = false;
                waitingCars.Remove(ai);
            }
        }
    }
}