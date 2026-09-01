using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class CollisionEvaluator : MonoBehaviour
    {
        [Header("Referencias SOA")]
        public GameEvent infractionEvent;     
        public InfraccionSO curbInfraction;    // MAN_BORDILLO
        public InfraccionSO objectInfraction;  // SIG_OBSTACULO
        public InfraccionSO trafficCollisionRule; // COLISION_VEHICULO
        public InfraccionSO estacionadoInfraction; // COLISION_VEHICULO_ESTACIONADO

        [Header("Configuración Física")]
        public float impulseThreshold = 100f; 

        private void OnCollisionEnter(Collision collision)
        {
            // 1. Filtro físico de fuerza de impacto
            float impactForce = collision.impulse.magnitude / Time.fixedDeltaTime;
            if (impactForce < impulseThreshold) return;

            // Bandera lógica para avisar a la zona de aparcamiento si corresponde
            bool avisarParking = false;

            // 2. Evaluamos la etiqueta del objeto utilizando el switch sobre .tag
            switch (collision.gameObject.tag)
            {
                case "Bordillo":
                    RegistrarInfraccion(curbInfraction); // MAN_BORDILLO
                    avisarParking = true;
                    break;

                case "Curb": // Soporte por si usas el tag en inglés de las mallas
                    RegistrarInfraccion(curbInfraction);
                    avisarParking = true;
                    break;

                case "Obstacle": // Para colisiones con farolas, edificios, etc.
                    RegistrarInfraccion(objectInfraction); // SIG_OBSTACULO
                    avisarParking = true; // Si chocas una farola aparcando, también invalida la maniobra
                    break;

                case "CocheAparcado": // etiqueta para vehículos estacionados
                    RegistrarInfraccion(estacionadoInfraction); // La nueva regla que has creado
                    avisarParking = true; // Si golpeas un coche aparcado, también invalida el parking (EST-FE)
                    break;

                case "Traffic": // Para colisiones contra la IA en movimiento
                    RegistrarInfraccion(trafficCollisionRule); // COLISION_VEHICULO
                    break;
            }

            // 3. Si la colisión requiere notificar al parking, ejecutamos la comprobación
            if (avisarParking)
            {
                ParkingZone activeZone = Object.FindFirstObjectByType<ParkingZone>();
                if (activeZone != null)
                {
                    activeZone.RegistrarColisionEnZona();
                }
            }
        }

        // Este es el método que te faltaba
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