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
        public bool evaluacionActiva = true;

        [Header("Estado de la Sesión")]
        public ModoDeJuego modoActual;

        private Dictionary<InfraccionSO, float> cooldowns = new Dictionary<InfraccionSO, float>();
        public float tiempoEsperaInfraccion = 1.5f;

        public void RegistrarInfraccion(object data)
        {
            if (data is InfraccionSO infraccion)
            {
                // ESCENARIO MANIOBRAS: No registramos infracciones en el circuito de maniobras
                if (modoActual == ModoDeJuego.Maniobras)
                {
                    Debug.Log($"<color=white>DGT Evaluador:</color> Infracción {infraccion.codigo} omitida por estar en circuito de Maniobras.");
                    return;
                }
            
                // --- LÓGICA DE COOLDOWN ---
                if (cooldowns.ContainsKey(infraccion))
                {
                    // Si ha pasado menos tiempo del permitido, ignoramos la multa
                    if (Time.time < cooldowns[infraccion] + tiempoEsperaInfraccion) return;
                    
                    // Si ha pasado el tiempo, actualizamos la última vez
                    cooldowns[infraccion] = Time.time;
                }
                else
                {
                    // Si es la primera vez que comete esta infracción, la añadimos
                    cooldowns.Add(infraccion, Time.time);
                }

                // --- REGISTRO NORMAL ---
                historialInfracciones.Add(infraccion);
                switch (infraccion.tipo)
                {
                    case InfraccionSO.Gravedad.Leve: faltasLeves++; break;
                    case InfraccionSO.Gravedad.Deficiente: faltasDeficientes++; break;
                    case InfraccionSO.Gravedad.Eliminatoria: faltasEliminatorias++; break;
                }
                Debug.Log($"<color=red><b>[DGT]</b></color> {infraccion.descripcion}");
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
        
        public int ContarInfraccionesTotales()
        {
            return historialInfracciones.Count;
        }

        public string ObtenerResumenTexto()
        {
            string resumen = "";
            foreach (var inf in historialInfracciones)
            {
                resumen += $"- {inf.descripcion} ({inf.puntosPenalizacion} pts)\n";
            }
            return resumen == "" ? "Conducción perfecta: Sin infracciones." : resumen;
        }

        public string ObtenerHistorialTabla()
        {
            string tabla = "";
            foreach (var inf in historialInfracciones)
            {
                // <pos=20%> hace que la descripción empiece siempre en el mismo sitio
                tabla += $"<color=#FFD700>{inf.puntosPenalizacion} pts.</color> \t <pos=20%>{inf.descripcion}</pos>\n";
            }
            return (historialInfracciones.Count == 0) ? "Sin infracciones registradas." : tabla;
        }
    }
}