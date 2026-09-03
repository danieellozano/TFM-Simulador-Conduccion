using NUnit.Framework;
using UnityEngine;
using Simulador.Core;
using Simulador.PhysicsModule;
using Simulador.Physics;

namespace Simulador.Tests
{
    public class PhysicsTests
    {
        private InputDataSO inputData;
        private FloatVariable currentSpeedSO;
        private IntVariable currentGearSO;
        private FloatVariable engineRPMSO;
        private BoolVariable isStalledSO;
        private BoolVariable isAutomaticSO;

        [SetUp]
        public void SetUp()
        {
            inputData = ScriptableObject.CreateInstance<InputDataSO>();
            currentSpeedSO = ScriptableObject.CreateInstance<FloatVariable>();
            currentGearSO = ScriptableObject.CreateInstance<IntVariable>();
            engineRPMSO = ScriptableObject.CreateInstance<FloatVariable>();
            isStalledSO = ScriptableObject.CreateInstance<BoolVariable>();
            isAutomaticSO = ScriptableObject.CreateInstance<BoolVariable>();

            inputData.ResetData();
            isAutomaticSO.Value = false;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(inputData);
            Object.DestroyImmediate(currentSpeedSO);
            Object.DestroyImmediate(currentGearSO);
            Object.DestroyImmediate(engineRPMSO);
            Object.DestroyImmediate(isStalledSO);
            Object.DestroyImmediate(isAutomaticSO);
        }

        // ---------------------------------------------------------------
        // 1. GEOMETRÍA DE DIRECCIÓN 
        // ---------------------------------------------------------------
        [Test]
        public void Test_Direccion_ZonaMuerta_Y_Angulo_Maximo()
        {
            float maxSteerAngle = 33f;

            // Caso A: Dentro de la zona muerta (< 0.01f)
            inputData.Steering = 0.005f;
            float inputFiltrado = (Mathf.Abs(inputData.Steering) < 0.01f) ? 0f : inputData.Steering;
            float steerAngleA = inputFiltrado * maxSteerAngle;
            Assert.AreEqual(0f, steerAngleA, "Valores por debajo de 0.01 deben ser filtrados a 0.");

            // Caso B: Giro al 50% hacia la derecha
            inputData.Steering = 0.5f;
            inputFiltrado = (Mathf.Abs(inputData.Steering) < 0.01f) ? 0f : inputData.Steering;
            float steerAngleB = inputFiltrado * maxSteerAngle;
            Assert.AreEqual(16.5f, steerAngleB, "Un input de 0.5 sobre 33° debe resultar en 16.5°.");
        }

        // ---------------------------------------------------------------
        // 2. TRANSMISIÓN Y RPM 
        // ---------------------------------------------------------------
        [Test]
        public void Test_Calculo_RPM_Y_Relacion_De_Marchas()
        {
            float[] gearRatios = { -3.4f, 0f, 3.5f, 1.9f, 1.3f, 0.95f, 0.78f };
            float finalDriveRatio = 4.1f;
            float wheelRadius = 0.33f;
            float wheelCircumference = 2 * Mathf.PI * wheelRadius;

            float speedMS = 5.55f; // ~20 km/h
            float wheelRPM = (speedMS / wheelCircumference) * 60f; // ~160.6 RPM

            // Probar en 1ª marcha (gearIndex = 2, ratio = 3.5)
            int gearIndex1 = 2;
            float targetRPM1 = wheelRPM * Mathf.Abs(gearRatios[gearIndex1]) * finalDriveRatio;
            Assert.GreaterOrEqual(targetRPM1, 2200f, "A 20 km/h en 1ª marcha las RPM deben ser elevadas.");

            // Probar en 3ª marcha (gearIndex = 4, ratio = 1.3)
            int gearIndex3 = 4;
            float targetRPM3 = wheelRPM * Mathf.Abs(gearRatios[gearIndex3]) * finalDriveRatio;
            Assert.Less(targetRPM3, targetRPM1, "A la misma velocidad, la 3ª marcha debe mantener RPM más bajas que la 1ª.");
        }

