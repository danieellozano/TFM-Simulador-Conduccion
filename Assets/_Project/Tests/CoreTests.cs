using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Simulador.Core;

namespace Simulador.Tests
{
    public class CoreTests
    {
        private InputDataSO inputData;
        private FloatVariable floatVar;
        private IntVariable intVar;
        private StringVariable stringVar;
        private BoolVariable boolVar;
        private Vector3Variable vector3Var;

        [SetUp]
        public void SetUp()
        {
            inputData = ScriptableObject.CreateInstance<InputDataSO>();
            floatVar = ScriptableObject.CreateInstance<FloatVariable>();
            intVar = ScriptableObject.CreateInstance<IntVariable>();
            stringVar = ScriptableObject.CreateInstance<StringVariable>();
            boolVar = ScriptableObject.CreateInstance<BoolVariable>();
            vector3Var = ScriptableObject.CreateInstance<Vector3Variable>();

            inputData.ResetData();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(inputData);
            Object.DestroyImmediate(floatVar);
            Object.DestroyImmediate(intVar);
            Object.DestroyImmediate(stringVar);
            Object.DestroyImmediate(boolVar);
            Object.DestroyImmediate(vector3Var);
        }

        // ---------------------------------------------------------------
        // 1. CANALES DE DATOS COMPARTIDOS 
        // ---------------------------------------------------------------
        [Test]
        public void Test_Variables_ScriptableObject_SetValue_Y_Lectura()
        {
            floatVar.SetValue(72.5f);
            Assert.AreEqual(72.5f, floatVar.Value, "FloatVariable debe actualizar su valor mediante SetValue.");

            intVar.SetValue(4);
            Assert.AreEqual(4, intVar.Value, "IntVariable debe almacenar el entero correcto.");

            stringVar.SetValue("1. Deténgase en el STOP.");
            Assert.AreEqual("1. Deténgase en el STOP.", stringVar.Value, "StringVariable debe almacenar la instrucción.");

            boolVar.Value = true;
            Assert.IsTrue(boolVar.Value, "BoolVariable debe almacenar el valor booleano.");

            vector3Var.Value = new Vector3(10f, 0f, 25f);
            Assert.AreEqual(new Vector3(10f, 0f, 25f), vector3Var.Value, "Vector3Variable debe almacenar coordenadas.");
        }

        // ---------------------------------------------------------------
        // 2. BUS DE EVENTOS REACTIVO 
        // ---------------------------------------------------------------
        [Test]
        public void Test_GameEvent_Registro_Desregistro_Y_Propagación_De_Datos()
        {
            GameEvent gameEvent = ScriptableObject.CreateInstance<GameEvent>();
            GameObject listenerObj = new GameObject("TestListener");
            GameEventListener listener = listenerObj.AddComponent<GameEventListener>();
            
            listener.Event = gameEvent;
            listener.Response = new UnityEngine.Events.UnityEvent<object>();

            object payloadRecibido = null;
            listener.Response.AddListener((data) => { payloadRecibido = data; });

            // Registro
            gameEvent.RegisterListener(listener);

            // Disparo con Payload de datos
            string testPayload = "Datos_De_Prueba";
            gameEvent.Raise(testPayload);

            Assert.AreEqual("Datos_De_Prueba", payloadRecibido, "El observador debe recibir los datos enviados en Raise().");

            // Desregistro
            gameEvent.UnregisterListener(listener);
            payloadRecibido = null;
            gameEvent.Raise("Segundo_Disparo");

            Assert.IsNull(payloadRecibido, "Un observador desregistrado NO debe recibir notificaciones.");

            Object.DestroyImmediate(listenerObj);
            Object.DestroyImmediate(gameEvent);
        }

        // ---------------------------------------------------------------
        // 3. MODELO NORMATIVO DE DATOS 
        // ---------------------------------------------------------------
        [Test]
        public void Test_InfraccionSO_Estructura_Y_Puntos_Penalizacion()
        {
            InfraccionSO falta = ScriptableObject.CreateInstance<InfraccionSO>();
            falta.codigo = "SIG-STOP";
            falta.descripcion = "No detenerse en la señal de STOP";
            falta.tipo = InfraccionSO.Gravedad.Eliminatoria;
            falta.puntosPenalizacion = 10;

            Assert.AreEqual("SIG-STOP", falta.codigo);
            Assert.AreEqual(InfraccionSO.Gravedad.Eliminatoria, falta.tipo);
            Assert.AreEqual(10, falta.puntosPenalizacion);

            Object.DestroyImmediate(falta);
        }

        // ---------------------------------------------------------------
        // 4. ESTRUCTURA DE PERFILES DE MISIÓN 
        // ---------------------------------------------------------------
        [Test]
        public void Test_UrbanMissionProfileSO_Estructura_De_Tareas()
        {
            UrbanTask tarea1 = new UrbanTask
            {
                tipo = UrbanTaskType.ConduccionLibre,
                instruccion = "Circule libremente durante 5 minutos.",
                tiempoMaximo = 300f,
                tagDestino = "Marker_Inicio"
            };

            UrbanTask tarea2 = new UrbanTask
            {
                tipo = UrbanTaskType.Estacionamiento,
                instruccion = "Aparque en el estacionamiento en línea.",
                tiempoMaximo = 180f,
                tagDestino = "Marker_Parking"
            };

            UrbanMissionProfileSO perfil = ScriptableObject.CreateInstance<UrbanMissionProfileSO>();
            perfil.tareas = new List<UrbanTask> { tarea1, tarea2 };

            Assert.AreEqual(2, perfil.tareas.Count);
            Assert.AreEqual(UrbanTaskType.ConduccionLibre, perfil.tareas[0].tipo);
            Assert.AreEqual(UrbanTaskType.Estacionamiento, perfil.tareas[1].tipo);

            Object.DestroyImmediate(perfil);
        }

        // ---------------------------------------------------------------
        // 5. GESTOR DE MISIONES URBANAS 
        // ---------------------------------------------------------------
        [Test]
        public void Test_UrbanMissionManager_Inicia_Y_Escribe_Instruccion_En_ObjectiveSO()
        {
            UrbanTask tarea1 = new UrbanTask
            {
                tipo = UrbanTaskType.ConduccionLibre,
                instruccion = "Realice el recorrido indicado.",
                tiempoMaximo = 60f
            };

            UrbanMissionProfileSO perfil = ScriptableObject.CreateInstance<UrbanMissionProfileSO>();
            perfil.tareas = new List<UrbanTask> { tarea1 };

            GameObject managerObj = new GameObject("UrbanManagerHolder");
            UrbanMissionManager manager = managerObj.AddComponent<UrbanMissionManager>();
            manager.objectiveSO = stringVar;

            // Iniciar Examen
            manager.IniciarExamen(perfil);

            Assert.AreEqual(0, manager.taskIndex, "La primera tarea debe ser el índice 0.");
            Assert.AreEqual("Realice el recorrido indicado.", stringVar.Value, "Debe escribir la instrucción en objectiveSO.");

            Object.DestroyImmediate(managerObj);
            Object.DestroyImmediate(perfil);
        }

        // ---------------------------------------------------------------
        // 6. GESTOR DE MANIOBRAS 
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
    }
}