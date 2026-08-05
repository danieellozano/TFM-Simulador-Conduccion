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
    }
}