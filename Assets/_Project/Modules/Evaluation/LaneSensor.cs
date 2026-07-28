using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class LaneSensor : MonoBehaviour
    {
        [Header("Configuración")]
        public LayerMask lineLayer;       // Selecciona la capa 'RoadLines'
        public GameEvent infractionEvent; // Arrastra 'OnInfractionDetected'
        public InfraccionSO lineInfraction; // Arrastra 'MAR-CONT'

        [Header("Ajustes")]
        public float detectionDistance = 0.5f; // Distancia del rayo hacia el suelo
        public float cooldown = 2.0f;          // Segundos para no repetir la multa
        
        private float lastInfractionTime;

        private void Update()
        {
            // Lanzamos un rayo desde el centro de la rueda hacia abajo
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, detectionDistance, lineLayer))
            {
                // Si el rayo toca algo en la capa 'RoadLines'
                RegistrarPisoLinea();
            }
        }

        private void RegistrarPisoLinea()
        {
            if (Time.time > lastInfractionTime + cooldown)
            {
                if (infractionEvent != null)
                {
                    infractionEvent.Raise(lineInfraction);
                    lastInfractionTime = Time.time;
                    Debug.Log("<color=orange>DGT:</color> Línea continua pisada detectada por " + gameObject.name);
                }
            }
        }
    }
}