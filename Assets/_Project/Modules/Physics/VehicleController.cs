using UnityEngine;
using Simulador.Core;

namespace Simulador.PhysicsModule
{
    // Controlador principal de la dinámica vehicular del simulador.
    // Simula el tren motriz (motor térmico, transmisión manual y automática), la entrega de par, el frenado,
    // el ángulo de dirección y las condiciones de calado, sincronizando el estado físico con la capa SOA.
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {
        [Header("Canales de Datos (SOA)")]
        [Tooltip("Buffer de entrada normalizado proveniente de la capa de abstracción de hardware (HAL).")]
        public InputDataSO inputData;
        [Tooltip("Variable reactiva en ScriptableObject que registra la velocidad lineal en km/h.")]
        public FloatVariable currentSpeedVariable;
        [Tooltip("Variable reactiva en ScriptableObject que registra el índice de la marcha activa.")]
        public IntVariable currentGearVariable;
        [Tooltip("Variable reactiva en ScriptableObject que almacena el límite de velocidad normativo.")]
        public FloatVariable currentSpeedLimitVariable;
        [Tooltip("Estado reactivo de la condición de calado del motor térmico.")]
        public BoolVariable isStalledSO; 
        [Tooltip("Variable booleana en ScriptableObject que define el tipo de transmisión activa.")]
        public BoolVariable isAutomaticSO;

        [Header("Física y Potencia")]
        [Tooltip("Fuerza base de aceleración aplicada al motor.")]
        public float motorForce = 350f; 
        [Tooltip("Fuerza de frenado hidráulico aplicada sobre los discos de las ruedas.")]
        public float brakeForce = 8000;
        [Tooltip("Ángulo máximo de giro angular del eje delantero de dirección en grados.")]
        public float maxSteerAngle = 33f;
        [Tooltip("Fuerza de retención por freno motor al soltar el acelerador.")]
        public float engineBrakeForce = 600f; 

        [Header("Transmisión Manual")]
        [Tooltip("Relaciones de cambio físicas para la marcha atrás (-1), punto muerto (0) y marchas de avance.")]
        public float[] gearRatios = { -3.4f, 0f, 3.5f, 1.9f, 1.3f, 0.95f, 0.78f }; 
        [Tooltip("Indica si el motor térmico se encuentra en estado calado o apagado.")]
        public bool isStalled = false;
        // Temporizador de protección para prevenir calados consecutivos inmediatos tras el arranque.
        private float stallProtectionTimer = 0f;

        [Header("Estabilización (Anti-Inclinación)")]
        [Tooltip("Fuerza estabilizadora anti-vuelco aplicada a la suspensión.")]
        public float antiRollForce = 5000f; 
        [Tooltip("Desplazamiento del centro de masas para garantizar la estabilidad dinámica.")]
        public float centerOfMassHeight = -0.7f; 

        [Header("Evaluación DGT")]
        [Tooltip("Evento disparado al detectar la falta por calado del motor.")]
        public GameEvent infractionEvent;
        [Tooltip("ScriptableObject con la regla de falta DGT aplicable al calar el vehículo.")]
        public InfraccionSO caladoInfraccion;

        [Header("Referencias Ruedas Físicas")]
        [Tooltip("WheelCollider delantero izquierdo.")]
        public WheelCollider frontLeftWheel; 
        [Tooltip("WheelCollider delantero derecho.")]
        public WheelCollider frontRightWheel;
        [Tooltip("WheelCollider trasero izquierdo.")]
        public WheelCollider rearLeftWheel; 
        [Tooltip("WheelCollider trasero derecho.")]
        public WheelCollider rearRightWheel;

        [Header("Referencias Visuales (Mallas)")]
        [Tooltip("Transform de la malla visual de la rueda delantera izquierda.")]
        public Transform visualFL; 
        [Tooltip("Transform de la malla visual de la rueda delantera derecha.")]
        public Transform visualFR;
        [Tooltip("Transform de la malla visual de la rueda trasera izquierda.")]
        public Transform visualRL; 
        [Tooltip("Transform de la malla visual de la rueda trasera derecha.")]
        public Transform visualRR;
        [Tooltip("Transform de la malla del volante de dirección del habitáculo.")]
        public Transform visualSteeringWheel;
        [Tooltip("Factor multiplicador de rotación del volante para reflejar la relación de dirección.")]
        public float steeringWheelMultiplier = 2.5f; 

        [Header("Luces e Indicadores")]
        [Tooltip("Dirección del intermitente activo (-1 Izquierda, 0 Ninguno, 1 Derecha).")]
        public int activeBlinker = 0; 
        [Tooltip("Estado del ciclo de parpadeo del testigo de intermitencia.")]
        public bool blinkerState = false; 
        // Temporizador interno para el ciclo de parpadeo de luces.
        private float blinkerTimer = 0f;
        [Tooltip("Frecuencia de parpadeo de los indicadores en segundos.")]
        public float blinkRate = 0.5f; 

        [Header("Simulación de Motor (RPM)")]
        [Tooltip("Variable reactiva en ScriptableObject que expone el régimen dinámico de RPM.")]
        public FloatVariable engineRPMVariable;
        [Tooltip("Régimen de revoluciones al ralentí.")]
        public float minRPM = 800f;
        [Tooltip("Régimen máximo de revoluciones antes de corte de inyección.")]
        public float maxRPM = 6000f;
        [Tooltip("Régimen dinámico instantáneo de RPM del motor.")]
        public float currentRPM;
        [Tooltip("Relación de desmultiplicación del grupo diferencial trasero.")]
        public float finalDriveRatio = 4.1f; 

        [Header("Ajustes de Caja Automática")]
        [Tooltip("RPM a las que el coche subirá de marcha (Calibrado a 4200 para estirar marchas).")]
        public float upshiftRPM = 4200f;
        [Tooltip("RPM a las que el coche bajará de marcha (Calibrado a 1800).")]
        public float downshiftRPM = 1800f;

        // Referencia al componente de física para el cálculo cinemático.
        private Rigidbody rb;

        // Inicializa el centro de masas, apaga el motor de forma preventiva y limpia los canales SOA.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void Awake() {
            rb = GetComponent<Rigidbody>();
            rb.centerOfMass = new Vector3(0f, -0.5f, 0.1f);
            
            // AJUSTE 2: Todos los modos (Manual y Automático) inician con el motor apagado (isStalled = true)
            isStalled = true;
            if (isStalledSO != null) isStalledSO.Value = isStalled; // Sincronizamos con la capa de datos

            if (inputData != null) inputData.ResetData();
            if (currentSpeedVariable != null) currentSpeedVariable.Value = 0;
            if (currentSpeedLimitVariable != null) currentSpeedLimitVariable.Value = 0;
        }

