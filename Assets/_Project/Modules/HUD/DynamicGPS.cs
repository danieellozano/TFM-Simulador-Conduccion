using UnityEngine;
using UnityEngine.AI; 
using Simulador.Core;

namespace Simulador.HUD
{
    public class DynamicGPS : MonoBehaviour
    {
        [Header("Referencias del Mundo")]
        public Transform playerTransform;      // Arrastra el Coche_Prototipo
        public Vector3Variable gpsTargetSO;    // Arrastra CurrentGPSTarget.asset

        [Header("Referencia UI")]
        public RectTransform arrowIcon;        // Arrastra el RectTransform de la flecha

        private NavMeshPath path;

        void Awake()
        {
            path = new NavMeshPath();
        }

        void Update()
        {
            // 1. Si no hay destino, ocultamos la flecha
            if (gpsTargetSO == null || gpsTargetSO.Value == Vector3.zero)
            {
                if(arrowIcon.gameObject.activeSelf) arrowIcon.gameObject.SetActive(false);
                return;
            }

            if(!arrowIcon.gameObject.activeSelf) arrowIcon.gameObject.SetActive(true);

            // 2. Calculamos la ruta por las calles (NavMesh)
            NavMesh.CalculatePath(playerTransform.position, gpsTargetSO.Value, NavMesh.AllAreas, path);

            // 3. Si hay una ruta válida, apuntamos a la primera esquina
            if (path.corners.Length > 1)
            {
                // path.corners[0] es donde estamos ahora.
                // path.corners[1] es la siguiente esquina a la que debemos ir.
                Vector3 targetPoint = path.corners[1];
                
                RotarFlecha(targetPoint);
            }
        }

        private void RotarFlecha(Vector3 target)
        {
            // Calculamos la dirección desde el coche al punto en el plano XZ (suelo)
            Vector3 direction = target - playerTransform.position;
            
            // Calculamos el ángulo en grados usando Atan2
            // En Unity, Atan2(x, z) nos da el ángulo respecto al norte del mundo
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            // IMPORTANTE: Restamos la rotación actual del coche para que el ángulo sea RELATIVO
            // Si el coche gira, la flecha debe compensar ese giro para seguir apuntando al sitio
            float relativeAngle = angle - playerTransform.eulerAngles.y;

            // Aplicamos la rotación a la flecha del HUD
            // En UI, la rotación se hace sobre el eje Z
            arrowIcon.localRotation = Quaternion.Euler(0, 0, -relativeAngle + 90);
        }
    }
}