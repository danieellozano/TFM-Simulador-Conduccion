using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

namespace Simulador.HUD
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Referencia de Paneles")]
        public GameObject panelPrincipal;
        public GameObject panelNiveles;
        public GameObject panelControles;
        public GameObject panelReglas;

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

        public void MostrarPanelPrincipal() => CambiarPanel(panelPrincipal);
        public void MostrarPanelNiveles() => CambiarPanel(panelNiveles);
        public void MostrarPanelControles() => CambiarPanel(panelControles);
        public void MostrarPanelReglas() => CambiarPanel(panelReglas);

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

        public void CargarEscena(string nombreEscena)
        {
            Debug.Log("<color=cyan>SISTEMA:</color> Cargando " + nombreEscena);
            SceneManager.LoadScene(nombreEscena);
        }

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