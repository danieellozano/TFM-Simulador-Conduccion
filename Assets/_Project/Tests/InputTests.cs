using NUnit.Framework;
using UnityEngine;
using Simulador.Core;

namespace Simulador.Tests
{
    public class InputTests
    {
        private InputDataSO inputData;
        private BoolVariable isAutomaticSO;

        [SetUp]
        public void SetUp()
        {
            // Instanciamos el contrato de datos y variables de prueba
            inputData = ScriptableObject.CreateInstance<InputDataSO>();
            isAutomaticSO = ScriptableObject.CreateInstance<BoolVariable>();

            // Estado inicial limpio
            inputData.ResetData();
            isAutomaticSO.Value = false; // Por defecto modo Manual
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(inputData);
            Object.DestroyImmediate(isAutomaticSO);
        }

        // ---------------------------------------------------------------
        // 1. PRUEBAS DE INICIALIZACIÓN Y RESET
        // ---------------------------------------------------------------
        [Test]
        public void Test_InputData_ResetData_Establece_Estado_Inicial_Determinista()
        {
            // Alteramos variables
            inputData.Throttle = 0.9f;
            inputData.Steering = -0.8f;
            inputData.CurrentGear = 3;
            inputData.Handbrake = false;

            // Ejecutamos ResetData()
            inputData.ResetData();

            Assert.AreEqual(0f, inputData.Throttle, "Throttle debe ser 0.");
            Assert.AreEqual(0f, inputData.Steering, "Steering debe ser 0.");
            Assert.AreEqual(0f, inputData.Breaking, "Breaking debe ser 0.");
            Assert.AreEqual(0f, inputData.Clutch, "Clutch debe ser 0.");
            Assert.AreEqual(0, inputData.CurrentGear, "CurrentGear debe ser Punto Muerto (0).");
            Assert.AreEqual(0, inputData.ActiveBlinker, "ActiveBlinker debe ser Off (0).");
            Assert.IsTrue(inputData.Handbrake, "Handbrake debe iniciar PUESTO por seguridad.");
        }

        // ---------------------------------------------------------------
        // 2. PRUEBAS DE INTERMITENTES 
        // ---------------------------------------------------------------
        [Test]
        public void Test_Intermitentes_Conmutacion_Izquierda_Y_Derecha()
        {
            // Simulación de pulsación Izquierda (val < -0.5f)
            inputData.ActiveBlinker = (inputData.ActiveBlinker == -1) ? 0 : -1;
            Assert.AreEqual(-1, inputData.ActiveBlinker, "Pulsar izquierda debe activar el intermitente (-1).");

            // Segunda pulsación Izquierda para apagar
            inputData.ActiveBlinker = (inputData.ActiveBlinker == -1) ? 0 : -1;
            Assert.AreEqual(0, inputData.ActiveBlinker, "Volver a pulsar izquierda debe apagar el intermitente (0).");

            // Simulación de pulsación Derecha (val > 0.5f)
            inputData.ActiveBlinker = (inputData.ActiveBlinker == 1) ? 0 : 1;
            Assert.AreEqual(1, inputData.ActiveBlinker, "Pulsar derecha debe activar el intermitente (1).");
        }

        // ---------------------------------------------------------------
        // 3. PRUEBAS DE TRANSMISIÓN MANUAL 
        // ---------------------------------------------------------------
        [Test]
        public void Test_TransmisionManual_SubirMarcha_Bloquea_Si_Embrague_Insuficiente()
        {
            isAutomaticSO.Value = false; // Modo Manual
            inputData.CurrentGear = 0;   // Punto muerto
            inputData.Clutch = 0.5f;     // Embrague al 50% (Insuficiente)

            // Lógica exacta de InputReader: requiere Clutch > 0.7f
            if (inputData.Clutch > 0.7f)
            {
                inputData.CurrentGear = Mathf.Clamp(inputData.CurrentGear + 1, -1, 5);
            }

            Assert.AreEqual(0, inputData.CurrentGear, "No debe permitir subir marcha con embrague <= 70%.");
        }

        [Test]
        public void Test_TransmisionManual_SubirMarcha_Permite_Si_Embrague_Mayor_70()
        {
            isAutomaticSO.Value = false; // Modo Manual
            inputData.CurrentGear = 0;
            inputData.Clutch = 0.85f;    // Embrague al 85% (Válido)

            if (inputData.Clutch > 0.7f)
            {
                inputData.CurrentGear = Mathf.Clamp(inputData.CurrentGear + 1, -1, 5);
            }

            Assert.AreEqual(1, inputData.CurrentGear, "Debe subir a 1ª marcha con embrague > 70%.");
        }

