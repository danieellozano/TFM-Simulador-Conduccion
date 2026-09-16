using UnityEngine;
using System.IO;
using System.Text;
using Simulador.Core;

namespace Simulador.Evaluation
{
    // Responsable de transformar los datos volátiles recopilados durante la sesión de conducción
    // en un documento técnico persistente en formato HTML estandarizado.
    // Ensambla la cabecera institucional, marcas de tiempo, duración, calificación oficial DGT (APTO / NO APTO)
    // y la tabla cronológica de faltas cometidas, habilitando su impresión o guardado directo en PDF.
    public class ReportExporter : MonoBehaviour
    {
        [Header("Referencias del Sistema")]
        [Tooltip("Instancia central del evaluador de la que se extrae el historial de faltas, el modo y el veredicto final.")]
        public DGTEvaluator evaluator;

        // Compila toda la información de la prueba, genera el archivo HTML en el almacenamiento persistente
        // del sistema operativo y lo abre automáticamente en el navegador web predeterminado.
        // Parámetros:
        //   Ninguno.
        // Salida:
        //   No devuelve ningún valor (void).
        public void GenerarInformeHTML()
        {
            if (evaluator == null) return;

            // Ruta de almacenamiento local persistente segura e independiente de la plataforma
            string rutaArchivo = Path.Combine(Application.persistentDataPath, "Acta_Examen_Conduccion.html");
            StringBuilder html = new StringBuilder();

            // --- ESTRUCTURA Y HOJAS DE ESTILO CSS DEL DOCUMENTO ---
            html.Append("<!DOCTYPE html><html><head><meta charset='UTF-8'>");
            html.Append("<title>Informe de Evaluación DGT</title>");
            html.Append("<style>");
            html.Append("body { font-family: 'Segoe UI', sans-serif; margin: 40px; color: #333; line-height: 1.6; }");
            html.Append(".header { text-align: center; border-bottom: 3px solid #d32f2f; padding-bottom: 20px; margin-bottom: 30px; }");
            html.Append(".logo { font-size: 28px; font-weight: bold; color: #d32f2f; text-transform: uppercase; }");
            html.Append(".info-box { display: flex; justify-content: space-between; background: #f9f9f9; padding: 20px; border: 1px solid #eee; border-radius: 8px; margin-bottom: 30px; }");
            html.Append(".result-banner { text-align: center; font-size: 24px; font-weight: bold; padding: 15px; border-radius: 5px; margin-bottom: 30px; border: 2px solid; }");
            html.Append(".apto { background-color: #e8f5e9; color: #2e7d32; border-color: #2e7d32; }");
            html.Append(".no-apto { background-color: #fce4e4; color: #d32f2f; border-color: #d32f2f; }");
            html.Append("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            html.Append("th { background-color: #444; color: white; padding: 12px; text-align: left; }");
            html.Append("td { border: 1px solid #ddd; padding: 12px; }");
            html.Append("tr:nth-child(even) { background-color: #f2f2f2; }");
            html.Append(".total { text-align: right; font-size: 18px; font-weight: bold; margin-top: 20px; }");
            html.Append(".no-print-zone { text-align: center; margin-bottom: 20px; }");
            html.Append(".btn-pdf { background: #d32f2f; color: white; padding: 12px 25px; border: none; border-radius: 5px; cursor: pointer; font-size: 16px; font-weight: bold; }");
            
            // Regla de medios para ocultar elementos de navegación al imprimir o guardar en PDF
            html.Append("@media print { .no-print-zone { display: none; } }");
            html.Append("</style></head><body>");

            // Botón interactivo para invocar el cuadro de impresión nativo del navegador
            html.Append("<div class='no-print-zone'><button class='btn-pdf' onclick='window.print()'>GUARDAR / IMPRIMIR PDF</button></div>");

            // Cabecera institucional
            html.Append("<div class='header'><div class='logo'>Simulador de Conducción - Formación Vial</div>");
            html.Append("Acta de Evaluación de Aptitudes y Comportamientos</div>");

            // --- METADATOS Y TIEMPOS DE LA SESIÓN ---
            string escenarioDesc = FormatearEscenario(evaluator.modoActual.ToString());
            
            // Conversión del tiempo transcurrido desde la carga del nivel a minutos y segundos formateados
            int tiempoTotalSegundos = Mathf.FloorToInt(Time.timeSinceLevelLoad);
            int minutos = tiempoTotalSegundos / 60;
            int segundos = tiempoTotalSegundos % 60;
            string duracionFormateada = $"{minutos} min {segundos:00} s";

            html.Append("<div class='info-box'>");
            html.Append($"<div><b>FECHA:</b> {System.DateTime.Now:dd/MM/yyyy}<br><b>HORA:</b> {System.DateTime.Now:HH:mm}</div>");
            html.Append($"<div><b>ESCENARIO:</b> {escenarioDesc}<br><b>DURACIÓN:</b> {duracionFormateada}</div>");
            html.Append("</div>");

            // --- BANNER DE CALIFICACIÓN OFICIAL ---
            // Si es circuito cerrado de maniobras se muestra resultado formativo neutral;
            // si es examen urbano, se aplica el veredicto estricto de aptitud DGT
            if (evaluator.modoActual == ModoDeJuego.Maniobras)
            {
                html.Append("<div class='result-banner' style='color:#666; border-color:#ccc;'>PRÁCTICA DE MANIOBRAS FINALIZADA</div>");
            }
            else
            {
                string clase = evaluator.EsApto() ? "apto" : "no-apto";
                string resultado = evaluator.EsApto() ? "APTO" : "NO APTO";
                html.Append($"<div class='result-banner {clase}'>CALIFICACIÓN FINAL: {resultado}</div>");
            }

            // --- TABLA DETALLADA DE INFRACCIONES REGISTRADAS ---
            html.Append("<table><tr><th>Tipo de Falta</th><th>Código</th><th>Descripción de la Infracción</th></tr>");
            
            foreach (var inf in evaluator.historialInfracciones)
            {
                html.Append($"<tr><td>{TraducirFalta(inf.tipo)}</td><td>{inf.codigo}</td><td>{inf.descripcion}</td></tr>");
            }
            
            // Mensaje informativo en caso de prueba limpia sin penalizaciones
            if (evaluator.historialInfracciones.Count == 0)
            {
                html.Append("<tr><td colspan='3' style='text-align:center;'>Sin incidencias registradas.</td></tr>");
            }
            html.Append("</table>");

            // Resumen numérico total
            html.Append($"<div class='total'>TOTAL INFRACCIONES: {evaluator.historialInfracciones.Count}</div>");
            html.Append("</body></html>");

            // --- PERSISTENCIA EN DISCO Y APERTURA EN EL NAVEGADOR ---
            File.WriteAllText(rutaArchivo, html.ToString());
            Application.OpenURL(rutaArchivo);
        }

        // Traduce el enumerado de gravedad de la infracción a su representación textual en español.
        // Parámetros:
        //   tipo: Nivel de gravedad oficial según el catálogo DGT (Leve, Deficiente o Eliminatoria).
        // Salida:
        //   Cadena de texto legible con el nombre de la falta.
        private string TraducirFalta(InfraccionSO.Gravedad tipo)
        {
            switch (tipo)
            {
                case InfraccionSO.Gravedad.Leve: return "Leve";
                case InfraccionSO.Gravedad.Deficiente: return "Deficiente";
                case InfraccionSO.Gravedad.Eliminatoria: return "Eliminatoria";
                default: return tipo.ToString();
            }
        }

        // Convierte el identificador del enumerado del modo de juego a una nomenclatura formal con espacios y tildes.
        // Parámetros:
        //   enumString: Nombre del modo de juego extraído del enumerado.
        // Salida:
        //   Cadena formateada apta para la cabecera del acta de examen.
        private string FormatearEscenario(string enumString)
        {
            if (enumString == "PracticaUrbana") return "Práctica Urbana";
            if (enumString == "ExamenUrbano") return "Examen Urbano";
            return enumString;
        }
    }
}