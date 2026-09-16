using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Monitoriza de forma continua la velocidad del vehículo frente a la normativa legal vigente del tramo.
    // Aplica el margen oficial de tolerancia de 10 Km/h de la DGT, exige una persistencia temporal mínima
    // de 2 segundos para evitar penalizaciones por aceleraciones transitorias y clasifica de forma escalonada
    // la gravedad de la falta en Leve (VEL_GEN), Deficiente (VEL_DEF) o Eliminatoria (VEL_MAX).
    public class SpeedEvaluator : MonoBehaviour
    {
        [Header("Referencias de Datos")]
        [Tooltip("Canal de datos que registra la velocidad lineal instantánea del vehículo en Km/h.")]
        public FloatVariable currentSpeedSO;

        [Tooltip("Canal de datos que almacena la velocidad máxima permitida en el tramo actual en Km/h.")]
        public FloatVariable currentSpeedLimitSO;

        [Tooltip("Canal global del bus de eventos para emitir la notificación de infracción.")]
        public GameEvent infractionEvent;

        [Header("Catálogo de Infracciones DGT")]
        [Tooltip("Falta leve por rebasar la velocidad máxima entre 10 y 20 Km/h sobre el límite (VEL_GEN).")]
        public InfraccionSO speedInfractionLeve;

        [Tooltip("Falta deficiente por rebasar la velocidad máxima entre 20 y 30 Km/h sobre el límite (VEL_DEF).")]
        public InfraccionSO speedInfractionDeficiente;

        [Tooltip("Falta eliminatoria por rebasar la velocidad máxima en más de 30 Km/h sobre el límite (VEL_MAX).")]
        public InfraccionSO speedInfractionEliminatoria;

        // Temporizador acumulativo para medir la persistencia del exceso de velocidad
        private float timerExceso = 0f;

        // Ciclo de actualización que evalúa pasivamente la velocidad del coche frente al límite de la vía.
        private void Update()
        {
            if (currentSpeedSO == null || currentSpeedLimitSO == null) return;

            // 1. Cálculo de la diferencia neta de velocidad respecto a la ley vigente
            float exceso = currentSpeedSO.Value - currentSpeedLimitSO.Value;

            // 2. Filtro de margen de tolerancia de la DGT (los primeros 10 Km/h de exceso no se sancionan)
            if (exceso > 10f)
            {
                timerExceso += Time.deltaTime;

                // 3. Ventana de persistencia: el exceso debe mantenerse de forma continua durante más de 2 segundos
                if (timerExceso > 2f)
                {
                    if (infractionEvent != null)
                    {
                        // 4. Clasificación escalonada de la infracción según el baremo DGT
                        if (exceso > 30f)
                        {
                            // Exceso superior a 30 Km/h: Falta Eliminatoria inmediata
                            infractionEvent.Raise(speedInfractionEliminatoria);
                        }
                        else if (exceso > 20f)
                        {
                            // Exceso entre 20 y 30 Km/h: Falta Deficiente
                            infractionEvent.Raise(speedInfractionDeficiente);
                        }
                        else
                        {
                            // Exceso entre 10 y 20 Km/h: Falta Leve
                            infractionEvent.Raise(speedInfractionLeve);
                        }
                    }

                    // 5. Período de enfriamiento: se establece un valor negativo (-3s) para conceder
                    // un margen de 3 segundos antes de poder registrar una nueva multa si persiste la infracción
                    timerExceso = -3f;
                }
            }
            else
            {
                // Si el alumno reduce la marcha y vuelve a circular dentro de los márgenes legales,
                // el cronómetro se reinicia a cero inmediatamente
                timerExceso = 0f;
            }
        }
    }
}