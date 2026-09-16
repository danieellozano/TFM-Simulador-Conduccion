// using UnityEngine;
// using Simulador.Core;
// using System.Collections.Generic;

// namespace Simulador.Evaluation
// {
//     public class DGTEvaluator : MonoBehaviour, IEvaluacionProvider
//     {
//         [Header("Contadores de Faltas")]
//         public int faltasLeves = 0;
//         public int faltasDeficientes = 0;
//         public int faltasEliminatorias = 0;

//         public List<InfraccionSO> historialInfracciones { get; } = new List<InfraccionSO>();

//         public bool evaluacionActiva = true;

//         [Header("Estado de la Sesión")]
//         public ModoDeJuego modoActual { get; set; }

//         private Dictionary<InfraccionSO, float> cooldowns = new Dictionary<InfraccionSO, float>();
//         public float tiempoEsperaInfraccion = 1.5f;


//         public void RegistrarInfraccion(object data)
//         {
//             if (data is InfraccionSO infraccion)
//             {
//                 // ESCENARIO MANIOBRAS: No registramos infracciones en el circuito de maniobras
//                 if (modoActual == ModoDeJuego.Maniobras)
//                 {
//                     Debug.Log($"<color=white>DGT Evaluador:</color> Infracción {infraccion.codigo} omitida por estar en circuito de Maniobras.");
//                     return;
//                 }
            
//                 // --- LÓGICA DE COOLDOWN ---
//                 if (cooldowns.ContainsKey(infraccion))
//                 {
//                     // Si ha pasado menos tiempo del permitido, ignoramos la multa
//                     if (Time.time < cooldowns[infraccion] + tiempoEsperaInfraccion) return;
                    
//                     // Si ha pasado el tiempo, actualizamos la última vez
//                     cooldowns[infraccion] = Time.time;
//                 }
//                 else
//                 {
//                     // Si es la primera vez que comete esta infracción, la añadimos
//                     cooldowns.Add(infraccion, Time.time);
//                 }

//                 // --- REGISTRO NORMAL ---
//                 historialInfracciones.Add(infraccion);
//                 switch (infraccion.tipo)
//                 {
//                     case InfraccionSO.Gravedad.Leve: faltasLeves++; break;
//                     case InfraccionSO.Gravedad.Deficiente: faltasDeficientes++; break;
//                     case InfraccionSO.Gravedad.Eliminatoria: faltasEliminatorias++; break;
//                 }
//                 Debug.Log($"<color=red><b>[DGT]</b></color> {infraccion.descripcion}");
//             }
//         }

//         public bool EsApto()
//         {
//             // BAREMO OFICIAL DGT:
//             // - 1 Eliminatoria = NO APTO
//             // - 2 Deficientes = NO APTO
//             // - 10 Leves = NO APTO
//             // - 1 Deficiente + 5 Leves = NO APTO (Opcional, según quieras complicarlo)

//             if (faltasEliminatorias > 0) return false;
//             if (faltasDeficientes >= 2) return false;
//             if (faltasLeves >= 10) return false;

//             return true;
//         }

//         public void ResetEvaluacion(object data = null)
//         {
//             faltasLeves = 0;
//             faltasDeficientes = 0;
//             faltasEliminatorias = 0;
//             historialInfracciones.Clear();
//         }
        
//         public int ContarInfraccionesTotales()
//         {
//             return historialInfracciones.Count;
//         }

//         public string ObtenerResumenTexto()
//         {
//             string resumen = "";
//             foreach (var inf in historialInfracciones)
//             {
//                 resumen += $"- {inf.descripcion} ({inf.puntosPenalizacion} pts)\n";
//             }
//             return resumen == "" ? "Conducción perfecta: Sin infracciones." : resumen;
//         }

//         public string ObtenerHistorialTabla()
//         {
//             string tabla = "";
//             foreach (var inf in historialInfracciones)
//             {
//                 // <pos=20%> hace que la descripción empiece siempre en el mismo sitio
//                 tabla += $"<color=#FFD700>{inf.puntosPenalizacion} pts.</color> \t <pos=20%>{inf.descripcion}</pos>\n";
//             }
//             return (historialInfracciones.Count == 0) ? "Sin infracciones registradas." : tabla;
//         }
        
//     }
// }



using UnityEngine;
using Simulador.Core;
using System.Collections.Generic;

namespace Simulador.Evaluation
{
    // Componente central del sistema de evaluación que actúa como cerebro calificador y base de datos de sesión.
    // Implementa la interfaz IEvaluacionProvider para centralizar el registro cronológico de faltas,
    // aplicar filtros temporales de enfriamiento (cooldown) contra registros repetitivos en colisiones continuadas
    // y dictaminar el veredicto de aptitud del alumno (APTO / NO APTO) según el baremo oficial de examen de la DGT.
    public class DGTEvaluator : MonoBehaviour, IEvaluacionProvider
    {
        [Header("Contadores de Faltas")]
        // Contadores acumulativos independientes clasificados por nivel de gravedad reglamentario
        public int faltasLeves = 0;
        public int faltasDeficientes = 0;
        public int faltasEliminatorias = 0;

        // Historial dinámico en memoria que almacena las instancias de infracción cometidas durante la sesión
        public List<InfraccionSO> historialInfracciones { get; } = new List<InfraccionSO>();

        // Bandera de control general para activar o suspender la auditoría del evaluador
        public bool evaluacionActiva = true;