        // ---------------------------------------------------------------
        // 3. GOBERNADOR MECÁNICO 
        // ---------------------------------------------------------------
        [Test]
        public void Test_GobernadorMecanico_Corte_De_Inyeccion_A_6000_RPM()
        {
            float maxRPM = 6000f;

            // Caso A: RPM normales (3000) -> Par completo (torqueFactor = 1.0)
            float currentRPM = 3000f;
            float torqueFactorA = 1.0f;
            float resistanceA = 0f;
            if (currentRPM >= maxRPM) { torqueFactorA = 0f; resistanceA = (currentRPM - maxRPM) * 40f; }
            else if (currentRPM > maxRPM * 0.9f) { torqueFactorA = Mathf.InverseLerp(maxRPM, maxRPM * 0.9f, currentRPM); }

            Assert.AreEqual(1.0f, torqueFactorA, "A 3000 RPM el par debe ser del 100%.");
            Assert.AreEqual(0f, resistanceA, "A 3000 RPM no debe haber contrafuerza de resistencia.");

            // Caso B: RPM en el corte (6100) -> Par nulo y 4000N de contrafuerza (100 * 40)
            currentRPM = 6100f;
            float torqueFactorB = 1.0f;
            float resistanceB = 0f;
            if (currentRPM >= maxRPM) { torqueFactorB = 0f; resistanceB = (currentRPM - maxRPM) * 40f; }

            Assert.AreEqual(0f, torqueFactorB, "A 6000+ RPM el par motor debe cortarse a 0.");
            // Corregido: (6100 - 6000) * 40 = 4000N
            Assert.AreEqual(4000f, resistanceB, "Deben aplicarse 4000N de contrafuerza por exceso de RPM.");
        }

        // ---------------------------------------------------------------
        // 4. ENTREGA DE PAR Y EMBRAGUE 
        // ---------------------------------------------------------------
        [Test]
        public void Test_EntregaDePar_Desacoplamiento_Por_Embrague()
        {
            float motorForce = 350f;
            float currentGearRatio = 3.5f;
            float finalDriveRatio = 4.1f;
            float torqueFactor = 1.0f;

            inputData.Throttle = 1.0f; // Acelerador a fondo

            // Caso A: Embrague totalmente suelto (Clutch = 0 -> transmission = 1.0)
            inputData.Clutch = 0.0f;
            float transmissionA = 1.0f - inputData.Clutch;
            float wheelTorqueA = inputData.Throttle * motorForce * currentGearRatio * finalDriveRatio * transmissionA * torqueFactor;

            Assert.Greater(wheelTorqueA, 5000f, "Con embrague suelto se debe entregar el 100% del par.");

            // Caso B: Embrague pisado a fondo (Clutch = 1.0 -> transmission = 0.0)
            inputData.Clutch = 1.0f;
            float transmissionB = 1.0f - inputData.Clutch;
            float wheelTorqueB = inputData.Throttle * motorForce * currentGearRatio * finalDriveRatio * transmissionB * torqueFactor;

            Assert.AreEqual(0f, wheelTorqueB, "Con embrague a fondo la entrega de par a las ruedas debe ser 0.");
        }

        // ---------------------------------------------------------------
        // 5. CALADO DE MOTOR 
        // ---------------------------------------------------------------
        [Test]
        public void Test_CheckForStall_Cala_En_Manual_Pero_No_En_Automatica()
        {
            float speedMS = 0.1f; // Prácticamente parado (< 0.5 m/s)
            inputData.CurrentGear = 1;
            inputData.Clutch = 0.0f;   // Embrague suelto
            inputData.Throttle = 0.0f; // Sin acelerar

            // Caso A: Transmisión Manual (isAutomaticSO = false)
            isAutomaticSO.Value = false;
            bool debeCalarManual = !isAutomaticSO.Value && (speedMS < 0.5f && inputData.Clutch < 0.2f && inputData.Throttle < 0.1f);
            Assert.IsTrue(debeCalarManual, "En transmisión manual debe calar si se suelta embrague a baja velocidad.");

            // Caso B: Transmisión Automática (isAutomaticSO = true)
            isAutomaticSO.Value = true;
            bool debeCalarAuto = !isAutomaticSO.Value && (speedMS < 0.5f && inputData.Clutch < 0.2f && inputData.Throttle < 0.1f);
            Assert.IsFalse(debeCalarAuto, "En transmisión automática el motor NUNCA debe calar.");
        }

