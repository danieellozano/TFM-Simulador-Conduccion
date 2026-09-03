using System.Collections.Generic;
using Simulador.Core;

namespace Simulador.Core
{
    public interface IEvaluacionProvider
    {
        ModoDeJuego modoActual { get; set; }
        List<InfraccionSO> historialInfracciones { get; }
        bool EsApto();
        int ContarInfraccionesTotales();
        string ObtenerResumenTexto();
        string ObtenerHistorialTabla();
    }
}