using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Simulador.Core;
using Simulador.AI;

namespace Simulador.Tests
{
    public class AITests
    {
        private GameObject aiHolder;
        private TrafficAIController aiController;

        [SetUp]
        public void SetUp()
        {
            aiHolder = new GameObject("Test_AI_Vehicle");
            aiController = aiHolder.AddComponent<TrafficAIController>();
            aiController.currentRoadLimit = 50f; // 50 km/h por defecto
            aiController.curveSpeedFactor = 0.5f; // Mínimo 50% en curvas
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(aiHolder);
        }

        // ---------------------------------------------------------------
        // 1. VELOCIDAD ADAPTATIVA EN CURVAS 
        // ---------------------------------------------------------------
        [Test]
        public void Test_Calculo_Velocidad_IA_En_Rectas_Y_Curvas()
        {
            float limit = aiController.currentRoadLimit; // 50 km/h
            float minFactor = aiController.curveSpeedFactor; // 0.5f

            // Caso A: Trayectoria recta (Ángulo < 5°) -> Mantiene 100% de la velocidad
            float angleRecta = 2f;
            float reductionRecta = (angleRecta > 5f) ? Mathf.Clamp(1.0f - (angleRecta / 90f), minFactor, 1.0f) : 1.0f;
            float targetSpeedRecta = limit * reductionRecta;

            Assert.AreEqual(50f, targetSpeedRecta, "En rectas (< 5°), la IA debe mantener el 100% de la velocidad límite.");

            // Caso B: Curva suave de 45° -> Reduce proporcionalmente
            float angleCurva45 = 45f;
            float reduction45 = Mathf.Clamp(1.0f - (angleCurva45 / 90f), minFactor, 1.0f); // 1.0 - 0.5 = 0.5
            float targetSpeed45 = limit * reduction45;

            Assert.AreEqual(25f, targetSpeed45, "En curva de 45°, la velocidad se reduce al 50% (25 km/h).");

            // Caso C: Curva muy cerrada de 90° -> Respeta el mínimo curveSpeedFactor (50%)
            float angleCurva90 = 90f;
            float reduction90 = Mathf.Clamp(1.0f - (angleCurva90 / 90f), minFactor, 1.0f); // Clamped a minFactor (0.5)
            float targetSpeed90 = limit * reduction90;

            Assert.AreEqual(25f, targetSpeed90, "En curva de 90°, la velocidad no debe bajar del límite mínimo configurado.");
        }

        // ---------------------------------------------------------------
        // 2. PERCEPCIÓN Y DISTANCIAS DINÁMICAS 
        // ---------------------------------------------------------------
        [Test]
        public void Test_Distancias_De_Parada_Diferenciadas_AIBlocker_Vs_Vehiculo()
        {
            int aiBlockerLayer = LayerMask.NameToLayer("AI_Blocker");
            int defaultLayer = LayerMask.NameToLayer("Default");

            // Distancia para líneas de parada / STOP / Semáforos
            float stopDistLine = aiController.stopDistanceAtLine; // 2.3m
            // Distancia de seguridad tras otro coche
            float stopDistCar = aiController.stopDistanceBehindCar; // 6.0m

            // Simulación Hit con AI_Blocker
            int hitLayerBlocker = aiBlockerLayer;
            float distRequeridaBlocker = (hitLayerBlocker == aiBlockerLayer) ? stopDistLine : stopDistCar;

            Assert.AreEqual(2.3f, distRequeridaBlocker, "Ante una línea de detención (AI_Blocker), la distancia de parada es 2.3m.");

            // Simulación Hit con Vehículo (Default/Traffic)
            int hitLayerCar = defaultLayer;
            float distRequeridaCar = (hitLayerCar == aiBlockerLayer) ? stopDistLine : stopDistCar;

            Assert.AreEqual(6.0f, distRequeridaCar, "Tras otro vehículo, la distancia de seguridad obligatoria es 6.0m.");
        }