        // Procesa la física de la transmisión, dirección, entrega de par motor, evaluación de calado y telemetría.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void FixedUpdate() {
            if (inputData == null) return;
            if (stallProtectionTimer > 0) stallProtectionTimer -= Time.fixedDeltaTime;

            // Lógica de transmisión automática
            if (isAutomaticSO != null && isAutomaticSO.Value)
            {
                GestionarCajaAutomatica();
            }

            HandleSteering();
            HandleMotor();
            CheckForStall();
            UpdateTelemetry();
        }

        // Actualiza las mallas visuales del habitáculo y procesa el temporizador de intermitentes.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void Update() {
            //UpdateWheelVisuals();
            UpdateVisualSteeringWheel();
            HandleBlinkersLogic();
        }

        // Modula la cadencia de parpadeo de los indicadores de dirección según la entrada de datos.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void HandleBlinkersLogic()
        {
            activeBlinker = inputData.ActiveBlinker;
            if (activeBlinker != 0)
            {
                blinkerTimer += Time.deltaTime;
                if (blinkerTimer >= blinkRate)
                {
                    blinkerTimer = 0;
                    blinkerState = !blinkerState;
                }
            }
            else { blinkerState = false; blinkerTimer = 0; }
        }

        // Aplica la rotación angular del eje delantero basándose en la entrada del volante normalizada.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void HandleSteering() {
            float steeringInput = inputData.Steering;
            if (Mathf.Abs(steeringInput) < 0.01f) steeringInput = 0f;

            float steerAngle = steeringInput * maxSteerAngle;
            frontLeftWheel.steerAngle = steerAngle;
            frontRightWheel.steerAngle = steerAngle;
        }

        // Calcula las RPM, entrega par motor a las ruedas motrices y distribuye la fuerza de frenado hidráulico y de mano.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void HandleMotor()
        {
            if (isStalled) { StopMotor(); currentRPM = 0; return; }

            // 1. CÁLCULO DE RPM ESTABLE
            float speedMS = rb.linearVelocity.magnitude;
            float wheelRadius = frontLeftWheel.radius;
            float wheelCircumference = 2 * Mathf.PI * wheelRadius;
            float wheelRPM = (speedMS / wheelCircumference) * 60f;

            int gearIndex = Mathf.Clamp(inputData.CurrentGear + 1, 0, gearRatios.Length - 1);
            float currentGearRatio = gearRatios[gearIndex];
            float targetRPM = wheelRPM * Mathf.Abs(currentGearRatio) * finalDriveRatio;

            if (inputData.CurrentGear == 0 || inputData.Clutch > 0.5f)
            {
                float accelRPM = Mathf.Lerp(minRPM, maxRPM, inputData.Throttle);
                targetRPM = Mathf.Max(targetRPM, accelRPM);
            }
            
            currentRPM = Mathf.Lerp(currentRPM, Mathf.Max(targetRPM, minRPM), Time.fixedDeltaTime * 12f);

            // 2. GOBERNADOR MECÁNICO
            float torqueFactor = 1.0f;
            float engineResistanceBrake = 0f;

            if (currentRPM >= maxRPM)
            {
                torqueFactor = 0f;
                engineResistanceBrake = (currentRPM - maxRPM) * 40f; 
            }
            else if (currentRPM > maxRPM * 0.9f)
            {
                torqueFactor = Mathf.InverseLerp(maxRPM, maxRPM * 0.9f, currentRPM);
            }

            // --- 3. LÓGICA DE FRENO DE MANO ---
            float handbrakeTorque = inputData.Handbrake ? brakeForce * 2f : 0f;

            // --- 4. ENTREGA DE PAR ---
            float transmission = 1.0f - inputData.Clutch;
            float wheelTorque = inputData.Throttle * motorForce * currentGearRatio * finalDriveRatio * transmission * torqueFactor;

            if (inputData.Handbrake) wheelTorque = 0;

            rearLeftWheel.motorTorque = wheelTorque;
            rearRightWheel.motorTorque = wheelTorque;

            // --- 5. SISTEMA DE FRENADO DISTRIBUIDO ---
            float pedalBrake = inputData.Breaking * brakeForce;

            frontLeftWheel.brakeTorque = pedalBrake + engineResistanceBrake;
            frontRightWheel.brakeTorque = pedalBrake + engineResistanceBrake;

            float totalRearBrake = pedalBrake + handbrakeTorque + engineResistanceBrake;

            if (inputData.Throttle < 0.1f && currentGearRatio != 0)
            {
                totalRearBrake += engineBrakeForce * Mathf.Abs(currentGearRatio);
            }

            rearLeftWheel.brakeTorque = totalRearBrake;
            rearRightWheel.brakeTorque = totalRearBrake;

            if (engineRPMVariable != null) engineRPMVariable.Value = currentRPM;
        }

        // Audita las condiciones cinemáticas para detectar un calado del motor por error en el embrague y notifica la infracción.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void CheckForStall() {
            // Si la transmisión es automática, el motor nunca se cala por bajo régimen
            if (isAutomaticSO != null && isAutomaticSO.Value) return;

            if (isStalled || inputData.CurrentGear == 0 || stallProtectionTimer > 0) return;
            
            if (rb.linearVelocity.magnitude < 0.5f && inputData.Clutch < 0.2f && inputData.Throttle < 0.1f) {
                isStalled = true;
                if (isStalledSO != null) isStalledSO.Value = true; 
                if(infractionEvent != null) infractionEvent.Raise(caladoInfraccion);
                Debug.Log("<color=orange>MOTOR CALADO</color>");
            }
        }

        // Intenta restablecer el funcionamiento del motor térmico verificando la posición del cambio o pedal de embrague.
        // Parámetros:
        //   - data: Objeto con parámetros opcionales del evento de reinicio.
        // Salida: Ninguna.
        public void RestartEngine(object data = null) {
            bool canRestart = false;

            // En modo automático el coche se enciende únicamente en Neutro (marcha 0)
            if (isAutomaticSO != null && isAutomaticSO.Value)
            {
                canRestart = (inputData.CurrentGear == 0);
            }
            else
            {
                canRestart = (inputData.CurrentGear == 0 || inputData.Clutch > 0.7f);
            }

            if (canRestart) {
                isStalled = false;
                if (isStalledSO != null) isStalledSO.Value = false; 
                stallProtectionTimer = 2f; 
                Debug.Log("<color=green>MOTOR ARRANCADO</color>");
            }
        }

        // Aplica un par de frenado homogéneo sobre todas las ruedas físicas del vehículo.
        // Parámetros:
        //   - force: Magnitud de par de frenado en N·m.
        // Salida: Ninguna.
        private void ApplyBrake(float force) {
            frontLeftWheel.brakeTorque = force; frontRightWheel.brakeTorque = force;
            rearLeftWheel.brakeTorque = force; rearRightWheel.brakeTorque = force;
        }

        // Exporta la velocidad lineal instantánea y el índice de marcha activa hacia las variables SOA de telemetría.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void UpdateTelemetry() {
            if(currentSpeedVariable != null) currentSpeedVariable.Value = rb.linearVelocity.magnitude * 3.6f;
            if(currentGearVariable != null) currentGearVariable.Value = inputData.CurrentGear;
        }

        // Sincroniza la postura del WheelCollider con la posición y rotación de la malla 3D de la rueda.
        // Parámetros:
        //   - col: Componente WheelCollider fuente de la simulación.
        //   - mesh: Componente Transform de la representación visual.
        // Salida: Ninguna.
        private void SyncWheel(WheelCollider col, Transform mesh) {
            if (mesh == null) return;
            Vector3 pos; Quaternion rot;
            col.GetWorldPose(out pos, out rot);
            mesh.position = pos; mesh.rotation = rot;
        }

        // Aplica la rotación visual correspondiente al elemento gráfico del volante en el interior del habitáculo.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void UpdateVisualSteeringWheel() {
            if (visualSteeringWheel != null)
                visualSteeringWheel.localRotation = Quaternion.Euler(0, 0, -inputData.Steering * maxSteerAngle * steeringWheelMultiplier);
        }

        // Anula el par de tracción en las ruedas motrices y aplica una leve resistencia hidráulica al estar el motor apagado.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void StopMotor()
        {
            rearLeftWheel.motorTorque = 0;
            rearRightWheel.motorTorque = 0;
            ApplyBrake(brakeForce * 0.1f);
        }

        // Algoritmo de gestión secuencial para cajas de cambio automáticas basado en el régimen de giro de RPM.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void GestionarCajaAutomatica()
        {
            // 1. AJUSTE: El embrague se fuerza a 0f (totalmente acoplado) para la entrega de par
            inputData.Clutch = 0f;

            // 2. AJUSTE: Eliminada la línea de auto-arranque. El motor permanecerá apagado hasta 
            // que el alumno presione 'R' estando en Neutro (0).

            // 3. Algoritmo secuencial de marchas según el régimen de giro (RPM) para marchas de avance (Drive)
            // Se han calibrado las constantes para estirar marchas de forma realista y evitar 5ª a baja velocidad.
            if (inputData.CurrentGear > 0)
            {
                // Si el motor excede las 4200 RPM, sube la marcha
                if (currentRPM > upshiftRPM && inputData.CurrentGear < 5)
                {
                    inputData.CurrentGear++;
                    Debug.Log($"<color=green>Caja Automática:</color> Subiendo a marcha {inputData.CurrentGear}");
                }
                // Si el motor cae por debajo de las 1800 RPM, reduce la marcha
                else if (currentRPM < downshiftRPM && inputData.CurrentGear > 1)
                {
                    inputData.CurrentGear--;
                    Debug.Log($"<color=green>Caja Automática:</color> Bajando a marcha {inputData.CurrentGear}");
                }
            }
        }
    }
}