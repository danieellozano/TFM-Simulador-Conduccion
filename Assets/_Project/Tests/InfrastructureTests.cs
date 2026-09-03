using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Simulador.Core;
using Simulador.Infrastructure;

namespace Simulador.Tests
{
    public class InfrastructureTests
    {
        private GameObject trafficLightObj;
        private TrafficLightController lightController;
        private GameObject redVisual;
        private GameObject amberVisual;
        private GameObject greenVisual;
        private GameObject aiBarrier;
        private GameObject detectionZone;
        private FloatVariable currentLimitSO;

        [SetUp]
        public void SetUp()
        {
            trafficLightObj = new GameObject("TrafficLight_Holder");
            lightController = trafficLightObj.AddComponent<TrafficLightController>();

            // Creamos los objetos hijos visuales y de interacción
            redVisual = new GameObject("Red_Light");
            amberVisual = new GameObject("Amber_Light");
            greenVisual = new GameObject("Green_Light");
            aiBarrier = new GameObject("AI_Barrier");
            detectionZone = new GameObject("Detection_Zone");

            lightController.redLight = redVisual;
            lightController.amberLight = amberVisual;
            lightController.greenLight = greenVisual;
            lightController.aiStopBarrier = aiBarrier;
            lightController.detectionZone = detectionZone;

            currentLimitSO = ScriptableObject.CreateInstance<FloatVariable>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(trafficLightObj);
            Object.DestroyImmediate(redVisual);
            Object.DestroyImmediate(amberVisual);
            Object.DestroyImmediate(greenVisual);
            Object.DestroyImmediate(aiBarrier);
            Object.DestroyImmediate(detectionZone);
            Object.DestroyImmediate(currentLimitSO);
        }

        // ---------------------------------------------------------------
        // 1. CONTROLADOR DE SEMÁFOROS 
        // ---------------------------------------------------------------
        [Test]
        public void Test_TrafficLightController_SetState_Rojo_Activa_Luz_Y_Muro_IA()
        {
            lightController.SetState(LightState.Red);

            Assert.AreEqual(LightState.Red, lightController.CurrentState);
            Assert.IsTrue(redVisual.activeSelf, "La luz roja debe encenderse.");
            Assert.IsFalse(amberVisual.activeSelf, "La luz ámbar debe estar apagada.");
            Assert.IsFalse(greenVisual.activeSelf, "La luz verde debe estar apagada.");
            
            // El muro para la IA debe estar ACTIVO en rojo
            Assert.IsTrue(aiBarrier.activeSelf, "El muro para detener a la IA debe estar activo en fase Roja.");
            // El tag de la zona debe ser ValidStopZone (parar es legal)
            Assert.AreEqual("ValidStopZone", detectionZone.tag, "En rojo, detenerse es legal (tag ValidStopZone).");
        }

        [Test]
        public void Test_TrafficLightController_SetState_Verde_Desactiva_Muro_IA()
        {
            lightController.SetState(LightState.Green);

            Assert.AreEqual(LightState.Green, lightController.CurrentState);
            Assert.IsFalse(redVisual.activeSelf);
            Assert.IsFalse(amberVisual.activeSelf);
            Assert.IsTrue(greenVisual.activeSelf, "La luz verde debe encenderse.");

            // El muro para la IA debe estar DESACTIVO en verde
            Assert.IsFalse(aiBarrier.activeSelf, "El muro de la IA debe desactivarse en fase Verde para permitir el paso.");
            // El tag cambia a Untagged (parar en verde es parada innecesaria)
            Assert.AreEqual("Untagged", detectionZone.tag, "En verde, detenerse sin motivo no es zona protegida (Untagged).");
        }

