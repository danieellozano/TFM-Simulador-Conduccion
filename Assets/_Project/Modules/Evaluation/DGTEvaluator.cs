using UnityEngine;
using Simulador.Core;
using System.Collections.Generic;

namespace Simulador.Evaluation
{
    public class DGTEvaluator : MonoBehaviour
    {
        [Header("Contadores de Faltas")]
        public int faltasLeves = 0;
        public int faltasDeficientes = 0;
        public int faltasEliminatorias = 0;

        public List<InfraccionSO> historialInfracciones = new List<InfraccionSO>();

        public void RegistrarInfraccion(object data)
        {
            if (data is InfraccionSO infraccion)
            {
                historialInfracciones.Add(infraccion);

                // Clasificar según el tipo definido en el ScriptableObject
                switch (infraccion.tipo)
                {
                    case InfraccionSO.Gravedad.Leve: faltasLeves++; break;
                    case InfraccionSO.Gravedad.Deficiente: faltasDeficientes++; break;
                    case InfraccionSO.Gravedad.Eliminatoria: faltasEliminatorias++; break;
                }

                Debug.Log($"<color=red><b>[DGT]</b></color> {infraccion.descripcion} ({infraccion.tipo})");
            }
        }

        public bool EsApto()
        {
            // BAREMO OFICIAL DGT:
            // - 1 Eliminatoria = NO APTO
            // - 2 Deficientes = NO APTO
            // - 10 Leves = NO APTO
            // - 1 Deficiente + 5 Leves = NO APTO (Opcional, según quieras complicarlo)

            if (faltasEliminatorias > 0) return false;
            if (faltasDeficientes >= 2) return false;
            if (faltasLeves >= 10) return false;

            return true;
        }

        public void ResetEvaluacion(object data = null)
        {
            faltasLeves = 0;
            faltasDeficientes = 0;
            faltasEliminatorias = 0;
            historialInfracciones.Clear();
        }
    }
}