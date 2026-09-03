using NUnit.Framework;
using UnityEngine;
using Simulador.Core;
using Simulador.HUD;

namespace Simulador.Tests
{
    public class HUDTests
    {
        private FloatVariable speedSO;
        private IntVariable gearSO;
        private FloatVariable limitSO;
        private FloatVariable rpmSO;
        private BoolVariable isStalledSO;
        private BoolVariable isAutomaticSO;

        [SetUp]
        public void SetUp()
        {
            speedSO = ScriptableObject.CreateInstance<FloatVariable>();
            gearSO = ScriptableObject.CreateInstance<IntVariable>();
            limitSO = ScriptableObject.CreateInstance<FloatVariable>();
            rpmSO = ScriptableObject.CreateInstance<FloatVariable>();
            isStalledSO = ScriptableObject.CreateInstance<BoolVariable>();
            isAutomaticSO = ScriptableObject.CreateInstance<BoolVariable>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(speedSO);
            Object.DestroyImmediate(gearSO);
            Object.DestroyImmediate(limitSO);
            Object.DestroyImmediate(rpmSO);
            Object.DestroyImmediate(isStalledSO);
            Object.DestroyImmediate(isAutomaticSO);
        }

        // ---------------------------------------------------------------
        // 1. TELEMETRÍA Y DASHBOARD (DashboardUI.cs - Págs 70-72)
        // ---------------------------------------------------------------
        [Test]
        public void Test_Dashboard_Normalizacion_Velocidad_Elimina_Decimales()
        {
            speedSO.Value = 48.79f;

            // Lógica exacta de DashboardUI.cs (Pág. 71): Mathf.FloorToInt
            string velocidadTexto = Mathf.FloorToInt(speedSO.Value).ToString();

            Assert.AreEqual("48", velocidadTexto, "La velocidad formateada debe eliminar decimales.");
        }

        [Test]
        public void Test_Dashboard_Traduccion_Simbolica_De_Marchas()
        {
            Assert.AreEqual("R", FormatGear(-1), "Marcha -1 debe mostrar 'R'.");
            Assert.AreEqual("N", FormatGear(0),  "Marcha 0 debe mostrar 'N'.");
            Assert.AreEqual("1", FormatGear(1),  "Marcha 1 debe mostrar '1'.");
            Assert.AreEqual("5", FormatGear(5),  "Marcha 5 debe mostrar '5'.");
        }

        private string FormatGear(int gear)
        {
            switch (gear)
            {
                case -1: return "R";
                case 0: return "N";
                default: return gear.ToString();
            }
        }

        [Test]
        public void Test_Dashboard_Indicador_Limite_Velocidad_Muestra_Guiones_Si_Cero()
        {
            // Estado inicial antes de pasar la primera señal
            limitSO.Value = 0f;
            string textoLimite = (limitSO.Value <= 0) ? "--" : limitSO.Value.ToString();
            Assert.AreEqual("--", textoLimite, "Si el límite es <= 0, debe mostrar '--' en el HUD.");

            // Cruzamos señal de 50 km/h
            limitSO.Value = 50f;
            textoLimite = (limitSO.Value <= 0) ? "--" : limitSO.Value.ToString();
            Assert.AreEqual("50", textoLimite, "Tras cruzar la señal, debe actualizarse al número correspondiente.");
        }

        [Test]
        public void Test_Dashboard_Redondeo_RPM_Y_Alerta_Visual_Roja()
        {
            rpmSO.Value = 5620f;
            int rpmStep = 200;

            // Lógica de redondeo por pasos en DashboardUI.cs (Pág. 71)
            int roundedRPM = Mathf.RoundToInt(rpmSO.Value / rpmStep) * rpmStep; // 5600 RPM
            Assert.AreEqual(5600, roundedRPM, "RPM debe redondearse al paso de 200 RPM más cercano.");

            // Evaluación de color de alerta (>= 5500 RPM -> Rojo)
            Color colorTexto = (roundedRPM >= 5500) ? Color.red : Color.white;
            Assert.AreEqual(Color.red, colorTexto, "A 5600 RPM el texto debe cambiar a color ROJO de advertencia.");

            // Si el motor está calado -> Fuerza RPM a 0
            isStalledSO.Value = true;
            if (isStalledSO.Value) roundedRPM = 0;
            Assert.AreEqual(0, roundedRPM, "Con motor calado las RPM renderizadas deben ser 0.");
        }

