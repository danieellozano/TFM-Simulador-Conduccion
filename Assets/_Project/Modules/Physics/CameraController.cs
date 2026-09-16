using UnityEngine;
using Simulador.Core;

namespace Simulador.Physics
{
    // Controlador de la perspectiva de visión y exploración de la cámara del conductor.
    // Procesa las entradas de rotación pasiva y centrado de mirada desde la capa de datos (InputDataSO),
    // delimitando los rangos de giro angular para emular la biomecánica del cuello y la observación de retrovisores.
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
        [Tooltip("Ángulo límite de giro horizontal hacia la izquierda.")]
        public float minRotationY = -85f;
        [Tooltip("Ángulo límite de giro horizontal hacia la derecha.")]
        public float maxRotationY = 85f;

        // Orientación Y acumulada para la restricción dinámica de ángulos.
        private float currentRotationY = 0f;
        // Estado indicador del centrado automático activo hacia la alineación frontal.
        private bool isResetting = false;
        // Rotación local inicial almacenada como referencia para el centrado.
        private Quaternion originalRotation;

        // Registra la rotación local por defecto y formatea la orientación en el rango [-180, 180].
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void Start()
        {
            // Guardamos la rotación local inicial configurada en el inspector
            originalRotation = transform.localRotation;
            
            currentRotationY = transform.localEulerAngles.y;
            if (currentRotationY > 180f) currentRotationY -= 360f;
        }

        // Muestrea los comandos de visión del conductor y aplica la rotación delimitada o la interpolación de centrado.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
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