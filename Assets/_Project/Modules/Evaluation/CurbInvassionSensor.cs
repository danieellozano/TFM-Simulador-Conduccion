// using UnityEngine;
// using Simulador.Core;

// namespace Simulador.Evaluation
// {
//     public class CurbInvasionSensor : MonoBehaviour
//     {
//         public GameEvent infractionEvent;
//         public InfraccionSO curbInfraction; // MAN_BORDILLO
//         public float detectionDistance = 0.5f;

//         private void Update()
//         {
//             RaycastHit hit;
//             // Lanzamos un rayo grueso desde la rueda hacia abajo
//             if (Physics.SphereCast(transform.position, 0.2f, Vector3.down, out hit, detectionDistance))
//             {
//                 // Si lo que hay debajo de la rueda es un Bordillo...
//                 if (hit.collider.CompareTag("Bordillo") || hit.collider.CompareTag("Curb"))
//                 {
//                     infractionEvent.Raise(curbInfraction);
//                     Debug.Log("<color=red>DGT:</color> Rueda sobre bordillo detectada.");
//                 }
//             }
//         }
//     }
// }


using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Sensor físico acoplado a los conjuntos de rueda del utilitario para supervisar la cota de rodadura.
    // Utiliza un barrido esférico descendente (SphereCast) para detectar invasiones o pisadas sobre el bordillo
    // de la acera (MAN-BOR). El uso de un volumen esférico emula el ancho real de la banda de rodadura del neumático,
    // garantizando que no se omitan los perfiles verticales de los bordillos ante contactos oblicuos.
    public class CurbInvasionSensor : MonoBehaviour
    {
        [Header("Referencias SOA")]
        [Tooltip("Canal del bus de eventos globales invocado al detectar una falta (OnInfractionDetected).")]
        public GameEvent infractionEvent;

        [Tooltip("Activo de datos persistente que encapsula la infracción eliminatoria por subirse al bordillo (MAN-BOR).")]
        public InfraccionSO curbInfraction;

        [Header("Configuración de Detección")]
        [Tooltip("Distancia máxima del barrido descendente desde el centro de la rueda hacia la superficie de la calzada.")]
        public float detectionDistance = 0.5f;

        // Ciclo continuo de supervisión física de la banda de rodadura
        private void Update()
        {
            RaycastHit hit;

            // 1. BARRIDO ESFÉRICO DESCENDENTE (RADIO 0.2m)
            // Lanza una esfera hacia abajo simulando la superficie de contacto de la goma contra el suelo
            if (Physics.SphereCast(transform.position, 0.2f, Vector3.down, out hit, detectionDistance))
            {
                // 2. DISCRIMINACIÓN POR ETIQUETAS (TAGS)
                // Comprueba si el colisionador impactado corresponde a un bordillo (soporta nomenclatura en español e inglés)
                if (hit.collider.CompareTag("Bordillo") || hit.collider.CompareTag("Curb"))
                {
                    // Dispara la notificación asíncrona hacia el evaluador central de la DGT
                    if (infractionEvent != null && curbInfraction != null)
                    {
                        infractionEvent.Raise(curbInfraction);
                    }

                    Debug.Log("<color=red>DGT:</color> Rueda sobre bordillo detectada.");
                }
            }
        }
    }
}