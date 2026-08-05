using UnityEngine;
using Simulador.Core;
using UnityEngine.SceneManagement;

namespace Simulador.HUD
{
    public class UrbanBriefingUI : MonoBehaviour
    {
        public GameObject urbanBriefingPanel;
        public UrbanMissionManager urbanManager;
        public GameObject inputManager;

        [Header("Perfiles de Misión")]
        public UrbanMissionProfileSO perfilExamen;
        public UrbanMissionProfileSO perfilLibre;

        private void Awake()
        {
            // Bloqueo inicial
            Time.timeScale = 0f;
            if (inputManager != null) inputManager.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Estas funciones se asignan a los botones físicos
        public void SeleccionarExamen() { Empezar(perfilExamen); }
        public void SeleccionarLibre() { Empezar(perfilLibre); }

        private void Empezar(UrbanMissionProfileSO perfil)
        {
            if (urbanManager == null) { Debug.LogError("Falta asignar el Urban_Manager en el HUD_Manager"); return; }

            if (urbanBriefingPanel != null) urbanBriefingPanel.SetActive(false);
            if (inputManager != null) inputManager.SetActive(true);
            
            Time.timeScale = 1f; 
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            urbanManager.IniciarExamen(perfil); 
        }

        public void VolverAlMenu()
        {
            Time.timeScale = 1f; // Resetear el tiempo siempre antes de cambiar de escena
            SceneManager.LoadScene("0_Menu_Principal"); // Asegúrate de que el nombre sea exacto
        }
    }
}