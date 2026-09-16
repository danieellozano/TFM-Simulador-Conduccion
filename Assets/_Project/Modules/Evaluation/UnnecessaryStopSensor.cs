using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Sensor encargado de auditar y sancionar las detenciones innecesarias o injustificadas en la calzada.
    // Supervisa de forma continua la velocidad del utilitario del alumno cuando este se encuentra detenido
    // fuera de áreas reglamentariamente autorizadas (tales como líneas de detención de STOP, semáforos o zonas de estacionamiento).
    // Implementa un contador entero para resolver la superposición tridimensional de zonas de parada seguras y
    // exime automáticamente de sanción al conductor si la detención está forzada por un fallo mecánico o calado del motor.
    public class UnnecessaryStopSensor : MonoBehaviour
    {
        [Header("Referencias SOA")]
        [Tooltip("Canal de datos que registra la velocidad lineal instantánea del vehículo en tiempo real.")]
        public FloatVariable vehicleSpeed;

        [Tooltip("Bandera booleana compartida que indica si el motor del vehículo se encuentra calado.")]
        public BoolVariable isStalledSO;

        [Tooltip("Canal del bus de eventos para emitir la infracción hacia el evaluador central.")]
        public GameEvent infractionEvent;

        [Tooltip("Activo normativo que define la falta leve por detención injustificada en mitad de la vía (CON-PAR).")]
        public InfraccionSO stopInfraction;

        [Header("Configuración")]
        [Tooltip("Margen de tolerancia temporal (en segundos) que el alumno puede permanecer detenido antes de ser sancionado.")]
        public float timeAllowedToStop = 3.0f;
        
        // Cronómetro interno que acumula el tiempo continuo que el vehículo permanece inmovilizado fuera de una zona válida
        private float stopTimer = 0f;

        // Bandera de control para evitar la emisión masiva o reiterada de multas durante una misma detención prolongada
        private bool infractionReported = false;

        // Contador de zonas seguras activas. Sustituye a una bandera booleana simple para gestionar de forma robusta
        // la superposición física de múltiples volúmenes de detención legal (ej. un STOP adyacente a una plaza de parking)
        [SerializeField] private int zonesCount = 0;

        // Bucle de actualización en el que se auditan las tres condiciones concurrentes:
        // justificante mecánico por calado, velocidad cinemática de reposo y ausencia de zonas de parada permitidas.
        // Parámetros:
        //   Ninguno.
        // Salida:
        //   No devuelve ningún valor (void).
        private void Update()
        {
            // 1. FILTRO DE JUSTIFICACIÓN MECÁNICA:
            // Si el motor se encuentra calado accidentalmente, la detención se considera forzada por un fallo técnico.
            // Se resetea el temporizador de infracción para no penalizar doblemente al alumno.
            if (isStalledSO != null && isStalledSO.Value == true) 
            {
                stopTimer = 0f;
                return;
            }

            // 2. AUDITORÍA NORMATIVA DE DETENCIÓN:
            // Se valida si la velocidad es inferior al umbral de reposo absoluto (< 0.1 km/h) y el vehículo
            // NO se encuentra intersectando ningún volumen protegido (zonesCount <= 0)
            if (vehicleSpeed.Value < 0.1f && zonesCount <= 0)
            {
                stopTimer += Time.deltaTime;

                // Si la detención injustificada persiste por encima de la tolerancia máxima configurada (3 segundos)
                if (stopTimer >= timeAllowedToStop && !infractionReported)
                {
                    // Dispara la infracción leve por detención injustificada en la calzada (CON-PAR)
                    infractionEvent.Raise(stopInfraction);
                    infractionReported = true;
                    Debug.Log("<color=red><b>[DGT]</b></color> Parada innecesaria fuera de zona legal.");
                }
            }
            else
            {
                // Si el utilitario reanuda la marcha o ingresa en una zona de detención autorizada,
                // se restablecen instantáneamente el cronómetro y la bandera de reporte
                stopTimer = 0f;
                infractionReported = false;
            }
        }

        // Detecta el ingreso del vehículo en volúmenes de detención autorizada (disparadores físicos de la escena).
        // Incrementa el contador de zonas para asegurar inmunidad normativa frente a sanciones de parada indebida.
        // Parámetros:
        //   other: Colisionador del volumen espacial en el que ingresa el vehículo.
        // Salida:
        //   No devuelve ningún valor (void).
        private void OnTriggerEnter(Collider other)
        {
            // ORDEN DE PRIORIDAD DE ZONAS PERMITIDAS:
            
            // Prioridad Máxima: Zonas de maniobra de aparcamiento o área de finalización de examen
            if (other.CompareTag("Parking Meta") || other.CompareTag("Parking"))
            {
                zonesCount++;
                return;
            }

            // Prioridad Media: Zonas reguladas de tráfico (intersecciones con STOP, semáforos o cedas el paso)
            // Se identifican mediante la etiqueta ValidStopZone, asignada dinámicamente según la fase de los elementos
            if (other.CompareTag("ValidStopZone"))
            {
                zonesCount++;
                return;
            }
        }

        // Detecta la salida del vehículo de un volumen de parada autorizada.
        // Decrementa de forma segura el contador de zonas, asegurando matemáticamente que nunca adopte valores negativos.
        // Parámetros:
        //   other: Colisionador del volumen espacial que abandona el vehículo.
        // Salida:
        //   No devuelve ningún valor (void).
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Parking Meta") || other.CompareTag("Parking") || other.CompareTag("ValidStopZone"))
            {
                // Decrementa el contador acotándolo a un valor mínimo de cero para evitar desincronizaciones espaciales
                zonesCount = Mathf.Max(0, zonesCount - 1);
            }
        }
    }
}