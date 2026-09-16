using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Simulador.Core;

namespace Simulador.HUD
{
    // Controlador de la interfaz de usuario para el seguimiento de misiones y la gestión de reportes al finalizar la sesión.
    // Supervisa la instrucción actual mediante arquitecturas de datos desacopladas (SOA) y presenta paneles de balance final
    // (Apto / No Apto) adaptados según el modo de juego activo (Maniobras, Práctica Urbana o Examen Urbano).
    public class MissionUI : MonoBehaviour
    {
        [Header("Dashboard Permanente (Durante conducción)")]
        [Tooltip("Componente de texto UI donde se renderiza la instrucción u objetivo en tiempo real.")]
        public TextMeshProUGUI dashboardObjectiveText; 
        [Tooltip("Variable reactiva en ScriptableObject que contiene la descripción del objetivo activo.")]
        public StringVariable objectiveSO; 

        [Header("Paneles de Fin de Sesión")]
        [Tooltip("Panel resumen simplificado utilizado al finalizar la prueba de Maniobras.")]
        public GameObject summaryPanel;      
        [Tooltip("Panel base del informe final donde se despliega la calificación global.")]
        public GameObject reportPanelBase;   
        [Tooltip("Panel detallado que incluye la tabla completa con el historial de infracciones.")]
        public GameObject detailsPanelTabla; 
        [Tooltip("Panel del menú de pausa interactivo.")]
        public GameObject pauseMenuPanel;

        [Header("Textos del Reporte Base")]
        [Tooltip("Componente de texto para el título del estado de la sesión.")]
        public TextMeshProUGUI titleStatusText; 
        [Tooltip("Componente de texto para el veredicto final de evaluación (Apto / No Apto).")]
        public TextMeshProUGUI resultStatusText; 

        [Header("Textos de la Tabla Detallada")]
        [Tooltip("Encabezado del reporte detallado que registra fecha, hora y dictamen.")]
        public TextMeshProUGUI reportHeader;
        [Tooltip("Cuerpo del texto donde se formatea en formato de tabla el desglose de faltas.")]
        public TextMeshProUGUI tableBodyText;
        [Tooltip("Texto para el conteo acumulado de infracciones cometidas.")]
        public TextMeshProUGUI totalInfraccionesText;

        [Header("Evaluación DGT")]
        [Tooltip("Objeto que implementa la interfaz de evaluación para consultar calificaciones e historial.")]
        [SerializeField] private MonoBehaviour evaluatorObject;
        public IEvaluacionProvider evaluator => evaluatorObject as IEvaluacionProvider;

        // Inicializa el estado visual de los paneles al arrancar la escena, garantizando que el HUD comience limpio.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void Start()
        {
            if (dashboardObjectiveText != null) dashboardObjectiveText.text = "";
            
            if(summaryPanel != null) summaryPanel.SetActive(false);
            if(reportPanelBase != null) reportPanelBase.SetActive(false);
            if(detailsPanelTabla != null) detailsPanelTabla.SetActive(false);
            if(pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        }

        // Actualiza de manera continua la instrucción visual mostrada en el HUD según el valor del ScriptableObject.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void Update()
        {
            if (objectiveSO != null && dashboardObjectiveText != null)
            {
                dashboardObjectiveText.text = objectiveSO.Value;
            }
        }

        // Detiene la simulación física, libera el cursor y conmuta la interfaz hacia el resumen final de la prueba.
        // Parámetros:
        //   - data: Objeto con parámetros opcionales sobre el evento de cierre de sesión.
        // Salida: Ninguna.
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

        // Ajusta los textos e indicadores del panel base en función de si la sesión fue una práctica o examen formal.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
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

        // Abre el desglose pormenorizado del examen generando la tabla de faltas DGT con sello de tiempo.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
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

        // Alterna la visibilidad ocultando el detalle extendido para volver al panel base del informe.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        public void VolverAlReporteBase()
        {
            if (detailsPanelTabla != null) detailsPanelTabla.SetActive(false);
            if (reportPanelBase != null) reportPanelBase.SetActive(true);
        }

        // Reanuda la escala temporal y redirige la ejecución a la escena del menú principal.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        public void IrAlMenuPrincipal()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("0_Menu_Principal");
        }

        // Restablece la simulación y recarga la escena activa actual para repetir la prueba o práctica.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        public void Reintentar()
        {
            // Reanudamos el tiempo físico del juego antes de recargar para evitar que la nueva escena inicie congelada
            Time.timeScale = 1f; 
            
            // Recarga dinámicamente la escena activa (funciona tanto para Maniobras como para Urbano)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
        }
    }
}