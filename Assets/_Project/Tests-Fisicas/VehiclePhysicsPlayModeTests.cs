using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Simulador.Core;
using Simulador.PhysicsModule;

namespace Simulador.Tests.Fisicas
{
    public class VehiclePhysicsPlayModeTests
    {
        private GameObject vehicleObj;
        private VehicleController vehicle;
        private Rigidbody rb;

        // Canales de Datos (SOA)
        private InputDataSO inputData;
        private FloatVariable currentSpeedVariable;
        private IntVariable currentGearVariable;
        private FloatVariable currentSpeedLimitVariable;
        private FloatVariable engineRPMVariable;
        private BoolVariable isStalledSO;
        private BoolVariable isAutomaticSO;
        private GameEvent infractionEvent;
        private InfraccionSO caladoInfraccion;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // 1. Instancias de ScriptableObjects
            inputData = ScriptableObject.CreateInstance<InputDataSO>();
            currentSpeedVariable = ScriptableObject.CreateInstance<FloatVariable>();
            currentGearVariable = ScriptableObject.CreateInstance<IntVariable>();
            currentSpeedLimitVariable = ScriptableObject.CreateInstance<FloatVariable>();
            engineRPMVariable = ScriptableObject.CreateInstance<FloatVariable>();
            isStalledSO = ScriptableObject.CreateInstance<BoolVariable>();
            isAutomaticSO = ScriptableObject.CreateInstance<BoolVariable>();
            infractionEvent = ScriptableObject.CreateInstance<GameEvent>();
            caladoInfraccion = ScriptableObject.CreateInstance<InfraccionSO>();
            caladoInfraccion.codigo = "CON-CALA";

            isAutomaticSO.Value = false;
            inputData.ResetData();

            // 2. Objeto Físico
            vehicleObj = new GameObject("Coche_Test_PlayMode");
            rb = vehicleObj.AddComponent<Rigidbody>();
            rb.mass = 1500f;
            rb.useGravity = false; // DESACTIVAMOS GRAVEDAD PARA EVITAR CAÍDA AL VACÍO

            vehicle = vehicleObj.AddComponent<VehicleController>();

            // 3. Asignación de referencias SOA
            vehicle.inputData = inputData;
            vehicle.currentSpeedVariable = currentSpeedVariable;
            vehicle.currentGearVariable = currentGearVariable;
            vehicle.currentSpeedLimitVariable = currentSpeedLimitVariable;
            vehicle.engineRPMVariable = engineRPMVariable;
            vehicle.isStalledSO = isStalledSO;
            vehicle.isAutomaticSO = isAutomaticSO;
            vehicle.infractionEvent = infractionEvent;
            vehicle.caladoInfraccion = caladoInfraccion;

            if (isStalledSO != null) isStalledSO.Value = vehicle.isStalled;

            // 4. Ruedas Físicas
            GameObject fl = new GameObject("FL"); fl.transform.SetParent(vehicleObj.transform);
            GameObject fr = new GameObject("FR"); fr.transform.SetParent(vehicleObj.transform);
            GameObject rl = new GameObject("RL"); rl.transform.SetParent(vehicleObj.transform);
            GameObject rr = new GameObject("RR"); rr.transform.SetParent(vehicleObj.transform);

            vehicle.frontLeftWheel = fl.AddComponent<WheelCollider>();
            vehicle.frontRightWheel = fr.AddComponent<WheelCollider>();
            vehicle.rearLeftWheel = rl.AddComponent<WheelCollider>();
            vehicle.rearRightWheel = rr.AddComponent<WheelCollider>();

            vehicle.frontLeftWheel.radius = 0.33f;
            vehicle.frontRightWheel.radius = 0.33f;
            vehicle.rearLeftWheel.radius = 0.33f;
            vehicle.rearRightWheel.radius = 0.33f;