        [Header("Estado de la Sesión")]
        // Modalidad formativa activa (Maniobras, Práctica Urbana o Examen Urbano)
        public ModoDeJuego modoActual { get; set; }

        // Diccionario de marcas de tiempo utilizado para gestionar los periodos de enfriamiento por infracción
        private Dictionary<InfraccionSO, float> cooldowns = new Dictionary<InfraccionSO, float>();

        [Tooltip("Margen de tiempo mínimo (en segundos) requerido para volver a registrar la misma falta.")]
        public float tiempoEsperaInfraccion = 1.5f;

        // Procesa y registra una infracción recibida como carga útil a través del bus de eventos reactivo.
        // Parámetros:
        //   data: Objeto genérico propagado por el evento, verificado como instancia de InfraccionSO.
        public void RegistrarInfraccion(object data)
        {
            if (data is InfraccionSO infraccion)
            {
                // ESCENARIO MANIOBRAS: El circuito cerrado de maniobras opera como entorno libre de penalizaciones
                if (modoActual == ModoDeJuego.Maniobras)
                {
                    Debug.Log($"<color=white>DGT Evaluador:</color> Infracción {infraccion.codigo} omitida por estar en circuito de Maniobras.");
                    return;
                }
            
                // --- LÓGICA DE COOLDOWN TEMPORAL ---
                // Previene que contactos físicos prolongados (ej. rozar un bordillo o pisar una línea)
                // penalicen al alumno decenas de veces en fracciones de segundo
                if (cooldowns.ContainsKey(infraccion))
                {
                    // Si no ha transcurrido el tiempo mínimo de espera, se descarta la alerta
                    if (Time.time < cooldowns[infraccion] + tiempoEsperaInfraccion) return;
                    
                    // Si ha expirado el cooldown, se actualiza la marca de tiempo de la última infracción
                    cooldowns[infraccion] = Time.time;
                }
                else
                {
                    // Si es la primera vez que se comete esta infracción en la sesión, se añade al diccionario
                    cooldowns.Add(infraccion, Time.time);
                }

                // --- REGISTRO FORMAL Y CLASIFICACIÓN ---
                historialInfracciones.Add(infraccion);
                switch (infraccion.tipo)
                {
                    case InfraccionSO.Gravedad.Leve: 
                        faltasLeves++; 
                        break;
                    case InfraccionSO.Gravedad.Deficiente: 
                        faltasDeficientes++; 
                        break;
                    case InfraccionSO.Gravedad.Eliminatoria: 
                        faltasEliminatorias++; 
                        break;
                }

                Debug.Log($"<color=red><b>[DGT]</b></color> {infraccion.descripcion}");
            }
        }

        // Evalúa el veredicto del examen práctico contrastando los contadores acumulados frente al baremo oficial de la DGT.
        // Salida:
        //   True si el alumno supera la prueba (APTO); False si se infringe cualquiera de los límites excluyentes (NO APTO).
        public bool EsApto()
        {
            // BAREMO OFICIAL DE CALIFICACIÓN DGT (Instrucción 19/V-134):
            // - 1 falta Eliminatoria = NO APTO
            // - 2 faltas Deficientes = NO APTO
            // - 10 faltas Leves = NO APTO
            if (faltasEliminatorias > 0) return false;
            if (faltasDeficientes >= 2) return false;
            if (faltasLeves >= 10) return false;

            return true;
        }

        // Restablece los contadores de faltas y vacía el expediente de la sesión para garantizar un inicio determinista.
        // Parámetros:
        //   data: Carga útil opcional para permitir la suscripción directa del método al bus de eventos de reinicio.
        public void ResetEvaluacion(object data = null)
        {
            faltasLeves = 0;
            faltasDeficientes = 0;
            faltasEliminatorias = 0;
            historialInfracciones.Clear();
        }
        
        // Consulta el número total acumulado de faltas registradas en la prueba actual.
        // Salida:
        //   Cantidad entera total de infracciones almacenadas en el historial.
        public int ContarInfraccionesTotales()
        {
            return historialInfracciones.Count;
        }

        // Ensambla un resumen estructurado en formato de texto plano con las faltas y sus penalizaciones.
        // Salida:
        //   Cadena de texto con el desglose de faltas o mensaje de conducción perfecta si no hubo incidencias.
        public string ObtenerResumenTexto()
        {
            string resumen = "";
            foreach (var inf in historialInfracciones)
            {
                resumen += $"- {inf.descripcion} ({inf.puntosPenalizacion} pts)\n";
            }
            return resumen == "" ? "Conducción perfecta: Sin infracciones." : resumen;
        }

        // Genera la tabla textual formateada para su renderizado en los paneles de resultados mediante TextMeshPro.
        // Salida:
        //   Texto enriquecido con etiquetas de tabulación relativa (<pos=20%>) y código de color para las penalizaciones.
        public string ObtenerHistorialTabla()
        {
            string tabla = "";
            foreach (var inf in historialInfracciones)
            {
                // La etiqueta <pos=20%> alinea el inicio de la descripción de la falta en una columna uniforme
                tabla += $"<color=#FFD700>{inf.puntosPenalizacion} pts.</color> \t <pos=20%>{inf.descripcion}</pos>\n";
            }
            return (historialInfracciones.Count == 0) ? "Sin infracciones registradas." : tabla;
        }
    }
}