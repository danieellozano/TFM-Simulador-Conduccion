using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

namespace Simulador.AI
{
    public class SplineLink : MonoBehaviour
    {
        [Header("Opciones de Siguiente Tramo")]
        public List<SplineContainer> nextSplines;

        private void OnDrawGizmos()
        {
            // Solo dibujamos si hay conexiones y tenemos el componente Spline
            var currentSpline = GetComponent<SplineContainer>();
            if (currentSpline == null || nextSplines == null) return;

            Gizmos.color = Color.cyan;

            // Obtenemos la posición del ÚLTIMO punto de este carril en el mundo
            Vector3 endPoint = currentSpline.EvaluatePosition(1f);

            foreach (var next in nextSplines)
            {
                if (next != null)
                {
                    // Obtenemos la posición del PRIMER punto del siguiente carril
                    Vector3 startPoint = next.EvaluatePosition(0f);
                    
                    // Dibujamos la línea de conexión real
                    Gizmos.DrawLine(endPoint, startPoint);
                    
                    // Dibujamos una pequeña esfera en la unión
                    Gizmos.DrawSphere(endPoint, 0.2f);
                }
            }
        }
    }
}