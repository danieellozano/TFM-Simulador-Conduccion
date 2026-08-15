using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Simulador.Core;
using Simulador.Evaluation;

namespace Simulador.HUD
{
    public class MissionUI : MonoBehaviour
    {
        [Header("Dashboard Permanente (Durante conducción)")]
        [Tooltip("El texto que dice '1. Deténgase en el STOP'")]
        public TextMeshProUGUI dashboardObjectiveText; 
        public StringVariable objectiveSO; 

        [Header("Paneles de Fin de Sesión")]
        public GameObject summaryPanel;      
        public GameObject reportPanelBase;   
        public GameObject detailsPanelTabla; 
        public GameObject pauseMenuPanel;

        [Header("Textos del Reporte Base")]
        public TextMeshProUGUI titleStatusText; 
        public TextMeshProUGUI resultStatusText; 

        [Header("Textos de la Tabla Detallada")]
        public TextMeshProUGUI reportHeader;
        public TextMeshProUGUI tableBodyText;
        public TextMeshProUGUI totalInfraccionesText;

        [Header("Referencias")]
        public DGTEvaluator evaluator;

        private void Start()
        {
            // Inicialización: Limpiamos el texto del dashboard para que no ponga "New Text"
            if (dashboardObjectiveText != null) dashboardObjectiveText.text = "";
            
            if(summaryPanel) summaryPanel.SetActive(false);
            if(reportPanelBase) reportPanelBase.SetActive(false);
            if(detailsPanelTabla) detailsPanelTabla.SetActive(false);
            if(pauseMenuPanel) pauseMenuPanel.SetActive(false);
        }

        private void Update()
        {
            // Sincronización constante: el texto de la pantalla siempre refleja el valor del archivo SO
            if (objectiveSO != null && dashboardObjectiveText != null)
            {
                dashboardObjectiveText.text = objectiveSO.Value;
            }
        }

        public void FinalizarSesion(object data = null)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Ocultamos el dashboard de objetivos al terminar
            if (dashboardObjectiveText != null) dashboardObjectiveText.gameObject.SetActive(false);

            if (evaluator.modoActual == ModoDeJuego.Maniobras)
            {
                summaryPanel.SetActive(true);
            }
            else
            {
                ConfigurarReporteBase();
            }
        }

        private void ConfigurarReporteBase()
        {
            if (summaryPanel != null) summaryPanel.SetActive(false);
            reportPanelBase.SetActive(true);
            detailsPanelTabla.SetActive(false);

            if (evaluator.modoActual == ModoDeJuego.PracticaUrbana)
            {
                titleStatusText.text = "PRÁCTICA FINALIZADA";
                resultStatusText.text = "Sesión de entrenamiento completada"; 
            }
            else 
            {
                titleStatusText.text = "EXAMEN FINALIZADO";
                string color = evaluator.EsApto() ? "green" : "red";
                string texto = evaluator.EsApto() ? "APTO" : "NO APTO";
                resultStatusText.text = $"RESULTADO: <color={color}>{texto}</color>";
            }
        }

        public void AbrirTablaDetallada()
        {
            if (evaluator == null || detailsPanelTabla == null || reportHeader == null) return;

            reportPanelBase.SetActive(false);
            detailsPanelTabla.SetActive(true);

            string resultadoTexto = "";
            if (evaluator.modoActual == ModoDeJuego.ExamenUrbano)
            {
                string color = evaluator.EsApto() ? "green" : "red";
                string calificacion = evaluator.EsApto() ? "APTO" : "NO APTO";
                resultadoTexto = $"   RESULTADO: <color={color}><b>{calificacion}</b></color>";
            }

            reportHeader.text = $"FECHA: {System.DateTime.Now:dd/MM/yyyy}   HORA: {System.DateTime.Now:HH:mm}{resultadoTexto}";
            tableBodyText.text = evaluator.ObtenerHistorialTabla();
            totalInfraccionesText.text = $"NÚMERO DE FALTAS: {evaluator.ContarInfraccionesTotales()}";
        }

        public void VolverAlReporteBase()
        {
            detailsPanelTabla.SetActive(false);
            reportPanelBase.SetActive(true);
        }

        public void IrAlMenuPrincipal()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("0_Menu_Principal");
        }
    }
}