            yield return new WaitForFixedUpdate();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(vehicleObj);
            Object.Destroy(inputData);
            Object.Destroy(currentSpeedVariable);
            Object.Destroy(currentGearVariable);
            Object.Destroy(currentSpeedLimitVariable);
            Object.Destroy(engineRPMVariable);
            Object.Destroy(isStalledSO);
            Object.Destroy(isAutomaticSO);
            Object.Destroy(infractionEvent);
            Object.Destroy(caladoInfraccion);
            yield return null;
        }

        // ===============================================================
        // 1. INICIALIZACIÓN Y CENTRO DE MASAS (Awake)
        // ===============================================================
        [UnityTest]
        public IEnumerator Test01_Awake_Establece_CentroDeMasas_Bajo_Y_Motor_Apagado()
        {
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(-0.5f, rb.centerOfMass.y, 0.01f, "El centro de masas debe estar rebajado a -0.5m.");
            Assert.IsTrue(vehicle.isStalled, "El vehículo debe iniciar con el motor apagado.");
            Assert.IsTrue(isStalledSO.Value, "La variable compartida isStalledSO debe reflejar el estado apagado.");
            Assert.AreEqual(0f, currentSpeedVariable.Value, "La velocidad inicial debe ser exactamente 0 Km/h.");
        }

        // ===============================================================
        // 2. PROTOCOLO DE ARRANQUE (RestartEngine)
        // ===============================================================
        [UnityTest]
        public IEnumerator Test02_ProtocoloArranque_Manual_Vs_Automatico()
        {
            yield return new WaitForFixedUpdate();

            // CASO A: Intenta arrancar en 1ª marcha sin pisar embrague -> DENEGADO
            inputData.CurrentGear = 1;
            inputData.Clutch = 0.0f;
            vehicle.RestartEngine();
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(vehicle.isStalled, "En manual no debe arrancar en marcha con embrague suelto.");

            // CASO B: Pisa embrague al 80% y arranca -> PERMITIDO
            inputData.Clutch = 0.8f;
            vehicle.RestartEngine();
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(vehicle.isStalled, "Debe arrancar al pisar el embrague > 70%.");
            Assert.IsFalse(isStalledSO.Value);

            // CASO C: Modo Automático solo permite arranque en Neutro (0)
            isAutomaticSO.Value = true;
            vehicle.isStalled = true;
            inputData.CurrentGear = 1;

            vehicle.RestartEngine();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(vehicle.isStalled, "En automático NO debe arrancar en marcha Drive (1).");

            inputData.CurrentGear = 0; // Neutro
            vehicle.RestartEngine();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(vehicle.isStalled, "En automático DEBE arrancar en marcha Neutro (0).");
        }

        // ===============================================================
        // 3. ACELERACIÓN Y TRACCIÓN
        // ===============================================================
        [UnityTest]
        public IEnumerator Test03_Aceleracion_EntregaDePar_Y_Movimiento_Fisico()
        {
            vehicle.RestartEngine();
            inputData.Handbrake = false;
            inputData.CurrentGear = 1;
            inputData.Clutch = 0.0f;
            inputData.Throttle = 1.0f;

            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.Greater(vehicle.rearLeftWheel.motorTorque, 0f, "Las ruedas motrices deben recibir par acelerador.");
            Assert.Greater(vehicle.rearRightWheel.motorTorque, 0f);
            Assert.Greater(currentSpeedVariable.Value, 0.0f, "La telemetría de velocidad debe actualizarse.");
        }

