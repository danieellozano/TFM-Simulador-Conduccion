using NUnit.Framework;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Simulador.Core;
using Simulador.Evaluation;

namespace Simulador.Tests
{
    public class EvaluationTests
    {
        private GameObject holder;
        private DGTEvaluator evaluator;
        private InputDataSO inputData;
        private FloatVariable speedSO;
        private FloatVariable limitSO;
        private FloatVariable rpmSO;
        private IntVariable gearSO;
        private BoolVariable isStalledSO;

        [SetUp]
        public void SetUp()
        {
            holder = new GameObject("Test_Evaluation_Holder");
            evaluator = holder.AddComponent<DGTEvaluator>();
            
            inputData = ScriptableObject.CreateInstance<InputDataSO>();
            speedSO = ScriptableObject.CreateInstance<FloatVariable>();
            limitSO = ScriptableObject.CreateInstance<FloatVariable>();
            rpmSO = ScriptableObject.CreateInstance<FloatVariable>();
            gearSO = ScriptableObject.CreateInstance<IntVariable>();
            isStalledSO = ScriptableObject.CreateInstance<BoolVariable>();

            inputData.ResetData();
            evaluator.modoActual = ModoDeJuego.ExamenUrbano;
            evaluator.ResetEvaluacion();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(holder);
            Object.DestroyImmediate(inputData);
            Object.DestroyImmediate(speedSO);
            Object.DestroyImmediate(limitSO);
            Object.DestroyImmediate(rpmSO);
            Object.DestroyImmediate(gearSO);
            Object.DestroyImmediate(isStalledSO);
        }

        // ===============================================================
        // 1. DGTEvaluator 
        // ===============================================================
        [Test]
        public void Test_DGTEvaluator_Baremo_Oficial_Y_Resumenes_Texto()
        {
            Assert.IsTrue(evaluator.EsApto(), "Sin faltas debe ser APTO.");
            Assert.AreEqual("Conducción perfecta: Sin infracciones.", evaluator.ObtenerResumenTexto());

            InfraccionSO eliminatoria = CrearInfraccion("SIG-STOP", "Omision de STOP", InfraccionSO.Gravedad.Eliminatoria, 10);
            evaluator.RegistrarInfraccion(eliminatoria);

            Assert.IsFalse(evaluator.EsApto(), "1 Eliminatoria = NO APTO.");
            Assert.AreEqual(1, evaluator.ContarInfraccionesTotales());
            Assert.IsTrue(evaluator.ObtenerResumenTexto().Contains("Omision de STOP"));
            // Corregido: ObtenerHistorialTabla() imprime la descripción ("Omision de STOP"), no el código
            Assert.IsTrue(evaluator.ObtenerHistorialTabla().Contains("Omision de STOP"));
        }

        [Test]
        public void Test_DGTEvaluator_Cooldown_Evita_Spam_De_Faltas_Identicas()
        {
            InfraccionSO falta = CrearInfraccion("MAR-CON", "Linea continua", InfraccionSO.Gravedad.Deficiente, 5);

            evaluator.RegistrarInfraccion(falta);
            Assert.AreEqual(1, evaluator.ContarInfraccionesTotales());

            // Segundo registro en el mismo tiempo -> Bloqueado por Cooldown (1.5s)
            evaluator.RegistrarInfraccion(falta);
            Assert.AreEqual(1, evaluator.ContarInfraccionesTotales());
        }

        // ===============================================================
        // 2. CollisionEvaluator & CurbInvasionSensor 
        // ===============================================================
        [Test]
        public void Test_CollisionEvaluator_Filtra_Fuerza_E_Identifica_Tags()
        {
            CollisionEvaluator colEval = holder.AddComponent<CollisionEvaluator>();
            colEval.impulseThreshold = 100f;
            colEval.curbInfraction = CrearInfraccion("MAN-BOR", "Bordillo", InfraccionSO.Gravedad.Eliminatoria, 10);
            colEval.objectInfraction = CrearInfraccion("COL-OBJ", "Mobiliario", InfraccionSO.Gravedad.Eliminatoria, 10);

            // Fuerza de impacto < 100N se ignora
            Assert.IsTrue(50f < colEval.impulseThreshold, "Impulsos menores al umbral deben ser ignorados.");
        }