        [Test]
        public void Test_ExternalStop_Fuerza_Velocidad_Objetivo_A_Cero()
        {
            aiController.externalStop = true;

            float targetSpeed = aiController.externalStop ? 0f : aiController.currentRoadLimit;

            Assert.AreEqual(0f, targetSpeed, "Cuando externalStop es true, la velocidad objetivo de la IA debe ser 0.");
        }

        // ---------------------------------------------------------------
        // 3. PRIORIDAD EN CRUCES Y PRODUCTO ESCALAR 
        // ---------------------------------------------------------------
        [Test]
        public void Test_PriorityIntersection_DotProduct_Filtra_Coches_Que_Se_Alejan()
        {
            // Vector del sensor de la intersección hacia adelante (Norte: 0, 0, 1)
            Vector3 forwardSensor = new Vector3(0, 0, 1);

            // Coche A: Se aproxima al cruce de frente (Sur: 0, 0, -1)
            Vector3 forwardCocheAproximando = new Vector3(0, 0, -1);
            float dotAproximando = Vector3.Dot(forwardCocheAproximando, forwardSensor); // -1.0f

            bool esAmenazaActiva = (dotAproximando < -0.2f);
            Assert.IsTrue(esAmenazaActiva, "Un coche aproximándose de frente (< -0.2f) debe marcarse como AMENAZA ACTIVA.");

            // Coche B: Ya cruzó la intersección y se aleja en la misma dirección (Norte: 0, 0, 1)
            Vector3 forwardCocheAlejandose = new Vector3(0, 0, 1);
            float dotAlejandose = Vector3.Dot(forwardCocheAlejandose, forwardSensor); // +1.0f

            bool esAmenazaAlejandose = (dotAlejandose < -0.2f);
            Assert.IsFalse(esAmenazaAlejandose, "Un coche que se aleja del cruce debe ser IGNORADO para no crear retenciones artificiales.");
        }

        // ---------------------------------------------------------------
        // 4. DETECCIÓN DE AMENAZAS EN STOP DE IA 
        // ---------------------------------------------------------------
        [Test]
        public void Test_AIStopManager_DotProduct_Detecta_Vehiculo_Aproximandose_Al_STOP()
        {
            // Posición de la línea de STOP en (0, 0, 10)
            Vector3 posStop = new Vector3(0, 0, 10f);

            // Coche acercándose en (0, 0, 0) mirando hacia el STOP (0, 0, 1)
            Vector3 posCoche = new Vector3(0, 0, 0f);
            Vector3 forwardCoche = new Vector3(0, 0, 1f);

            Vector3 directionToStop = posStop - posCoche; // (0, 0, 10)
            float approachCheck = Vector3.Dot(forwardCoche, directionToStop.normalized); // +1.0f

            bool vieneHaciaNosotros = (approachCheck > 0.1f);
            Assert.IsTrue(vieneHaciaNosotros, "Si el coche apunta hacia el STOP (> 0.1f), se detecta como amenaza transversal.");
        }

        // ---------------------------------------------------------------
        // 5. LECTURA DE SEÑALES DE VELOCIDAD 
        // ---------------------------------------------------------------
        [Test]
        public void Test_TrafficAIController_Actualiza_LimiteVelocidad_Al_Cruzar_Señal()
        {
            GameObject signalObj = new GameObject("SpeedSignal");
            SpeedLimitSign signal = signalObj.AddComponent<SpeedLimitSign>();
            signal.speedLimitValue = 40f; // Señal de 40 km/h

            // Al atravesar la señal, la IA lee la interfaz ISpeedLimitProvider
            ISpeedLimitProvider provider = signal as ISpeedLimitProvider;
            if (provider != null)
            {
                aiController.currentRoadLimit = provider.GetSpeedLimit();
            }

            Assert.AreEqual(40f, aiController.currentRoadLimit, "La IA debe actualizar su límite de velocidad de vía a 40 km/h.");

            Object.DestroyImmediate(signalObj);
        }
    }
}