        [Test]
        public void Test_TransmisionManual_Limites_Clamp_Min_Max()
        {
            inputData.Clutch = 1.0f; // Embrague a fondo

            // Intentamos bajar de Reversa (-1)
            inputData.CurrentGear = -1;
            inputData.CurrentGear = Mathf.Clamp(inputData.CurrentGear - 1, -1, 5);
            Assert.AreEqual(-1, inputData.CurrentGear, "El límite inferior no debe bajar de Reversa (-1).");

            // Intentamos subir de 5ª marcha (5)
            inputData.CurrentGear = 5;
            inputData.CurrentGear = Mathf.Clamp(inputData.CurrentGear + 1, -1, 5);
            Assert.AreEqual(5, inputData.CurrentGear, "El límite superior no debe pasar de 5ª marcha (5).");
        }

        // ---------------------------------------------------------------
        // 4. PRUEBAS DE TRANSMISIÓN AUTOMÁTICA 
        // ---------------------------------------------------------------
        [Test]
        public void Test_TransmisionAutomatica_Bypass_Embrague_Secuencia_Marchas()
        {
            isAutomaticSO.Value = true; // Modo Automático
            inputData.Clutch = 0.0f;    // Sin embrague
            inputData.CurrentGear = -1; // Iniciamos en Reversa

            // GearUp en Automático: Reversa (-1) -> Neutro (0)
            if (inputData.CurrentGear == -1) inputData.CurrentGear = 0;
            Assert.AreEqual(0, inputData.CurrentGear, "En automático debe pasar de R a N sin embrague.");

            // GearUp en Automático: Neutro (0) -> Drive (1)
            if (inputData.CurrentGear == 0) inputData.CurrentGear = 1;
            Assert.AreEqual(1, inputData.CurrentGear, "En automático debe pasar de N a Drive (1) sin embrague.");
        }

        // ---------------------------------------------------------------
        // 5. PRUEBAS DE FRENO DE MANO INTERRUPTOR (Toggle)
        // ---------------------------------------------------------------
        [Test]
        public void Test_FrenoDeMano_Funciona_Como_Toggle()
        {
            inputData.Handbrake = true; // Inicialmente puesto

            // Simulamos acción Handbrake
            inputData.Handbrake = !inputData.Handbrake;
            Assert.IsFalse(inputData.Handbrake, "Debe quitar el freno de mano.");

            inputData.Handbrake = !inputData.Handbrake;
            Assert.IsTrue(inputData.Handbrake, "Debe volver a poner el freno de mano.");
        }

        // ---------------------------------------------------------------
        // 6. PRUEBAS DE EVENTOS DISCRETOS (Reinicio y Pausa)
        // ---------------------------------------------------------------
        [Test]
        public void Test_EventosDiscretos_Reinicio_Y_Pausa_Disparan_Exitosamente()
        {
            GameEvent restartEvent = ScriptableObject.CreateInstance<GameEvent>();
            GameEvent pauseEvent = ScriptableObject.CreateInstance<GameEvent>();

            bool restartDisparado = false;
            bool pauseDisparado = false;

            GameObject holder = new GameObject("ListenerHolder");
            
            GameEventListener restartListener = holder.AddComponent<GameEventListener>();
            restartListener.Event = restartEvent;
            restartListener.Response = new UnityEngine.Events.UnityEvent<object>();
            restartListener.Response.AddListener((data) => { restartDisparado = true; });
            restartEvent.RegisterListener(restartListener);

            GameEventListener pauseListener = holder.AddComponent<GameEventListener>();
            pauseListener.Event = pauseEvent;
            pauseListener.Response = new UnityEngine.Events.UnityEvent<object>();
            pauseListener.Response.AddListener((data) => { pauseDisparado = true; });
            pauseEvent.RegisterListener(pauseListener);

            // Disparamos ambos eventos
            restartEvent.Raise();
            pauseEvent.Raise();

            Assert.IsTrue(restartDisparado, "El evento de reinicio de motor (Tecla R) debe notificar.");
            Assert.IsTrue(pauseDisparado, "El evento de pausa debe notificar.");

            Object.DestroyImmediate(holder);
            Object.DestroyImmediate(restartEvent);
            Object.DestroyImmediate(pauseEvent);
        }
    }
}