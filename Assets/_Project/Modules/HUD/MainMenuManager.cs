using UnityEngine;
using UnityEngine.SceneManagement;

namespace Simulador.HUD
{
    // Controlador principal del hub de navegación y la interfaz de usuario en el menú inicial.
    // Administra la conmutación de estados visuales (paneles), la inicialización del ciclo de vida de simulación 
    // y la carga asíncrona/sincrónicade escenas mediante el motor de Unity.
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Referencia de Paneles")]
        [Tooltip("Panel principal con los botones de acceso primarios del sistema.")]
        public GameObject panelPrincipal;
        [Tooltip("Panel selector de niveles y escenarios de prueba (Maniobras / Urbano).")]
        public GameObject panelNiveles;
        [Tooltip("Panel de consulta de controles de hardware y mapa de entradas.")]
        public GameObject panelControles;
        [Tooltip("Panel informativo con el reglamento de conducción y normativa DGT.")]
        public GameObject panelReglas;

        // Configura el estado inicial del menú, restablece la escala temporal y libera la interacción del cursor.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void Start()
        {
            // Al arrancar, forzamos que solo se vea el panel principal
            MostrarPanelPrincipal();
            
            // Aseguramos que el tiempo corre (por si venimos de una pausa en el nivel)
            Time.timeScale = 1f;
            
            // Liberamos el ratón para navegar por el menú
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // --- MÉTODOS DE NAVEGACIÓN ---

        // Activa la vista del panel principal.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        public void MostrarPanelPrincipal() => CambiarPanel(panelPrincipal);

        // Activa la vista del panel selector de niveles.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        public void MostrarPanelNiveles() => CambiarPanel(panelNiveles);

        // Activa la vista del panel de configuración/consulta de controles.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        public void MostrarPanelControles() => CambiarPanel(panelControles);

        // Activa la vista del panel con las reglas de evaluación DGT.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        public void MostrarPanelReglas() => CambiarPanel(panelReglas);

        // Alterna la visibilidad de las capas de la interfaz mediante la conmutación segura de GameObjects.
        // Parámetros:
        //   - panelDestino: GameObject del panel que se desea activar visualmente en pantalla.
        // Salida: Ninguna.
        private void CambiarPanel(GameObject panelDestino)
        {
            // Apagamos todos los paneles por seguridad
            panelPrincipal.SetActive(false);
            panelNiveles.SetActive(false);
            panelControles.SetActive(false);
            panelReglas.SetActive(false);

            // Encendemos el panel al que queremos ir
            if (panelDestino != null) panelDestino.SetActive(true);
        }

        // --- FUNCIONES DE ACCIÓN ---

        // Ejecuta el proceso de transición hacia una escena de juego o circuito específico.
        // Parámetros:
        //   - nombreEscena: Cadena identificadora con la nomenclatura exacta de la escena dentro del Build Profile.
        // Salida: Ninguna.
        public void CargarEscena(string nombreEscena)
        {
            Debug.Log("<color=cyan>SISTEMA:</color> Cargando " + nombreEscena);
            SceneManager.LoadScene(nombreEscena);
        }

        // Ejecuta el protocolo de cierre de la aplicación gestionando el comportamiento según el entorno de ejecución.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        public void SalirDelSimulador()
        {
            Debug.Log("<color=red>SISTEMA:</color> Cerrando aplicación...");
            Application.Quit();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }
}