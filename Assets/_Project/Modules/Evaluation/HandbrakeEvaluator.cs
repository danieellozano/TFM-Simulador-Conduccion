using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Supervisa y audita el uso del freno de estacionamiento (freno de mano) según los criterios de la DGT.
    // Implementa un sistema de evaluación escalonado en tres niveles de gravedad reglamentarios:
    // 1. Falta Eliminatoria (CON-MAN-E): Activación deliberada en marcha a alta velocidad (> 20 km/h) con riesgo de pérdida de control.
    // 2. Falta Deficiente (CON-MAN-D): Circulación continuada o intento prolongado de avance (> 5 segundos) con el freno activado.
    // 3. Falta Leve (CON-MAN-L): Intento de inicio de marcha con el freno accionado, rectificando el error antes de transcurrir 5 segundos.
    public class HandbrakeEvaluator : MonoBehaviour
    {
        [Header("Referencias SOA")]
        [Tooltip("Contrato de datos de entrada para consultar el estado del freno de mano y la demanda del acelerador.")]
        public InputDataSO inputData;

        [Tooltip("Canal de telemetría que expone la velocidad instantánea del utilitario en Km/h.")]
        public FloatVariable currentSpeed;

        [Tooltip("Canal del bus de eventos globales para notificar el registro de faltas.")]
        public GameEvent infractionEvent;

        [Header("Reglas DGT")]
        [Tooltip("Falta Leve: inicio de marcha con freno de mano puesto pero rectificado a tiempo (CON-MAN-L).")]
        public InfraccionSO ruleLeve;

        [Tooltip("Falta Deficiente: persistencia en el intento de marcha con freno de mano más de 5s (CON-MAN-D).")]
        public InfraccionSO ruleDeficiente;

        [Tooltip("Falta Eliminatoria: accionamiento brusco del freno de mano en marcha a velocidad > 20 km/h (CON-MAN-E).")]
        public InfraccionSO ruleEliminatoria;

        [Header("Configuración")]
        [Tooltip("Tiempo límite de tolerancia acelerando con freno de mano antes de escalar la falta a Deficiente.")]
        public float timeForDeficiente = 5.0f;

        // Cronómetro que acumula el tiempo continuo demandando aceleración mientras el freno permanece puesto
        private float handbrakeTimer = 0f;

        // Bandera que asegura que la falta Deficiente se registre una sola vez por cada maniobra incorrecta
        private bool deficienteReported = false;

        // Estado del freno de mano en el fotograma anterior para detectar transiciones (flancos de subida y bajada)
        private bool wasHandbrakeActive = false;

        // Bucle de actualización en el que se analizan las transiciones de estado del freno y la velocidad
        private void Update()
        {
            if (inputData == null || currentSpeed == null) return;

            // --- CASO 1: FALTA ELIMINATORIA (Conducción negligente o temeraria) ---
            // Detección por flanco de subida: si el freno se activa en este frame exacto y el coche circula a más de 20 Km/h
            if (inputData.Handbrake && !wasHandbrakeActive && currentSpeed.Value > 20f)
            {
                if (infractionEvent != null && ruleEliminatoria != null)
                {
                    infractionEvent.Raise(ruleEliminatoria);
                }

                Debug.Log("<color=red>DGT ELIMINATORIA:</color> Freno de mano accionado en marcha a alta velocidad.");
            }

            // --- CASO 2: FALTA DEFICIENTE O LEVE (Inicio de marcha con freno accionado) ---
            // El conductor tiene el freno de mano puesto y pisa el acelerador con una demanda significativa (> 20%)
            if (inputData.Handbrake && inputData.Throttle > 0.2f)
            {
                handbrakeTimer += Time.deltaTime;

                // Si mantiene la demanda de avance con el freno de mano puesto durante 5.0 segundos o más
                if (handbrakeTimer >= timeForDeficiente && !deficienteReported)
                {
                    if (infractionEvent != null && ruleDeficiente != null)
                    {
                        infractionEvent.Raise(ruleDeficiente);
                    }

                    deficienteReported = true; 
                    Debug.Log("<color=orange>DGT DEFICIENTE:</color> Circulación prolongada con freno de mano accionado.");
                }
            }
            else
            {
                // Detección por flanco de bajada: el alumno soltó el freno tras haber intentado avanzar
                // Si aceleró durante más de 0.5s (intento real) pero menos de 5.0s, se sanciona únicamente como Leve
                if (wasHandbrakeActive && !inputData.Handbrake && handbrakeTimer > 0.5f && !deficienteReported)
                {
                    if (infractionEvent != null && ruleLeve != null)
                    {
                        infractionEvent.Raise(ruleLeve);
                    }

                    Debug.Log("<color=yellow>DGT LEVE:</color> Inicio de marcha con freno de mano (rectificado a tiempo).");
                }

                // Restablece el cronómetro y el estado al soltar el acelerador o desactivar el freno
                handbrakeTimer = 0f;
                deficienteReported = false;
            }

            // Almacena el estado actual para la comparación del siguiente fotograma
            wasHandbrakeActive = inputData.Handbrake;
        }
    }
}