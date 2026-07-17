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

        [Header("Física y Potencia")]
        public float motorForce = 350f; 
        public float brakeForce = 15000f;
        public float maxSteerAngle = 60f;
        public float engineBrakeForce = 600f; 

        [Header("Transmisión Manual")]
        // Ratios de utilitario real: 1ª muy corta (lenta), 5ª larga (rápida)
        // R, N, 1, 2, 3, 4, 5
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
            
            // Forzamos el centro de masas exactamente en el centro horizontal (X=0)
            // Y muy bajo (Y=-0.9) para máxima estabilidad.
            // Z en 0 o ligeramente adelantado (0.1)
            //rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
            // Evita que Unity calcule inercias extrañas si el modelo no es simétrico
            //rb.inertiaTensorRotation = Quaternion.identity;
            
            isStalled = true;
            if (inputData != null) inputData.ResetData();
            if (currentSpeedVariable != null) currentSpeedVariable.Value = 0;
            if (currentSpeedLimitVariable != null) currentSpeedLimitVariable.Value = 0;
        }

        private void FixedUpdate() {
            if (inputData == null) return;
            if (stallProtectionTimer > 0) stallProtectionTimer -= Time.fixedDeltaTime;

            //ApplyAntiRoll(frontLeftWheel, frontRightWheel); // Estabilizar eje delantero
            //ApplyAntiRoll(rearLeftWheel, rearRightWheel);   // Estabilizar eje trasero
            HandleSteering();
            HandleMotor();
            CheckForStall();
            UpdateTelemetry();
        }

        private void ApplyAntiRoll(WheelCollider wheelL, WheelCollider wheelR)
        {
            WheelHit hit;
            float travelL = 1.0f;
            float travelR = 1.0f;

            // Calculamos cuánto se ha comprimido la suspensión de cada lado
            bool groundedL = wheelL.GetGroundHit(out hit);
            if (groundedL)
                travelL = (-wheelL.transform.InverseTransformPoint(hit.point).y - wheelL.radius) / wheelL.suspensionDistance;

            bool groundedR = wheelR.GetGroundHit(out hit);
            if (groundedR)
                travelR = (-wheelR.transform.InverseTransformPoint(hit.point).y - wheelR.radius) / wheelR.suspensionDistance;

            // Aplicamos una fuerza opuesta proporcional a la diferencia de compresión
            float antiRollAmount = (travelL - travelR) * antiRollForce;

            if (groundedL)
                rb.AddForceAtPosition(wheelL.transform.up * -antiRollAmount, wheelL.transform.position);
            if (groundedR)
                rb.AddForceAtPosition(wheelR.transform.up * antiRollAmount, wheelR.transform.position);
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
            // Zona muerta: si el input es muy pequeño, forzamos 0
            float steeringInput = inputData.Steering;
            if (Mathf.Abs(steeringInput) < 0.01f) steeringInput = 0f;

            float steerAngle = steeringInput * maxSteerAngle;
            frontLeftWheel.steerAngle = steerAngle;
            frontRightWheel.steerAngle = steerAngle;
        }

        private void HandleMotor()
        {
            if (isStalled) { StopMotor(); currentRPM = 0; return; }

            // 1. CÁLCULO DE RPM ESTABLE (Basado en velocidad real de avance)
            float speedMS = rb.linearVelocity.magnitude;
            float wheelRadius = frontLeftWheel.radius;
            float wheelCircumference = 2 * Mathf.PI * wheelRadius;
            
            // RPM de la rueda basadas en el avance real
            float wheelRPM = (speedMS / wheelCircumference) * 60f;

            int gearIndex = Mathf.Clamp(inputData.CurrentGear + 1, 0, gearRatios.Length - 1);
            float currentGearRatio = gearRatios[gearIndex];

            // RPM del Motor = RPM Rueda * Marcha * Diferencial
            float targetRPM = wheelRPM * Mathf.Abs(currentGearRatio) * finalDriveRatio;

            // Simulación de aceleración en vacío (N o Embrague)
            if (inputData.CurrentGear == 0 || inputData.Clutch > 0.5f)
            {
                float accelRPM = Mathf.Lerp(minRPM, maxRPM, inputData.Throttle);
                targetRPM = Mathf.Max(targetRPM, accelRPM);
            }
            
            // Suavizado de aguja y estabilización de cálculo
            currentRPM = Mathf.Lerp(currentRPM, Mathf.Max(targetRPM, minRPM), Time.fixedDeltaTime * 12f);

            // 2. GOBERNADOR MECÁNICO (Corte de potencia y freno motor al límite)
            float torqueFactor = 1.0f;
            float engineResistanceBrake = 0f;

            if (currentRPM >= maxRPM)
            {
                torqueFactor = 0f; // Corte de inyección
                // Si la inercia intenta superar las RPM, el motor opone resistencia física
                engineResistanceBrake = (currentRPM - maxRPM) * 40f; 
            }
            else if (currentRPM > maxRPM * 0.9f)
            {
                // Pérdida de potencia progresiva al acercarse al "rojo"
                torqueFactor = Mathf.InverseLerp(maxRPM, maxRPM * 0.9f, currentRPM);
            }

            // 3. ENTREGA DE PAR (Torque)
            float transmission = 1.0f - inputData.Clutch;
            float wheelTorque = inputData.Throttle * motorForce * currentGearRatio * finalDriveRatio * transmission * torqueFactor;

            rearLeftWheel.motorTorque = wheelTorque;
            rearRightWheel.motorTorque = wheelTorque;

            // 4. SISTEMA DE FRENADO (Pedal + Resistencia de motor al corte)
            float totalBrake = (inputData.Breaking * brakeForce) + engineResistanceBrake;

            // Freno motor estándar (cuando no se acelera)
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
                if(infractionEvent != null) infractionEvent.Raise(caladoInfraccion);
            }
        }

        public void RestartEngine(object data = null) {
            if (inputData.CurrentGear == 0 || inputData.Clutch > 0.7f) {
                isStalled = false;
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
            // Aplicamos un pequeño freno para que el coche no ruede eternamente al calarse
            ApplyBrake(brakeForce * 0.1f);
        }
    }
}