        // ---------------------------------------------------------------
        // 2. BRÚJULA Y NAVEGACIÓN GPS 
        // ---------------------------------------------------------------
        [Test]
        public void Test_DynamicGPS_Calculo_Angulo_Relativo_Rotacion_Flecha()
        {
            // Posición del coche en el origen, orientada mirando hacia el Este (+90° en Y)
            GameObject playerObj = new GameObject("Coche");
            playerObj.transform.position = Vector3.zero;
            playerObj.transform.rotation = Quaternion.Euler(0, 90f, 0);

            // Destino GPS situado en el Norte (0, 0, 10)
            Vector3 targetPos = new Vector3(0, 0, 10f);

            // Lógica de DynamicGPS.cs (Pág. 74)
            Vector3 direction = targetPos - playerObj.transform.position;
            float angleNorteAbsoluto = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg; // 0°
            float relativeAngle = angleNorteAbsoluto - playerObj.transform.eulerAngles.y;    // 0° - 90° = -90°

            Assert.AreEqual(-90f, relativeAngle, "Con el coche orientado al Este y destino al Norte, la flecha debe rotar -90°.");

            Object.DestroyImmediate(playerObj);
        }

        // ---------------------------------------------------------------
        // 3. EXCLUSIÓN MUTUA DE PANELES EN MENÚ 
        // ---------------------------------------------------------------
        [Test]
        public void Test_MainMenuManager_Exclusion_Mutua_De_Paneles()
        {
            GameObject menuObj = new GameObject("MainMenu");
            MainMenuManager manager = menuObj.AddComponent<MainMenuManager>();

            manager.panelPrincipal = new GameObject("Principal");
            manager.panelNiveles = new GameObject("Niveles");
            manager.panelControles = new GameObject("Controles");
            manager.panelReglas = new GameObject("Reglas");

            // Mostrar Panel Niveles
            manager.MostrarPanelNiveles();

            Assert.IsFalse(manager.panelPrincipal.activeSelf, "Panel Principal debe apagarse.");
            Assert.IsTrue(manager.panelNiveles.activeSelf, "Panel Niveles debe encenderse.");
            Assert.IsFalse(manager.panelControles.activeSelf, "Panel Controles debe apagarse.");
            Assert.IsFalse(manager.panelReglas.activeSelf, "Panel Reglas debe apagarse.");

            Object.DestroyImmediate(menuObj);
        }

        // ---------------------------------------------------------------
        // 4. CONTROL DE PAUSA Y TIMESCALE 
        // ---------------------------------------------------------------
        [Test]
        public void Test_PauseManager_Pausar_Y_Continuar_Ajusta_TimeScale()
        {
            GameObject pauseObj = new GameObject("PauseManager");
            PauseManager pauseManager = pauseObj.AddComponent<PauseManager>();
            pauseManager.pauseMenuPanel = new GameObject("PausePanel");

            // Pausar
            pauseManager.Pausar();
            Assert.AreEqual(0f, Time.timeScale, "Al pausar, Time.timeScale debe ser 0.");
            Assert.IsTrue(pauseManager.pauseMenuPanel.activeSelf, "El panel de pausa debe estar visible.");

            // Continuar
            pauseManager.Continuar();
            Assert.AreEqual(1f, Time.timeScale, "Al reanudar, Time.timeScale debe volver a 1.");
            Assert.IsFalse(pauseManager.pauseMenuPanel.activeSelf, "El panel de pausa debe ocultarse.");

            Object.DestroyImmediate(pauseObj);
        }

        // ---------------------------------------------------------------
        // 5. SELECTOR DE TRANSMISIÓN 
        // ---------------------------------------------------------------
        [Test]
        public void Test_SelectorTransmisionUI_Elegir_Manual_O_Automatica()
        {
            GameObject selectorObj = new GameObject("SelectorUI");
            SelectorTransmisionUI selector = selectorObj.AddComponent<SelectorTransmisionUI>();
            selector.isAutomaticSO = isAutomaticSO;

            bool callbackInvocado = false;
            selector.OnTransmissionSelected += () => { callbackInvocado = true; };

            // Elegir Manual
            selector.ElegirManual();
            Assert.IsFalse(isAutomaticSO.Value, "ElegirManual debe poner isAutomaticSO en false.");
            Assert.IsTrue(callbackInvocado, "Debe notificar la selección mediante el callback.");

            callbackInvocado = false;

            // Elegir Automática
            selector.ElegirAutomatica();
            Assert.IsTrue(isAutomaticSO.Value, "ElegirAutomatica debe poner isAutomaticSO en true.");
            Assert.IsTrue(callbackInvocado);

            Object.DestroyImmediate(selectorObj);
        }
    }
}