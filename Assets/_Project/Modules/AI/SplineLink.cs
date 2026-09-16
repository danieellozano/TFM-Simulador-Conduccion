using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

namespace Simulador.AI
{
    // Modela los nodos de interconexión y ramificación en la red vial de Splines.
    // Actúa como un conector de grafo dirigido a nivel de carril, permitiendo que
    // los vehículos autónomos de la IA seleccionen aleatoriamente entre múltiples
    // tramos de destino disponibles al alcanzar el final de su trayectoria actual.
    public class SplineLink : MonoBehaviour
    {
        [Header("Opciones de Siguiente Tramo")]
        [Tooltip("Lista de Splines candidatos a los que puede transicionar un vehículo al finalizar este tramo.")]
        public List<SplineContainer> nextSplines;

        // Dibuja ayudas visuales en la vista de Escena del Editor de Unity para facilitar
        // el diseño de la red vial y verificar la continuidad espacial entre carriles.
        private void OnDrawGizmos()
        {
            // Obtiene el contenedor de Spline asociado a este objeto
            var currentSpline = GetComponent<SplineContainer>();
            if (currentSpline == null) return;

            // 1. DIBUJAR INICIO DE CARRIL (Esfera Verde en t = 0)
            // Representa el punto de entrada donde los vehículos inician el recorrido del tramo
            Gizmos.color = Color.green;
            Vector3 startPointSelf = currentSpline.EvaluatePosition(0f);
            Gizmos.DrawSphere(startPointSelf, 1.5f);

            // 2. DIBUJAR FINAL DE CARRIL (Esfera Roja en t = 1)
            // Representa el punto terminal donde el vehículo consulta este script para cambiar de Spline
            Gizmos.color = Color.red;
            Vector3 endPointSelf = currentSpline.EvaluatePosition(1f);
            Gizmos.DrawSphere(endPointSelf, 0.8f);

            // 3. DIBUJAR ENLACES DE CONEXIÓN (Líneas Cian)
            // Traza vectores visuales desde el punto de salida hacia el punto de entrada de cada carril de destino
            if (nextSplines == null) return;

            Gizmos.color = Color.cyan;
            foreach (var next in nextSplines)
            {
                if (next != null)
                {
                    // Evalúa las coordenadas del primer punto (t = 0) del siguiente carril candidato
                    Vector3 startPointNext = next.EvaluatePosition(0f);
                    
                    // Dibuja la línea de conexión para comprobar la continuidad topológica
                    Gizmos.DrawLine(endPointSelf, startPointNext);
                }
            }
        }
    }
}