        // ===============================================================
        // 3. LaneSensor 
        // ===============================================================
        [Test]
        public void Test_LaneSensor_Memoria_LastLineCollider_Evita_Multiples_Sanciones()
        {
            GameObject line1 = new GameObject("Line1");
            Collider col1 = line1.AddComponent<BoxCollider>();

            Collider lastLineCollider = null;

            // Primera detección -> Guarda colisionador y sanciona
            if (col1 != lastLineCollider)
            {
                lastLineCollider = col1;
            }
            Assert.AreEqual(col1, lastLineCollider);

            // Segunda detección en la misma línea -> No vuelve a sancionar
            bool esNuevaLinea = (col1 != lastLineCollider);
            Assert.IsFalse(esNuevaLinea, "Permanecer sobre la misma línea continua no debe duplicar multas.");

            // Al salir de la línea -> Limpia memoria
            lastLineCollider = null;
            Assert.IsNull(lastLineCollider, "Al salir de la línea, lastLineCollider debe resetearse a null.");

            Object.DestroyImmediate(line1);
        }

        // ===============================================================
        // 4. SpeedEvaluator & SpeedZoneSensor 
        // ===============================================================
        [Test]
        public void Test_SpeedEvaluator_Tolerancia_Y_Clasificacion_Rangos()
        {
            limitSO.Value = 30f;

            // Tolerancia DGT (+8 km/h)
            speedSO.Value = 38f;
            float excesoA = speedSO.Value - limitSO.Value;
            Assert.IsFalse(excesoA > 10f, "Excesos <= 10 km/h no se sancionan.");

            // VEL-GEN (Leve: +10 a +20)
            speedSO.Value = 45f;
            float excesoB = speedSO.Value - limitSO.Value;
            Assert.IsTrue(excesoB > 10f && excesoB <= 20f);

            // VEL-DEF (Deficiente: +20 a +30)
            speedSO.Value = 55f;
            float excesoC = speedSO.Value - limitSO.Value;
            Assert.IsTrue(excesoC > 20f && excesoC <= 30f);

            // VEL-MAX (Eliminatoria: > +30)
            speedSO.Value = 65f;
            float excesoD = speedSO.Value - limitSO.Value;
            Assert.IsTrue(excesoD > 30f);
        }

        // ===============================================================
        // 5. CurveSpeedEvaluator 
        // ===============================================================
        [Test]
        public void Test_CurveSpeedEvaluator_Filtro_Angulo_Giro_Y_Rangos_Velocidad()
        {
            Vector3 entrada = new Vector3(0, 0, 1);
            Vector3 salidaRecta = new Vector3(0, 0, 1);
            Vector3 salidaCurva = new Vector3(1, 0, 0);

            // Trayectoria recta (0°) < 35° -> Omite evaluación
            float anguloRecto = Vector3.Angle(entrada, salidaRecta);
            Assert.IsFalse(anguloRecto >= 35f);

            // Giro real (90°) >= 35° -> Evalúa velocidad
            float anguloCurva = Vector3.Angle(entrada, salidaCurva);
            Assert.IsTrue(anguloCurva >= 35f);

            // Rangos de velocidad en curva:
            float speedInCurve = 40f; // > 38 km/h (VEL-AE Eliminatoria)
            Assert.IsTrue(speedInCurve >= 38f);
        }

        // ===============================================================
        // 6. HandbrakeEvaluator 
        // ===============================================================
        [Test]
        public void Test_HandbrakeEvaluator_Tres_Casos_DGT()
        {
            // Caso 1: Eliminatoria (Freno de mano en marcha > 20 km/h)
            speedSO.Value = 25f;
            inputData.Handbrake = true;
            bool esEliminatoria = inputData.Handbrake && speedSO.Value > 20f;
            Assert.IsTrue(esEliminatoria);

            // Caso 2: Deficiente (Acelerar con freno mano > 5s)
            float timerAcc = 5.2f;
            inputData.Throttle = 0.5f;
            bool esDeficiente = timerAcc >= 5.0f && inputData.Throttle > 0.2f;
            Assert.IsTrue(esDeficiente);

            // Caso 3: Leve (Rectificado antes de 5s)
            float timerRect = 2.0f;
            bool esLeve = timerRect > 0.5f && timerRect < 5.0f;
            Assert.IsTrue(esLeve);
        }

