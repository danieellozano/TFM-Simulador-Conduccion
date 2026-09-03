using UnityEngine;
using Simulador.Core;

namespace Simulador.Physics
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Referencias de Datos")]
        [Tooltip("Arrastra aquí el activo de datos del simulador (CurrentInput.asset)")]
        public InputDataSO inputData; 

        [Header("Configuración de Rotación")]
        [Tooltip("Velocidad de rotación horizontal de la cámara.")]
        public float rotationSpeed = 110f;
        [Tooltip("Velocidad de retorno a la posición de mirada al frente.")]
        public float resetSpeed = 150f;

        [Header("Límites de Ángulo (Grados)")]
        public float minRotationY = -85f;
        public float maxRotationY = 85f;

        private float currentRotationY = 0f;
        private bool isResetting = false;
        private Quaternion originalRotation;

        private void Start()
        {
            // Guardamos la rotación local inicial configurada en el inspector
            originalRotation = transform.localRotation;
            
            currentRotationY = transform.localEulerAngles.y;
            if (currentRotationY > 180f) currentRotationY -= 360f;
        }

        private void Update()
        {
            if (inputData == null) return;

            // Leemos de forma pasiva y desacoplada las señales de la capa de datos
            float lookValue = inputData.Look;
            bool pressedReset = inputData.LookReset;

            // 1. Activar el retorno automático de la mirada
            if (pressedReset)
            {
                isResetting = true;
            }

            // 2. Si el conductor aplica rotación manual, se desactiva el centrado automático
            if (lookValue != 0f)
            {
                isResetting = false;
            }

            // 3. Ejecución de la rotación
            if (isResetting)
            {
                // Retornamos suavemente hacia la alineación frontal original
                transform.localRotation = Quaternion.RotateTowards(
                    transform.localRotation, 
                    originalRotation, 
                    resetSpeed * Time.deltaTime
                );

                // Sincronizamos la variable interna de rotación Y
                currentRotationY = transform.localEulerAngles.y;
                if (currentRotationY > 180f) currentRotationY -= 360f;

                if (Quaternion.Angle(transform.localRotation, originalRotation) < 0.1f)
                {
                    transform.localRotation = originalRotation;
                    isResetting = false;
                }
            }
            else
            {
                // Control manual lateral basado en datos normalizados
                if (lookValue != 0f)
                {
                    currentRotationY += lookValue * rotationSpeed * Time.deltaTime;
                    currentRotationY = Mathf.Clamp(currentRotationY, minRotationY, maxRotationY);

                    // Aplicamos la rotación local sobre el eje Y conservando los valores de X y Z originales
                    transform.localRotation = Quaternion.Euler(
                        originalRotation.eulerAngles.x, 
                        currentRotationY, 
                        originalRotation.eulerAngles.z
                    );
                }
            }
        }
    }
}