using UnityEngine;
using Simulador.Core;
using UnityEngine.SceneManagement;

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
       
        [SerializeField] private MonoBehaviour dgtEvaluator; // Solo para el Inspector
        public IEvaluacionProvider evaluator => dgtEvaluator as IEvaluacionProvider; // Acceso limpio
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
            if (transmissionSelector != null)
            {
                transmissionSelector.OnTransmissionSelected += AlConfirmarTransmision;
            }
        }

        private void OnDestroy()
        {
            if (transmissionSelector != null)
            {
                transmissionSelector.OnTransmissionSelected -= AlConfirmarTransmision;
            }
        }

        public void SeleccionarExamen() 
        { 
            if (evaluator != null) evaluator.modoActual = ModoDeJuego.ExamenUrbano;
            PrepararInicio(perfilExamen); 
        }

        public void SeleccionarLibre() 
        { 
            if (evaluator != null) evaluator.modoActual = ModoDeJuego.PracticaUrbana;
            PrepararInicio(perfilLibre); 
        }

        private void PrepararInicio(UrbanMissionProfileSO perfil)
        {
            perfilSeleccionado = perfil;
            if (urbanBriefingPanel != null) urbanBriefingPanel.SetActive(false);

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