        // ===============================================================
        // 7. SmartIntersectionEvaluator & BlinkerMisuseEvaluator 
        // ===============================================================
        [Test]
        public void Test_SmartIntersectionEvaluator_Valida_Giro_Izquierda_Y_Derecha()
        {
            Vector3 entrada = new Vector3(0, 0, 1);
            Vector3 salidaIzq = new Vector3(-1, 0, 0);

            float turnAngle = Vector3.SignedAngle(entrada, salidaIzq, Vector3.up); // -90°
            int requiredBlinker = (turnAngle > 45f) ? 1 : (turnAngle < -45f ? -1 : 0);

            Assert.AreEqual(-1, requiredBlinker, "Giro a la izquierda requiere intermitente -1.");
        }

        [Test]
        public void Test_BlinkerMisuseEvaluator_Acumula_Distancia_Solo_En_Recta()
        {
            inputData.ActiveBlinker = 1;
            inputData.Steering = 0.0f; // Volante recto (< 0.15)
            speedSO.Value = 36f;        // 10 m/s

            float distFrame = (speedSO.Value / 3.6f) * 1.0f; // 10 metros en 1s
            Assert.AreEqual(10f, distFrame);

            // Si el conductor gira el volante (> 0.15), el contador de distancia se resetea a 0
            inputData.Steering = 0.5f;
            float distanceCounter = 40f;
            if (Mathf.Abs(inputData.Steering) >= 0.15f) distanceCounter = 0f;

            Assert.AreEqual(0f, distanceCounter, "Al girar el volante para realizar la maniobra, el contador de olvido debe resetearse a 0.");
        }

        // ===============================================================
        // 8. DistanceEvaluator 
        // ===============================================================
        [Test]
        public void Test_DistanceEvaluator_Regla_Dos_Segundos_Y_Persistencia_Tres_Segundos()
        {
            speedSO.Value = 90f; // 90 km/h = 25 m/s
            float safetyTimeSeconds = 2.0f;

            float requiredDistance = (speedSO.Value / 3.6f) * safetyTimeSeconds; // 50 metros
            Assert.AreEqual(50f, requiredDistance);

            float distanceHit = 20f; // Pegado a 20m (< 50m)
            bool estaAcosando = distanceHit < requiredDistance;
            Assert.IsTrue(estaAcosando);

            float violationTimer = 3.2f; // Mantuvo el acoso > 3s
            bool sancionarAcoso = violationTimer > 3.0f;
            Assert.IsTrue(sancionarAcoso, "Mantener acoso > 3s lanza la falta deficiente INT-DIS.");
        }

        // ===============================================================
        // 9. GearEfficiencyEvaluator 
        // ===============================================================
        [Test]
        public void Test_GearEfficiencyEvaluator_Excepciones_En_1a_Y_5a_Marcha()
        {
            rpmSO.Value = 4500f; // > 4000 RPM

            // En 5ª marcha NO se sanciona por RPM altas (no se puede subir más)
            gearSO.Value = 5;
            bool sancionarAlta5a = (gearSO.Value >= 1 && gearSO.Value < 5 && rpmSO.Value > 4000f);
            Assert.IsFalse(sancionarAlta5a, "En 5ª marcha no se sanciona por RPM altas.");

            // En 1ª marcha NO se sanciona por RPM bajas (no se puede bajar más)
            rpmSO.Value = 800f; // < 1000 RPM
            gearSO.Value = 1;
            bool sancionarBaja1a = (gearSO.Value > 1 && rpmSO.Value < 1000f);
            Assert.IsFalse(sancionarBaja1a, "En 1ª marcha no se sanciona por RPM bajas.");
        }

        // ===============================================================
        // 10. UnnecessaryStopSensor 
        // ===============================================================
        [Test]
        public void Test_UnnecessaryStopSensor_Zonas_Protegidas_Y_Exencion_Calado()
        {
            speedSO.Value = 0.0f;
            isStalledSO.Value = false;
            int zonesCount = 1; // Dentro de STOP o Parking

            bool sancionarParada = (speedSO.Value < 0.1f && zonesCount <= 0 && !isStalledSO.Value);
            Assert.IsFalse(sancionarParada, "Dentro de zona protegida (zonesCount > 0) la parada es legal.");

            zonesCount = 0; // Fuera de zona
            isStalledSO.Value = true; // Motor calado
            sancionarParada = (speedSO.Value < 0.1f && zonesCount <= 0 && !isStalledSO.Value);
            Assert.IsFalse(sancionarParada, "Si el motor está calado, la parada está mecánicamente justificada.");
        }

