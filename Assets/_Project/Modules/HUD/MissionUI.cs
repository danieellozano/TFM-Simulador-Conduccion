using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Simulador.Core;

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

        [SerializeField] private MonoBehaviour evaluatorObject;
        public IEvaluacionProvider evaluator => evaluatorObject as IEvaluacionProvider;

        private void Start()
        {
            if (dashboardObjectiveText != null) dashboardObjectiveText.text = "";
            
            if(summaryPanel != null) summaryPanel.SetActive(false);
            if(reportPanelBase != null) reportPanelBase.SetActive(false);
            if(detailsPanelTabla != null) detailsPanelTabla.SetActive(false);
            if(pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        }

        private void Update()
        {
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

            if (dashboardObjectiveText != null) dashboardObjectiveText.gameObject.SetActive(false);

            // CORRECCIÓN DE SEGURIDAD: Añadido null-check para el evaluator
            if (evaluator != null && evaluator.modoActual == ModoDeJuego.Maniobras)
            {
                if (summaryPanel != null) summaryPanel.SetActive(true);
            }
            else
            {
                ConfigurarReporteBase();
                titleStatusText.text = "EXAMEN FINALIZADO";
            }
        }

        private void ConfigurarReporteBase()
        {
            if (summaryPanel != null) summaryPanel.SetActive(false);
            if (reportPanelBase != null) reportPanelBase.SetActive(true);
            if (detailsPanelTabla != null) detailsPanelTabla.SetActive(false);

            if (evaluator == null) return;

            if (evaluator.modoActual == ModoDeJuego.PracticaUrbana)
            {
                if (titleStatusText != null) titleStatusText.text = "PRÁCTICA FINALIZADA";
                if (resultStatusText != null) resultStatusText.text = "Sesión de entrenamiento completada"; 
            }
            else 
            {
                if (titleStatusText != null) titleStatusText.text = "EXAMEN FINALIZADO";
                string color = evaluator.EsApto() ? "green" : "red";
                string texto = evaluator.EsApto() ? "APTO" : "NO APTO";
                if (resultStatusText != null) resultStatusText.text = $"RESULTADO: <color={color}>{texto}</color>";
            }
        }

        public void AbrirTablaDetallada()
        {
            if (evaluator == null || detailsPanelTabla == null || reportHeader == null) return;

            if (reportPanelBase != null) reportPanelBase.SetActive(false);
            detailsPanelTabla.SetActive(true);

            string resultadoTexto = "";
            if (evaluator.modoActual == ModoDeJuego.ExamenUrbano)
            {
                string color = evaluator.EsApto() ? "green" : "red";
                string calificacion = evaluator.EsApto() ? "APTO" : "NO APTO";
                resultadoTexto = $"   RESULTADO: <color={color}><b>{calificacion}</b></color>";
            }

            reportHeader.text = $"FECHA: {System.DateTime.Now:dd/MM/yyyy}   HORA: {System.DateTime.Now:HH:mm}{resultadoTexto}";
            if (tableBodyText != null) tableBodyText.text = evaluator.ObtenerHistorialTabla();
            if (totalInfraccionesText != null) totalInfraccionesText.text = $"NÚMERO DE FALTAS: {evaluator.ContarInfraccionesTotales()}";
        }

        public void VolverAlReporteBase()
        {
            if (detailsPanelTabla != null) detailsPanelTabla.SetActive(false);
            if (reportPanelBase != null) reportPanelBase.SetActive(true);
        }

        public void IrAlMenuPrincipal()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("0_Menu_Principal");
        }

        public void Reintentar()
        {
            // Reanudamos el tiempo físico del juego antes de recargar para evitar que la nueva escena inicie congelada
            Time.timeScale = 1f; 
            
            // Recarga dinámicamente la escena activa (funciona tanto para Maniobras como para Urbano)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
        }
    }
}