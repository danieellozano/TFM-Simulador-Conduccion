using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class LaneSensor : MonoBehaviour
    {
        [Header("Referencias SOA")]
        public LayerMask lineLayer;       
        public GameEvent infractionEvent; 
        public InfraccionSO lineInfraction; 

        [Header("Ajustes de Sensor")]
        public float detectionDistance = 0.5f; 

        // Guardamos el objeto que estamos pisando actualmente
        private Collider lastLineCollider; 

        private void Update()
        {
            RaycastHit hit;
            
            // LANZAMOS EL RAYCAST (Línea fina vertical)
            // Origen: centro de la rueda | Dirección: Abajo | Capa: RoadLines
            bool hitDetected = Physics.Raycast(transform.position, Vector3.down, out hit, detectionDistance, lineLayer);
        
            // LOGICA DE DETECCIÓN POR OBJETO
            if (hitDetected)
            {
                // Si el colisionador que toca el rayo es DIFERENTE al anterior
                if (hit.collider != lastLineCollider)
                {
                    lastLineCollider = hit.collider;
                    RegistrarInfraccion();
                }
            }
            else
            {
                // Si el rayo ya no toca ninguna línea, limpiamos la memoria
                lastLineCollider = null;
            }
        }

        private void RegistrarInfraccion()
        {
            if (infractionEvent != null)
            {
                infractionEvent.Raise(lineInfraction);
                Debug.Log($"<color=orange>DGT:</color> Invasión (Raycast) detectada por {gameObject.name} en: {lastLineCollider.name}");
            }
        }
        // Dibuja un haz de luz láser sólido y coloreado para representar de forma visible el Raycast
        private void OnDrawGizmos()
        {
            // Determinamos el color: rojo si está pisando una línea (memoria activa), verde si está libre
            Gizmos.color = (lastLineCollider != null) ? Color.red : Color.green;

            // Calculamos el centro exacto del rayo vertical (punto medio del recorrido descendente)
            Vector3 centroRayo = transform.position + Vector3.down * (detectionDistance * 0.5f);

            // Creamos un prisma de 2 cm de grosor (X y Z) y con la altura de la distancia de detección (Y)
            Vector3 dimensionesRayo = new Vector3(0.02f, detectionDistance, 0.02f);

            // Dibujamos el prisma sólido en la escena (simulando un haz de luz láser)
            Gizmos.DrawCube(centroRayo, dimensionesRayo);
        }
    }
}