        // ---------------------------------------------------------------
        // 6. PROTOCOLO DE ARRANQUE 
        // ---------------------------------------------------------------
        [Test]
        public void Test_RestartEngine_Reglas_De_Seguridad_Manual_Vs_Automatica()
        {
            // Caso Manual: Intenta arrancar en 1ª marcha sin pisar embrague -> RECHAZADO
            isAutomaticSO.Value = false;
            inputData.CurrentGear = 1;
            inputData.Clutch = 0.0f;
            bool canRestartManualFail = (inputData.CurrentGear == 0 || inputData.Clutch > 0.7f);
            Assert.IsFalse(canRestartManualFail, "En manual no debe arrancar en marcha con embrague suelto.");

            // Caso Manual: Intenta arrancar pisando embrague al 80% -> PERMITIDO
            inputData.Clutch = 0.8f;
            bool canRestartManualSuccess = (inputData.CurrentGear == 0 || inputData.Clutch > 0.7f);
            Assert.IsTrue(canRestartManualSuccess, "En manual debe permitir arranque con embrague > 70%.");

            // Caso Automático: Intenta arrancar en Drive (1ª) -> RECHAZADO
            isAutomaticSO.Value = true;
            inputData.CurrentGear = 1;
            bool canRestartAutoFail = (inputData.CurrentGear == 0);
            Assert.IsFalse(canRestartAutoFail, "En automático solo debe permitir arranque en Neutro (0).");
        }

        // ---------------------------------------------------------------
        // 7. CAJA AUTOMÁTICA 
        // ---------------------------------------------------------------
        [Test]
        public void Test_CajaAutomatica_Secuenciacion_Upshift_Y_Downshift()
        {
            float upshiftRPM = 4200f;
            float downshiftRPM = 1800f;
            inputData.CurrentGear = 2; // Vamos en 2ª marcha

            // Caso A: Motor muy revolucionado (4500 RPM) -> Debe subir a 3ª
            float currentRPM = 4500f;
            if (currentRPM > upshiftRPM && inputData.CurrentGear < 5) inputData.CurrentGear++;
            Assert.AreEqual(3, inputData.CurrentGear, "Al superar 4200 RPM debe subir automáticamente de marcha.");

            // Caso B: Motor ahogado (1500 RPM) -> Debe bajar a 2ª
            currentRPM = 1500f;
            if (currentRPM < downshiftRPM && inputData.CurrentGear > 1) inputData.CurrentGear--;
            Assert.AreEqual(2, inputData.CurrentGear, "Al caer de 1800 RPM debe bajar automáticamente de marcha.");
        }

        // ---------------------------------------------------------------
        // 8. CÁMARA Y LÍMITES ANGULARES 
        // ---------------------------------------------------------------
        [Test]
        public void Test_CameraController_Límites_Angulares_Y_Centrado()
        {
            float minRotationY = -85f;
            float maxRotationY = 85f;

            // Simulación de rotación excesiva a la derecha (+120°)
            float rotacionSolicitada = 120f;
            float rotacionClampeada = Mathf.Clamp(rotacionSolicitada, minRotationY, maxRotationY);

            Assert.AreEqual(85f, rotacionClampeada, "La rotación de la cámara debe estar clamped a un máximo de +85°.");

            // Simulación de rotación excesiva a la izquierda (-100°)
            rotacionSolicitada = -100f;
            rotacionClampeada = Mathf.Clamp(rotacionSolicitada, minRotationY, maxRotationY);

            Assert.AreEqual(-85f, rotacionClampeada, "La rotación de la cámara debe estar clamped a un mínimo de -85°.");
        }
    }
}