        // ===============================================================
        // 4. CALADO AUTOMÁTICO (CheckForStall) Y EVENTO DGT
        // ===============================================================
        [UnityTest]
        public IEnumerator Test04_MecanicaDeCalado_Cala_Y_Dispara_Evento_DGT()
        {
            vehicle.RestartEngine();
            yield return new WaitForFixedUpdate();

            var timerField = typeof(VehicleController).GetField("stallProtectionTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (timerField != null) timerField.SetValue(vehicle, 0f);

            bool eventoCaladoRecibido = false;
            GameObject listenerObj = new GameObject("EventListener");
            GameEventListener listener = listenerObj.AddComponent<GameEventListener>();
            listener.Event = infractionEvent;
            listener.Response = new UnityEngine.Events.UnityEvent<object>();
            listener.Response.AddListener((data) => { eventoCaladoRecibido = true; });
            infractionEvent.RegisterListener(listener);

            inputData.CurrentGear = 1;
            inputData.Clutch = 0.0f;
            inputData.Throttle = 0.0f;

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(vehicle.isStalled, "El motor debe calarse automáticamente.");
            Assert.IsTrue(isStalledSO.Value);
            Assert.IsTrue(eventoCaladoRecibido, "Debe disparar el evento de falta DGT por calado.");

            Object.Destroy(listenerObj);
        }

        // ===============================================================
        // 5. FRENADO DISTRIBUIDO Y FRENO DE MANO
        // ===============================================================
        [UnityTest]
        public IEnumerator Test05_SistemaDeFrenadoDistribuido_Servicio_Y_Mano()
        {
            vehicle.RestartEngine();
            inputData.CurrentGear = 1;

            // Caso A: Freno de Servicio
            inputData.Breaking = 1.0f;
            inputData.Handbrake = false;
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(8000f, vehicle.frontLeftWheel.brakeTorque, "Eje delantero recibe 8000N de freno.");
            Assert.GreaterOrEqual(vehicle.rearLeftWheel.brakeTorque, 8000f);

            // Caso B: Freno de Mano
            inputData.Breaking = 0.0f;
            inputData.Handbrake = true;
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(0f, vehicle.rearLeftWheel.motorTorque, "Freno de mano anula el par motor.");
            Assert.GreaterOrEqual(vehicle.rearLeftWheel.brakeTorque, 16000f, "Eje trasero recibe 16000N (2x).");
        }

        // ===============================================================
        // 6. CAJA AUTOMÁTICA SECUENCIAL (INERCIA DE RPMs)
        // ===============================================================
        [UnityTest]
        public IEnumerator Test06_CajaAutomatica_Upshift_Y_Downshift_Secuencial()
        {
            isAutomaticSO.Value = true;
            
            // 1. Arrancar en Neutro (0)
            inputData.CurrentGear = 0; 
            vehicle.RestartEngine();
            yield return new WaitForFixedUpdate();

            // 2. Pasar a Drive (1)
            inputData.CurrentGear = 1;

            // 3. Simular velocidad lineal de 12 m/s (~43.2 km/h en 1ª marcha)
            rb.linearVelocity = new Vector3(0f, 0f, 12f);

            // 4. Dejar pasar 15 fotogramas físicos para dar tiempo a que Lerp suba las RPMs por encima de 4200
            for (int i = 0; i < 15; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.AreEqual(2, inputData.CurrentGear, "La caja automática debe subir a 2ª marcha tras acelerar (> 4200 RPM).");

            // 5. Frenar el coche a 0 m/s y dejar pasar 15 fotogramas para que las RPMs bajen (< 1800 RPM)
            rb.linearVelocity = Vector3.zero;
            for (int i = 0; i < 15; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.AreEqual(1, inputData.CurrentGear, "La caja automática debe reducir a 1ª marcha al frenar (< 1800 RPM).");
        }

        // ===============================================================
        // 7. GEOMETRÍA DE DIRECCIÓN (SteeringAngle)
        // ===============================================================
        [UnityTest]
        public IEnumerator Test07_GeometriaDeDireccion_Aplica_Angulo_Correcto_A_Ruedas()
        {
            vehicle.RestartEngine();
            inputData.Steering = 1.0f; // Giro a la derecha

            yield return new WaitForFixedUpdate();

            Assert.AreEqual(33f, vehicle.frontLeftWheel.steerAngle, "Rueda izquierda a 33°.");
            Assert.AreEqual(33f, vehicle.frontRightWheel.steerAngle, "Rueda derecha a 33°.");
        }
    }
}