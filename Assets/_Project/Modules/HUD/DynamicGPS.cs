using UnityEngine;
using UnityEngine.AI; 
using Simulador.Core;

namespace Simulador.HUD
{
    // Sistema de orientación dinámica y guiado espacial mediante interfaz de usuario (GPS/Mini-mapa).
    // Procesa el cálculo de rutas topológicas sobre la malla NavMesh para proyectar vectores de dirección
    // dinámicos sobre el plano local del Canvas UI, independizando la posición global de la cámara.
    public class DynamicGPS : MonoBehaviour
    {
        [Header("Referencias del Mundo")]
        [Tooltip("Transform del vehículo jugador utilizado como origen vectorial.")]
        public Transform playerTransform;      // Arrastra el Coche_Prototipo
        [Tooltip("Canal de datos SOA con la posición tridimensional del objetivo de navegación.")]
        public Vector3Variable gpsTargetSO;    // Arrastra CurrentGPSTarget.asset

        [Header("Referencia UI")]
        [Tooltip("Componente transformacional de la interfaz para la orientación del icono en pantalla.")]
        public RectTransform arrowIcon;        // Arrastra el RectTransform de la flecha

        // Buffer de memoria persistente para el cálculo de nodos de la ruta y prevención de basura (GC).
        private NavMeshPath path;

        // Inicializa el buffer de la ruta cinemática antes del ciclo de actualización.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        void Awake()
        {
            path = new NavMeshPath();
        }

        // Evalúa frame a frame las coordenadas del objetivo, resuelve el camino óptimo y actualiza el icono visual.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        void Update()
        {
            // 1. Evaluación del canal de datos SOA: si el vector es cero o nulo, se inhabilita el elemento gráfico
            if (gpsTargetSO == null || gpsTargetSO.Value == Vector3.zero)
            {
                if(arrowIcon.gameObject.activeSelf) arrowIcon.gameObject.SetActive(false);
                return;
            }

            if(!arrowIcon.gameObject.activeSelf) arrowIcon.gameObject.SetActive(true);

            // 2. Cálculo topológico del camino sobre la superficie transitable de la red vial (NavMesh)
            NavMesh.CalculatePath(playerTransform.position, gpsTargetSO.Value, NavMesh.AllAreas, path);

            // 3. Extracción del siguiente nodo espacial (corner) para la orientación progresiva
            if (path.corners.Length > 1)
            {
                // path.corners[0] representa el origen (posición actual)
                // path.corners[1] representa el siguiente vértice físico a alcanzar
                Vector3 targetPoint = path.corners[1];
                
                RotarFlecha(targetPoint);
            }
        }

        // Transforma un vector de posición 3D del mundo en un ángulo de rotación 2D sobre el eje Z del Canvas UI.
        // Parámetros:
        //   - target: Vector tridimensional con las coordenadas del punto de destino/vértice.
        // Salida: Ninguna.
        private void RotarFlecha(Vector3 target)
        {
            // Proyección del vector dirección en el plano horizontal (XZ)
            Vector3 direction = target - playerTransform.position;
            
            // Conversión trigonométrica del vector de dirección a ángulo polar en grados (respecto al Norte global)
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            // Compensación angular relativa a la orientación cinemática actual del vehículo
            float relativeAngle = angle - playerTransform.eulerAngles.y;

            // Mapeo del ángulo resultante sobre la rotación del elemento 2D en el espacio local del Canvas
            arrowIcon.localRotation = Quaternion.Euler(0, 0, -relativeAngle + 90);
        }
    }
}



