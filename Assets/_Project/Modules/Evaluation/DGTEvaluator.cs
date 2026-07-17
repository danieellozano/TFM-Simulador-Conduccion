using UnityEngine;
using Simulador.Core;
using System.Collections.Generic;

namespace Simulador.Evaluation
{
    public class DGTEvaluator : MonoBehaviour
    {
        public List<InfraccionSO> historialInfracciones = new List<InfraccionSO>();

        // Este método lo llama el GameEventListener
        public void RegistrarInfraccion(object data)
        {
            if (data is InfraccionSO infraccion)
            {
                historialInfracciones.Add(infraccion);

                // MENSAJE POR CONSOLA
                Debug.Log($"<color=red><b>[DGT INFRACCIÓN]</b></color> Ha cometido una falta: <b>{infraccion.descripcion}</b> (Gravedad: {infraccion.tipo})");
                
                if (infraccion.tipo == InfraccionSO.Gravedad.Eliminatoria)
                {
                    Debug.Log("<color=black><b>ESTADO: NO APTO.</b></color> Examen finalizado por falta eliminatoria.");
                }
            }
        }
    }
}