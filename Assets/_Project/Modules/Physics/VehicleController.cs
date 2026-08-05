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
        public BoolVariable isStalledSO; // <--- NUEVO: Canal para informar si el motor está calado

        [Header("Física y Potencia")]
        public float motorForce = 350f; 
        public float brakeForce = 15000f;
        public float maxSteerAngle = 45f;
        public float engineBrakeForce = 600f; 

        [Header("Transmisión Manual")]
        public float[] gearRatios = { -7.0f, 0f, 8.2f, 4.5f, 2.8f, 1.8f, 1.3f }; 
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

        private Rigidbody rb;

        private void Awake() {
            rb = GetComponent<Rigidbody>();
            rb.centerOfMass = new Vector3(0f, -0.5f, 0.1f);
            
            // Inicialización de estado
            isStalled = true;
            if (isStalledSO != null) isStalledSO.Value = true; // Sincronizamos con la capa de datos

            if (inputData != null) inputData.ResetData();
            if (currentSpeedVariable != null) currentSpeedVariable.Value = 0;
            if (currentSpeedLimitVariable != null) currentSpeedLimitVariable.Value = 0;
        }

        private void FixedUpdate() {
            if (inputData == null) return;
            if (stallProtectionTimer > 0) stallProtectionTimer -= Time.fixedDeltaTime;

            HandleSteering();
            HandleMotor();
            CheckForStall();
            UpdateTelemetry();
        }

        private void Update() {
            UpdateWheelVisuals();
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

            // 3. ENTREGA DE PAR
            float transmission = 1.0f - inputData.Clutch;
            float wheelTorque = inputData.Throttle * motorForce * currentGearRatio * finalDriveRatio * transmission * torqueFactor;

            rearLeftWheel.motorTorque = wheelTorque;
            rearRightWheel.motorTorque = wheelTorque;

            // 4. FRENADO
            float totalBrake = (inputData.Breaking * brakeForce) + engineResistanceBrake;

            if (inputData.Throttle < 0.1f && currentGearRatio != 0)
            {
                totalBrake += engineBrakeForce * Mathf.Abs(currentGearRatio);
            }

            ApplyBrake(totalBrake);

            if (engineRPMVariable != null) engineRPMVariable.Value = currentRPM;
        }

        private void CheckForStall() {
            if (isStalled || inputData.CurrentGear == 0 || stallProtectionTimer > 0) return;
            
            if (rb.linearVelocity.magnitude < 0.5f && inputData.Clutch < 0.2f && inputData.Throttle < 0.1f) {
                isStalled = true;
                if (isStalledSO != null) isStalledSO.Value = true; // Informar a la capa de datos
                if(infractionEvent != null) infractionEvent.Raise(caladoInfraccion);
                Debug.Log("<color=orange>MOTOR CALADO</color>");
            }
        }

        public void RestartEngine(object data = null) {
            if (inputData.CurrentGear == 0 || inputData.Clutch > 0.7f) {
                isStalled = false;
                if (isStalledSO != null) isStalledSO.Value = false; // Informar a la capa de datos
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

        private void UpdateWheelVisuals() {
            SyncWheel(frontLeftWheel, visualFL); SyncWheel(frontRightWheel, visualFR);
            SyncWheel(rearLeftWheel, visualRL); SyncWheel(rearRightWheel, visualRR);
        }

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
    }
}