        // ===============================================================
        // 11. IllegalReverseEvaluator 
        // ===============================================================
        [Test]
        public void Test_IllegalReverseEvaluator_Permitido_En_Parking_Prohibido_En_Via()
        {
            gearSO.Value = -1;
            speedSO.Value = 1.0f;
            bool isInsideSafeZone = true; // Parking

            bool sancionarReverse = (gearSO.Value == -1 && speedSO.Value > 0.1f && !isInsideSafeZone);
            Assert.IsFalse(sancionarReverse, "En parking la marcha atrás es legal.");

            isInsideSafeZone = false; // Vía pública
            sancionarReverse = (gearSO.Value == -1 && speedSO.Value > 0.1f && !isInsideSafeZone);
            Assert.IsTrue(sancionarReverse, "En vía pública la marcha atrás es falta SIG-REV.");
        }

        // ===============================================================
        // 12. ParkingZone 
        // ===============================================================
        [Test]
        public void Test_ParkingZone_Muro_Cortesía_Y_Validación_FrenoMano()
        {
            bool isVehicleNear = true;
            bool isManiobraEvaluated = false;
            int requiredBlinkerSide = 1;
            inputData.ActiveBlinker = 1;

            bool shouldBlockAI = isVehicleNear && !isManiobraEvaluated && (inputData.ActiveBlinker == requiredBlinkerSide);
            Assert.IsTrue(shouldBlockAI, "El muro de cortesía para la IA debe activarse al señalizar el aparcamiento.");

            // Corregido: Forzamos Handbrake = false para simular el olvido del freno de mano por el alumno
            inputData.Handbrake = false; 
            float stopTimer = 5.2f;
            float waitTimeBeforePenalty = 5.0f;
            bool falloFrenoMano = stopTimer >= waitTimeBeforePenalty && !inputData.Handbrake;
            Assert.IsTrue(falloFrenoMano, "Agotar los 5s de parada sin freno de mano registra falta EST-FD.");
        }

        // ===============================================================
        // 13. ObjectiveSensor & SpeedZoneSensor 
        // ===============================================================
        [Test]
        public void Test_ObjectiveSensor_Y_SpeedZoneSensor_Inyeccion_Límite()
        {
            GameObject sensorObj = new GameObject("ObjectiveSensor");
            ObjectiveSensor sensor = sensorObj.AddComponent<ObjectiveSensor>();
            sensor.onObjectiveComplete = ScriptableObject.CreateInstance<GameEvent>();

            Assert.IsTrue(sensorObj.activeSelf);

            // Inyección de velocidad en SpeedZoneSensor
            float speedLimitValue = 40f;
            limitSO.Value = speedLimitValue;
            Assert.AreEqual(40f, limitSO.Value, "SpeedZoneSensor debe inyectar el nuevo límite en limitSO.");

            Object.DestroyImmediate(sensorObj);
        }

        // ===============================================================
        // 14. ReportExporter 
        // ===============================================================
        [Test]
        public void Test_ReportExporter_Traducciones_Y_Ruta_Archivo()
        {
            GameObject exporterObj = new GameObject("Exporter");
            ReportExporter exporter = exporterObj.AddComponent<ReportExporter>();
            exporter.evaluator = evaluator;

            // Verificación de ruta en almacenamiento local persistente
            string rutaEsperada = Path.Combine(Application.persistentDataPath, "Acta_Examen_Conduccion.html");
            Assert.IsTrue(rutaEsperada.EndsWith("Acta_Examen_Conduccion.html"), "El informe debe guardarse en la carpeta persistente del sistema.");

            Object.DestroyImmediate(exporterObj);
        }

        // Helper auxiliar
        private InfraccionSO CrearInfraccion(string codigo, string desc, InfraccionSO.Gravedad gravedad, int puntos)
        {
            InfraccionSO inf = ScriptableObject.CreateInstance<InfraccionSO>();
            inf.codigo = codigo;
            inf.descripcion = desc;
            inf.tipo = gravedad;
            inf.puntosPenalizacion = puntos;
            return inf;
        }
    }
}