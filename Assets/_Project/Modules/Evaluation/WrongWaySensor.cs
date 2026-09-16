using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Sensor direccional encargado de auditar la circulación en sentido contrario y el acceso a vías prohibidas.
    // Utiliza disparadores físicos invisibles (Triggers) posicionados estratégicamente en la red viaria.
    // Su principio de funcionamiento se basa en el cálculo del Producto Escalar (Dot Product) entre el vector
    // de avance frontal del vehículo y el vector de orientación espacial del sensor, determinando de forma
    // geométrica e instantánea si el alumno circula en sentido opuesto al legalmente establecido.
    // Permite cubrir tanto la entrada indebida en calles de sentido único (SIG-SENT) como la invasión
    // del carril contrario en calzadas de doble sentido mediante sensores fantasma (MAR-SEN).
    public class WrongWaySensor : MonoBehaviour
    {
        [Header("Referencias SOA")]
        [Tooltip("Canal del bus de eventos para notificar la falta hacia el evaluador central (OnInfractionDetected.asset).")]
        public GameEvent infractionEvent;

        [Tooltip("Activo normativo que encapsula la falta de gravedad eliminatoria por sentido prohibido (SIG_SENT o MAR_SEN).")]
        public InfraccionSO wrongWayInfraction;

        // Callback de entrada en el volumen físico del disparador.
        // Evalúa el alineamiento angular del vehículo del alumno respecto a la orientación fijada del sensor.
        // Parámetros:
        //   other: Colisionador de la entidad que intersecta el volumen del sensor.
        // Salida:
        //   No devuelve ningún valor (void).
        private void OnTriggerEnter(Collider other)
        {
            // Filtro de exclusividad: solo se evalúa si la entidad corresponde al vehículo del alumno
            if (other.CompareTag("Player"))
            {
                // CÁLCULO DE ALINEACIÓN MEDIANTE PRODUCTO ESCALAR:
                // El vector 'transform.forward' del sensor se orienta previamente hacia AFUERA de la dirección prohibida
                // (apuntando en el sentido legal de salida de la calle).
                // El producto escalar de dos vectores unitarios normalizados calcula el coseno del ángulo formado entre ellos:
                // Dot = cos(theta)
                float alignment = Vector3.Dot(other.transform.forward, transform.forward);

                // Si el producto escalar es estrictamente inferior a -0.5 (correspondiente a un ángulo > 120°):
                // Demuestra matemáticamente que ambos vectores están en oposición frontal directa.
                // Esto confirma que el conductor se desplaza entrando de frente en el sentido vetado.
                if (alignment < -0.5f) 
                {
                    // Emisión de la infracción eliminatoria al bus de eventos reactivo
                    if (infractionEvent != null)
                    {
                        infractionEvent.Raise(wrongWayInfraction);
                        Debug.Log("<color=red>DGT: Entrada en sentido prohibido detectada.</color>");
                    }
                }
            }
        }
    }
}