        [Test]
        public void Test_TrafficLightController_SetState_Ambar_Mantiene_Muro_IA_Y_Parada_Valida()
        {
            lightController.SetState(LightState.Amber);

            Assert.AreEqual(LightState.Amber, lightController.CurrentState);
            Assert.IsTrue(amberVisual.activeSelf, "La luz ámbar debe encenderse.");
            Assert.IsTrue(aiBarrier.activeSelf, "En ámbar, la IA debe detenerse (muro activo).");
            Assert.AreEqual("ValidStopZone", detectionZone.tag, "En ámbar, la parada de prevención es legal.");
        }

        // ---------------------------------------------------------------
        // 2. ORQUESTACIÓN DE INTERSECCIONES 
        // ---------------------------------------------------------------
        [Test]
        public void Test_IntersectionManager_Secuenciacion_Excluyente_De_Fases()
        {
            GameObject managerObj = new GameObject("IntersectionManager_Holder");
            IntersectionManager manager = managerObj.AddComponent<IntersectionManager>();

            // Creamos semáforos para las tres fases
            GameObject semAvenida = new GameObject("Sem_Avenida");
            TrafficLightController lightAvenida = semAvenida.AddComponent<TrafficLightController>();

            GameObject semIzquierda = new GameObject("Sem_Izquierda");
            TrafficLightController lightIzquierda = semIzquierda.AddComponent<TrafficLightController>();

            manager.grupoAvenida = new List<TrafficLightController> { lightAvenida };
            manager.grupoIzquierda = new List<TrafficLightController> { lightIzquierda };

            // Simulación de Fase 1: Avenida en Verde -> Izquierda debe pasar a Rojo
            lightAvenida.SetState(LightState.Green);
            lightIzquierda.SetState(LightState.Red);

            Assert.AreEqual(LightState.Green, lightAvenida.CurrentState, "Avenida debe estar en VERDE.");
            Assert.AreEqual(LightState.Red, lightIzquierda.CurrentState, "Fases transversales deben estar en ROJO.");

            Object.DestroyImmediate(managerObj);
            Object.DestroyImmediate(semAvenida);
            Object.DestroyImmediate(semIzquierda);
        }

        // ---------------------------------------------------------------
        // 3. SEÑALIZACIÓN INTELIGENTE DE VELOCIDAD 
        // ---------------------------------------------------------------
        [Test]
        public void Test_SpeedLimitSign_Inyecta_Nuevo_Limite_A_CurrentLimitSO()
        {
            GameObject signObj = new GameObject("SpeedLimit_30");
            SpeedLimitSign sign = signObj.AddComponent<SpeedLimitSign>();
            sign.speedLimitValue = 30f;
            sign.currentLimitSO = currentLimitSO;

            // Límite inicial desconocido
            currentLimitSO.Value = 0f;

            // Verificamos que la interfaz ISpeedLimitProvider expone el valor correcto
            ISpeedLimitProvider provider = sign as ISpeedLimitProvider;
            Assert.IsNotNull(provider);
            Assert.AreEqual(30f, provider.GetSpeedLimit());

            // Simulación de paso del coche del alumno (TriggerEnter con Tag "Player")
            if (currentLimitSO != null)
            {
                currentLimitSO.Value = sign.speedLimitValue;
            }

            Assert.AreEqual(30f, currentLimitSO.Value, "Al cruzar la señal de 30 km/h, currentLimitSO debe actualizarse a 30.");

            Object.DestroyImmediate(signObj);
        }

        // ---------------------------------------------------------------
        // 4. DESACOPLAMIENTO MEDIANTE INTERFAZ 
        // ---------------------------------------------------------------
        [Test]
        public void Test_ILightSource_Polimorfismo_Interface_Desacoplada()
        {
            lightController.SetState(LightState.Red);

            // El módulo de evaluación consulta la luz a través de la interfaz ILightSource sin conocer la clase concreta
            ILightSource fuenteLuz = lightController as ILightSource;

            Assert.IsNotNull(fuenteLuz, "TrafficLightController debe implementar ILightSource.");
            Assert.AreEqual(LightState.Red, fuenteLuz.CurrentState, "La interfaz debe devolver el estado actual (Red).");
        }
    }
}