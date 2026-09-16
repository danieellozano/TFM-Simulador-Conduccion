// using UnityEngine;
// using Simulador.Core;

// namespace Simulador.Evaluation
// {
//     public class CollisionEvaluator : MonoBehaviour
//     {
//         [Header("Referencias SOA")]
//         public GameEvent infractionEvent;     
//         public InfraccionSO curbInfraction;    // MAN_BORDILLO
//         public InfraccionSO objectInfraction;  // SIG_OBSTACULO
//         public InfraccionSO trafficCollisionRule; // COLISION_VEHICULO
//         public InfraccionSO estacionadoInfraction; // COLISION_VEHICULO_ESTACIONADO

//         [Header("Configuración Física")]
//         public float impulseThreshold = 100f; 

//         private void OnCollisionEnter(Collision collision)
//         {
//             // 1. Filtro físico de fuerza de impacto
//             float impactForce = collision.impulse.magnitude / Time.fixedDeltaTime;
//             if (impactForce < impulseThreshold) return;

//             // Bandera lógica para avisar a la zona de aparcamiento si corresponde
//             bool avisarParking = false;

//             // 2. Evaluamos la etiqueta del objeto utilizando el switch sobre .tag
//             switch (collision.gameObject.tag)
//             {
//                 case "Bordillo":
//                     RegistrarInfraccion(curbInfraction); // MAN_BORDILLO
//                     avisarParking = true;
//                     break;

//                 case "Curb": // Soporte por si usas el tag en inglés de las mallas
//                     RegistrarInfraccion(curbInfraction);
//                     avisarParking = true;
//                     break;

//                 case "Obstacle": // Para colisiones con farolas, edificios, etc.
//                     RegistrarInfraccion(objectInfraction); // SIG_OBSTACULO
//                     avisarParking = true; // Si chocas una farola aparcando, también invalida la maniobra
//                     break;

//                 case "CocheAparcado": // etiqueta para vehículos estacionados
//                     RegistrarInfraccion(estacionadoInfraction); // La nueva regla que has creado
//                     avisarParking = true; // Si golpeas un coche aparcado, también invalida el parking (EST-FE)
//                     break;

//                 case "Traffic": // Para colisiones contra la IA en movimiento
//                     RegistrarInfraccion(trafficCollisionRule); // COLISION_VEHICULO
//                     break;
//             }

//             // 3. Si la colisión requiere notificar al parking, ejecutamos la comprobación
//             if (avisarParking)
//             {
//                 ParkingZone activeZone = Object.FindFirstObjectByType<ParkingZone>();
//                 if (activeZone != null)
//                 {
//                     activeZone.RegistrarColisionEnZona();
//                 }
//             }
//         }

//         // Este es el método que te faltaba
//         private void RegistrarInfraccion(InfraccionSO info)
//         {
//             if (infractionEvent != null && info != null)
//             {
//                 infractionEvent.Raise(info);
//                 Debug.Log($"<color=red><b>[DGT COLISIÓN]</b></color> {info.descripcion}");
//             }
//         }
//     }
// }


using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Responsable de capturar, filtrar y clasificar los impactos físicos del chasis contra el entorno.
    // Utiliza el impulso vectorial de PhysX para calcular la fuerza de impacto resultante en Newtons,
    // filtrando vibraciones o contactos leves, y clasifica la gravedad de la falta eliminatoria según
    // la etiqueta del objeto impactado, notificando además a las zonas de maniobra activas.
    public class CollisionEvaluator : MonoBehaviour
    {
        [Header("Referencias SOA")]
        [Tooltip("Canal de eventos reactivo utilizado para emitir la notificación de colisión hacia el evaluador central.")]
        public GameEvent infractionEvent;

        [Tooltip("Falta eliminatoria asignada al impacto contra bordillos o aceras (MAN-BOR).")]
        public InfraccionSO curbInfraction;

        [Tooltip("Falta eliminatoria asignada a la colisión contra mobiliario urbano, señales o edificios (COL-OBJ).")]
        public InfraccionSO objectInfraction;

        [Tooltip("Falta eliminatoria asignada al impacto contra vehículos del tráfico autónomo en circulación (COL-VEH).")]
        public InfraccionSO trafficCollisionRule;

        [Tooltip("Falta eliminatoria asignada a la colisión contra vehículos detenidos o estacionados (COL-VE).")]
        public InfraccionSO estacionadoInfraction;

        [Header("Configuración Física")]
        [Tooltip("Umbral mínimo de fuerza de impacto en Newtons requerido para validar y procesar una colisión.")]
        public float impulseThreshold = 100f;

        // Callback del motor de físicas invocado en el primer contacto del chasis con otro colisionador
        private void OnCollisionEnter(Collision collision)
        {
            // 1. FILTRADO ANALÍTICO DE FUERZA DE IMPACTO
            // Se calcula la fuerza resultante dividiendo la magnitud del impulso entre el diferencial de tiempo fijo
            float impactForce = collision.impulse.magnitude / Time.fixedDeltaTime;
            if (impactForce < impulseThreshold) return;

            // Bandera lógica para avisar a la zona de aparcamiento si el choque invalida la maniobra
            bool avisarParking = false;

            // 2. DISCRIMINACIÓN NORMATIVA MEDIANTE ETIQUETAS (TAGS)
            switch (collision.gameObject.tag)
            {
                case "Bordillo":
                    RegistrarInfraccion(curbInfraction);
                    avisarParking = true;
                    break;

                case "Curb": // Soporte para mallas o paquetes de entorno con nomenclatura en inglés
                    RegistrarInfraccion(curbInfraction);
                    avisarParking = true;
                    break;

                case "Obstacle": // Farolas, contenedores, postes y mobiliario urbano
                    RegistrarInfraccion(objectInfraction);
                    avisarParking = true; // Un impacto contra mobiliario durante el aparcamiento invalida la maniobra
                    break;

                case "CocheAparcado": // Vehículos estáticos estacionados en batería o en línea
                    RegistrarInfraccion(estacionadoInfraction);
                    avisarParking = true; // Invalida el estacionamiento de forma inmediata (EST-FE)
                    break;

                case "Traffic": // Vehículos autónomos de la Inteligencia Artificial en movimiento
                    RegistrarInfraccion(trafficCollisionRule);
                    break;
            }

            // 3. PROPAGACIÓN DE ESTADO A LA ZONA DE ESTACIONAMIENTO ACTIVA
            if (avisarParking)
            {
                ParkingZone activeZone = Object.FindFirstObjectByType<ParkingZone>();
                if (activeZone != null)
                {
                    activeZone.RegistrarColisionEnZona();
                }
            }
        }

        // Emite el evento global de infracción hacia el bus reactivo del simulador
        // Parámetros:
        //   info: Activo ScriptableObject que encapsula la falta detectada y su nivel de gravedad oficial.
        private void RegistrarInfraccion(InfraccionSO info)
        {
            if (infractionEvent != null && info != null)
            {
                infractionEvent.Raise(info);
                Debug.Log($"<color=red><b>[DGT COLISIÓN]</b></color> {info.descripcion}");
            }
        }
    }
}