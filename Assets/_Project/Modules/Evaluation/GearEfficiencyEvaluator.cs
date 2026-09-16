using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Supervisa continuamente la eficiencia mecánica de la conducción y el uso adecuado de la caja de cambios.
    // Evalúa si el conductor circula en una relación de marchas inapropiada para el régimen de revoluciones por minuto (RPM)
    // del motor según la normativa de la DGT (infracción leve CON-MAR), sancionando tanto el motor revolucionado
    // (consumo excesivo y desgaste) como el motor ahogado (sobrecarga mecánica), con un margen de tolerancia de 5 segundos
    // para permitir aceleraciones transitorias y una pausa de enfriamiento de 10 segundos tras registrar una falta.
    public class GearEfficiencyEvaluator : MonoBehaviour
    {
        [Header("Referencias SOA")]
        [Tooltip("Canal de datos de telemetría que expone el régimen de giro actual del motor en RPM.")]
        public FloatVariable engineRPM;

        [Tooltip("Canal de datos que contiene la marcha actualmente engranada (-1 Reversa, 0 Neutral, 1 a 5 Avance).")]
        public IntVariable currentGear;

        [Tooltip("Canal del bus de eventos reactivo para notificar la detección de la infracción.")]
        public GameEvent infractionEvent;

        [Tooltip("Activo de datos persistente que define la falta Leve por relación de marchas inadecuada (CON-MAR).")]
        public InfraccionSO gearInfraction;

        [Header("Configuración de Límites")]
        [Tooltip("Umbral superior de revoluciones (4000 RPM); superarlo de forma sostenida indica motor revolucionado.")]
        public float highRPMThreshold = 4000f;

        [Tooltip("Umbral inferior de revoluciones (1000 RPM); circular por debajo en marchas largas indica motor ahogado.")]
        public float lowRPMThreshold = 1000f;

        [Tooltip("Margen de cortesía temporal (5.0 segundos) antes de confirmar y penalizar la conducción ineficiente.")]
        public float maxTimeOutOfRange = 5.0f;

        // Cronómetro acumulativo para el exceso continuado de revoluciones
        private float highRPMTimer = 0f;

        // Cronómetro acumulativo para el defecto continuado de revoluciones
        private float lowRPMTimer = 0f;

        // Bucle de actualización en el que se auditan de forma pasiva las variables de transmisión y motor
        private void Update()
        {
            if (engineRPM == null || currentGear == null) return;

            // 1. EVALUACIÓN DE REVOLUCIONES ALTAS (No subir marcha)
            // Se audita en marchas de 1ª a 4ª. Se excluye la 5ª marcha porque es la relación más alta y no es posible subir más.
            if (currentGear.Value >= 1 && currentGear.Value < 5 && engineRPM.Value > highRPMThreshold)
            {
                highRPMTimer += Time.deltaTime;

                // Si el conductor mantiene el motor sobre-revolucionado por más de 5 segundos continuos
                if (highRPMTimer >= maxTimeOutOfRange)
                {
                    RegistrarFalta("Motor revolucionado. Suba de marcha.");

                    // Pausa de enfriamiento (cooldown) fijando el temporizador a -10 segundos
                    // para dar tiempo al alumno a corregir la marcha sin acumular sanciones consecutivas inmediatas
                    highRPMTimer = -10f;
                }
            }
            else
            {
                // Si las revoluciones vuelven al rango seguro o se cambia de marcha, se resetea el contador
                highRPMTimer = 0f;
            }

            // 2. EVALUACIÓN DE REVOLUCIONES BAJAS (No bajar marcha)
            // Se audita a partir de 2ª marcha inclusive. Se excluyen punto muerto y 1ª marcha por ser la relación mínima de arranque.
            if (currentGear.Value > 1 && engineRPM.Value < lowRPMThreshold)
            {
                lowRPMTimer += Time.deltaTime;

                // Si el conductor mantiene una marcha excesivamente larga ahogando el motor por más de 5 segundos continuos
                if (lowRPMTimer >= maxTimeOutOfRange)
                {
                    RegistrarFalta("Régimen demasiado bajo. Reduzca marcha.");

                    // Pausa de enfriamiento de 10 segundos
                    lowRPMTimer = -10f;
                }
            }
            else
            {
                // Si el motor recupera revoluciones suficientes o se reduce la marcha, se resetea el contador
                lowRPMTimer = 0f;
            }
        }

        // Emite el evento global de infracción leve por conducción ineficiente hacia el bus de eventos
        // y muestra el mensaje explicativo en la consola de depuración.
        // Parámetros:
        //   mensaje: Texto descriptivo que detalla la causa técnica de la falta (revoluciones excesivas o bajas).
        // Salida:
        //   Ninguna.
        private void RegistrarFalta(string mensaje)
        {
            if (infractionEvent != null && gearInfraction != null)
            {
                infractionEvent.Raise(gearInfraction);
                Debug.Log($"<color=yellow><b>[DGT EFICIENCIA]</b></color> {mensaje}");
            }
        }
    }
}