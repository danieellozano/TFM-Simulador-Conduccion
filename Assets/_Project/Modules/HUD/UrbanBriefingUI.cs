using UnityEngine;
using Simulador.Core;
using UnityEngine.SceneManagement;
using Simulador.Evaluation;

namespace Simulador.HUD
{
    public class UrbanBriefingUI : MonoBehaviour
    {
        [Header("Referencias de UI")]
        public GameObject urbanBriefingPanel;
        public SelectorTransmisionUI transmissionSelector;

        [Header("Referencias de Sistemas")]
        public UrbanMissionManager urbanManager;
        public GameObject inputManager;

        [Header("Perfiles de Misión")]
        public UrbanMissionProfileSO perfilExamen;
        public UrbanMissionProfileSO perfilLibre;

        public DGTEvaluator dgtEvaluator; 

        private UrbanMissionProfileSO perfilSeleccionado;

        private void Awake()
        {
            Time.timeScale = 0f;
            if (inputManager != null) inputManager.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Start()
        {
            // NUEVO: Suscripción al evento
            if (transmissionSelector != null)
            {
                transmissionSelector.OnTransmissionSelected += AlConfirmarTransmision;
            }
        }

        private void OnDestroy()
        {
            // NUEVO: Desuscripción
            if (transmissionSelector != null)
            {
                transmissionSelector.OnTransmissionSelected -= AlConfirmarTransmision;
            }
        }

        public void SeleccionarExamen() 
        { 
            if (dgtEvaluator != null) dgtEvaluator.modoActual = ModoDeJuego.ExamenUrbano;
            PrepararInicio(perfilExamen); 
        }

        public void SeleccionarLibre() 
        { 
            if (dgtEvaluator != null) dgtEvaluator.modoActual = ModoDeJuego.PracticaUrbana;
            PrepararInicio(perfilLibre); 
        }

        private void PrepararInicio(UrbanMissionProfileSO perfil)
        {
            perfilSeleccionado = perfil;

            if (urbanBriefingPanel != null) urbanBriefingPanel.SetActive(false);

            // Abrimos el selector de transmisión independiente
            if (transmissionSelector != null)
            {
                transmissionSelector.MostrarSelector();
            }
            else
            {
                Empezar(perfilSeleccionado);
            }
        }

        private void AlConfirmarTransmision()
        {
            Empezar(perfilSeleccionado);
        }

        private void Empezar(UrbanMissionProfileSO perfil)
        {
            if (urbanManager == null) { Debug.LogError("Falta asignar el Urban_Manager en el HUD_Manager"); return; }

            if (inputManager != null) inputManager.SetActive(true);
            
            Time.timeScale = 1f; 
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            urbanManager.IniciarExamen(perfil); 
        }

        public void VolverAlMenu()
        {
            Time.timeScale = 1f; 
            SceneManager.LoadScene("0_Menu_Principal"); 
        }
    }
}