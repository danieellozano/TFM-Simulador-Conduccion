using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Monitoriza la invasión y el franqueamiento no permitido de marcas viales longitudinales continuas (infracción MAR-CON).
    // Conforme al Reglamento General de Conductores de la DGT, pisar o rebasar de forma injustificada una línea continua
    // constituye una falta de gravedad Deficiente. 
    // Para evitar registros redundantes continuados mientras el neumático rueda sobre la misma marca vial, el sensor
    // incorpora un algoritmo de persistencia por puntero de colisionador (lastLineCollider) que limita la sanción a un único error.
    public class LaneSensor : MonoBehaviour
    {
        [Header("Referencias SOA")]
        [Tooltip("Máscara de capas física que aísla exclusivamente los colisionadores de señalización horizontal (RoadLines).")]
        public LayerMask lineLayer;       

        [Tooltip("Canal del bus de eventos globales para notificar la falta deficiente.")]
        public GameEvent infractionEvent; 

        [Tooltip("Activo de datos que parametriza la infracción por pisar línea continua (MAR-CON).")]
        public InfraccionSO lineInfraction; 

        [Header("Ajustes de Sensor")]
        [Tooltip("Distancia vertical máxima de proyección del rayo descendente desde el eje de rodadura.")]
        public float detectionDistance = 0.5f; 

        // Puntero de memoria que almacena la instancia del colisionador de la línea que se está pisando actualmente
        private Collider lastLineCollider; 

        // Bucle de actualización en el que se ejecuta la consulta espacial descendente y la lógica de deduplicación
        private void Update()
        {
            RaycastHit hit;
            
            // Proyección del rayo lineal vertical:
            // Origen: Coordenadas locales del centro de la rueda | Dirección: Vector descendente | Capa: RoadLines
            bool hitDetected = Physics.Raycast(transform.position, Vector3.down, out hit, detectionDistance, lineLayer);
        
            // --- LÓGICA DE DETECCIÓN Y FILTRADO POR INSTANCIA DE OBJETO ---
            if (hitDetected)
            {
                // Solo se registra una nueva falta si el colisionador impactado es diferente al del fotograma anterior
                if (hit.collider != lastLineCollider)
                {
                    lastLineCollider = hit.collider;
                    RegistrarInfraccion();
                }
                // Si es el mismo colisionador, el vehículo sigue sobre la misma línea y se omite el registro para evitar spam
            }
            else
            {
                // Al salir de la marca vial hacia el asfalto despejado, se limpia la memoria del puntero
                // Esto prepara de forma segura al sensor para registrar una nueva invasión si se pisa otra línea en el futuro
                lastLineCollider = null;
            }
        }

        // Emite la notificación de falta deficiente al bus de eventos y registra el suceso en la consola de depuración
        private void RegistrarInfraccion()
        {
            if (infractionEvent != null)
            {
                infractionEvent.Raise(lineInfraction);
                Debug.Log($"<color=orange>DGT:</color> Invasión (Raycast) detectada por {gameObject.name} en: {lastLineCollider.name}");
            }
        }

        // Dibuja en la vista de escena de Unity un prisma volumétrico para representar visualmente el alcance y estado del Raycast
        private void OnDrawGizmos()
        {
            // Código de color dinámico: Rojo si el rayo intersecta con una línea activa; Verde si la rodadura está libre
            Gizmos.color = (lastLineCollider != null) ? Color.red : Color.green;

            // Centro geométrico del haz vertical (punto medio del intervalo descendente)
            Vector3 centroRayo = transform.position + Vector3.down * (detectionDistance * 0.5f);

            // Dimensiones del prisma volumétrico: grosor de 2 cm en X/Z y la altura exacta de la distancia de detección en Y
            Vector3 dimensionesRayo = new Vector3(0.02f, detectionDistance, 0.02f);

            // Representación tridimensional sólida del haz de inspección en el editor
            Gizmos.DrawCube(centroRayo, dimensionesRayo);
        }
    }
}