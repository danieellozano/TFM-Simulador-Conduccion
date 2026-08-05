using UnityEngine;
using Simulador.Core;
using System.Collections.Generic;

namespace Simulador.Evaluation
{
    public class DGTEvaluator : MonoBehaviour
    {
        public List<InfraccionSO> historialInfracciones = new List<InfraccionSO>();
        public bool evaluacionActiva = false;
        // Este método lo llama el GameEventListener
        public void RegistrarInfraccion(object data)
        {
            if (data is InfraccionSO infraccion)
            {
                historialInfracciones.Add(infraccion);
                Debug.Log($"<color=red>INFRACCIÓN:</color> {infraccion.descripcion}");

                // SOLO terminamos la sesión si la evaluación está activa (Modo Examen)
                if (evaluacionActiva && infraccion.tipo == InfraccionSO.Gravedad.Eliminatoria)
                {
                    // Aquí llamarías al HUD para mostrar "NO APTO" y pausar
                    Debug.Log("EXAMEN FINALIZADO: NO APTO");
                }
            }
        }
    }
}