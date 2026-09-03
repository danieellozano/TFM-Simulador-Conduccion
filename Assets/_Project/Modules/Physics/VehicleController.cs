using UnityEngine;
using Simulador.Core;

namespace Simulador.PhysicsModule
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {
        [Header("Canales de Datos (SOA)")]
        public InputDataSO inputData;
        public FloatVariable currentSpeedVariable;
        public IntVariable currentGearVariable;
        public FloatVariable currentSpeedLimitVariable;
        public BoolVariable isStalledSO; 
        public BoolVariable isAutomaticSO;

        [Header("Física y Potencia")]
        public float motorForce = 350f; 
        public float brakeForce = 8000;
        public float maxSteerAngle = 33f;
        public float engineBrakeForce = 600f; 

        [Header("Transmisión Manual")]
        public float[] gearRatios = { -3.4f, 0f, 3.5f, 1.9f, 1.3f, 0.95f, 0.78f }; 
        public bool isStalled = false;
        private float stallProtectionTimer = 0f;

        [Header("Estabilización (Anti-Inclinación)")]
        public float antiRollForce = 5000f; 
        public float centerOfMassHeight = -0.7f; 

        [Header("Evaluación DGT")]
        public GameEvent infractionEvent;
        public InfraccionSO caladoInfraccion;

        [Header("Referencias Ruedas Físicas")]
        public WheelCollider frontLeftWheel; public WheelCollider frontRightWheel;
        public WheelCollider rearLeftWheel; public WheelCollider rearRightWheel;

        [Header("Referencias Visuales (Mallas)")]
        public Transform visualFL; public Transform visualFR;
        public Transform visualRL; public Transform visualRR;
        public Transform visualSteeringWheel;
        public float steeringWheelMultiplier = 2.5f; 

        [Header("Luces e Indicadores")]
        public int activeBlinker = 0; 
        public bool blinkerState = false; 
        private float blinkerTimer = 0f;
        public float blinkRate = 0.5f; 

        [Header("Simulación de Motor (RPM)")]
        public FloatVariable engineRPMVariable;
        public float minRPM = 800f;
        public float maxRPM = 6000f;
        public float currentRPM;
        public float finalDriveRatio = 4.1f; 

        [Header("Ajustes de Caja Automática")]
        [Tooltip("RPM a las que el coche subirá de marcha (Calibrado a 4200 para estirar marchas).")]
        public float upshiftRPM = 4200f;
        [Tooltip("RPM a las que el coche bajará de marcha (Calibrado a 1800).")]
        public float downshiftRPM = 1800f;

        private Rigidbody rb;

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

        private void Update() {
            //UpdateWheelVisuals();
            UpdateVisualSteeringWheel();
            HandleBlinkersLogic();
        }

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

        private void HandleSteering() {
            float steeringInput = inputData.Steering;
            if (Mathf.Abs(steeringInput) < 0.01f) steeringInput = 0f;

            float steerAngle = steeringInput * maxSteerAngle;
            frontLeftWheel.steerAngle = steerAngle;
            frontRightWheel.steerAngle = steerAngle;
        }

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

        private void ApplyBrake(float force) {
            frontLeftWheel.brakeTorque = force; frontRightWheel.brakeTorque = force;
            rearLeftWheel.brakeTorque = force; rearRightWheel.brakeTorque = force;
        }

        private void UpdateTelemetry() {
            if(currentSpeedVariable != null) currentSpeedVariable.Value = rb.linearVelocity.magnitude * 3.6f;
            if(currentGearVariable != null) currentGearVariable.Value = inputData.CurrentGear;
        }

        // private void UpdateWheelVisuals() {
        //     SyncWheel(frontLeftWheel, visualFL); SyncWheel(frontRightWheel, visualFR);
        //     SyncWheel(rearLeftWheel, visualRL); SyncWheel(rearRightWheel, visualRR);
        // }

        private void SyncWheel(WheelCollider col, Transform mesh) {
            if (mesh == null) return;
            Vector3 pos; Quaternion rot;
            col.GetWorldPose(out pos, out rot);
            mesh.position = pos; mesh.rotation = rot;
        }

        private void UpdateVisualSteeringWheel() {
            if (visualSteeringWheel != null)
                visualSteeringWheel.localRotation = Quaternion.Euler(0, 0, -inputData.Steering * maxSteerAngle * steeringWheelMultiplier);
        }

        private void StopMotor()
        {
            rearLeftWheel.motorTorque = 0;
            rearRightWheel.motorTorque = 0;
            ApplyBrake(brakeForce * 0.1f);
        }

        // --- SISTEMA INTERNO DE CAJA AUTOMÁTICA ---
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