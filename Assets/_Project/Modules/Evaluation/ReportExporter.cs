using UnityEngine;
using System.IO;
using System.Text;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class ReportExporter : MonoBehaviour
    {
        public DGTEvaluator evaluator;

        public void GenerarInformeHTML()
        {
            if (evaluator == null) return;

            string rutaArchivo = Path.Combine(Application.persistentDataPath, "Acta_Examen_Conduccion.html");
            StringBuilder html = new StringBuilder();

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
            html.Append("@media print { .no-print-zone { display: none; } }");
            html.Append("</style></head><body>");

            // Botón para PDF
            html.Append("<div class='no-print-zone'><button class='btn-pdf' onclick='window.print()'>GUARDAR / IMPRIMIR PDF</button></div>");

            // Cabecera
            html.Append("<div class='header'><div class='logo'>Simulador de Conducción - Formación Vial</div>");
            html.Append("Acta de Evaluación de Aptitudes y Comportamientos</div>");

            // Datos Sesión
            string escenarioDesc = FormatearEscenario(evaluator.modoActual.ToString());
            
            // --- CÁLCULO DE DURACIÓN EN MINUTOS Y SEGUNDOS ---
            int tiempoTotalSegundos = Mathf.FloorToInt(Time.timeSinceLevelLoad);
            int minutos = tiempoTotalSegundos / 60;
            int segundos = tiempoTotalSegundos % 60;
            string duracionFormateada = $"{minutos} min {segundos:00} s";

            html.Append("<div class='info-box'>");
            html.Append($"<div><b>FECHA:</b> {System.DateTime.Now:dd/MM/yyyy}<br><b>HORA:</b> {System.DateTime.Now:HH:mm}</div>");
            html.Append($"<div><b>ESCENARIO:</b> {escenarioDesc}<br><b>DURACIÓN:</b> {duracionFormateada}</div>");
            html.Append("</div>");

            // --- LÓGICA DE RESULTADO ACTUALIZADA ---
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

            // Tabla de Infracciones
            html.Append("<table><tr><th>Tipo de Falta</th><th>Código</th><th>Descripción de la Infracción</th></tr>");
            foreach (var inf in evaluator.historialInfracciones)
            {
                html.Append($"<tr><td>{TraducirFalta(inf.tipo)}</td><td>{inf.codigo}</td><td>{inf.descripcion}</td></tr>");
            }
            if (evaluator.historialInfracciones.Count == 0)
                html.Append("<tr><td colspan='3' style='text-align:center;'>Sin incidencias registradas.</td></tr>");
            html.Append("</table>");

            html.Append($"<div class='total'>TOTAL INFRACCIONES: {evaluator.historialInfracciones.Count}</div>");
            html.Append("</body></html>");

            File.WriteAllText(rutaArchivo, html.ToString());
            Application.OpenURL(rutaArchivo);
        }

        // --- AYUDANTES DE FORMATO PARA TFM ---

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

        private string FormatearEscenario(string enumString)
        {
            if (enumString == "PracticaUrbana") return "Práctica Urbana";
            if (enumString == "ExamenUrbano") return "Examen Urbano";
            return enumString;
        }
    }
}