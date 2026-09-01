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
            // Solo dibujamos si tenemos el componente Spline
            var currentSpline = GetComponent<SplineContainer>();
            if (currentSpline == null) return;

            // 1. DIBUJAR INICIO DE ESTE CARRIL (Esfera Verde de Entrada)
            Gizmos.color = Color.green;
            Vector3 startPointSelf = currentSpline.EvaluatePosition(0f);
            Gizmos.DrawSphere(startPointSelf, 1.5f);

            // 2. DIBUJAR FINAL DE ESTE CARRIL (Esfera Roja de Salida)
            Gizmos.color = Color.red;
            Vector3 endPointSelf = currentSpline.EvaluatePosition(1f);
            Gizmos.DrawSphere(endPointSelf, 0.0f);

            // 3. DIBUJAR CONEXIONES (Líneas de enlace en color Cian)
            if (nextSplines == null) return;

            Gizmos.color = Color.cyan;
            foreach (var next in nextSplines)
            {
                if (next != null)
                {
                    // Obtenemos la posición del primer punto del siguiente carril (su esfera verde)
                    Vector3 startPointNext = next.EvaluatePosition(0f);
                    
                    // Dibujamos la línea de conexión desde nuestra salida hacia su entrada
                    Gizmos.DrawLine(endPointSelf, startPointNext);
                }
            }
        }
    }
}