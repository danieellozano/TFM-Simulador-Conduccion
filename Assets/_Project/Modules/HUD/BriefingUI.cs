using UnityEngine;
using UnityEngine.SceneManagement;
using Simulador.Core;     

namespace Simulador.HUD
{
    public class BriefingUI : MonoBehaviour
    {
        [Header("Configuración de Inicio")]
        public GameObject briefingPanel;
        public GameObject inputManager;

        [Header("Selector de Transmisión Reutilizable")]
        public SelectorTransmisionUI transmissionSelector;

        [Header("Evaluación DGT")]
        [SerializeField] private MonoBehaviour dgtEvaluator;
        public IEvaluacionProvider evaluator => dgtEvaluator as IEvaluacionProvider;

        private void Awake()
        {
            Time.timeScale = 0f;
            if (inputManager != null) inputManager.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (evaluator != null)
            {
                evaluator.modoActual = ModoDeJuego.Maniobras;
            }
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

        public void StartMission()
        {
            if (briefingPanel != null) briefingPanel.SetActive(false);
            if (transmissionSelector != null)
            {
                transmissionSelector.MostrarSelector();
            }
            else
            {
                ComenzarMisionFisicamente();
            }
        }

        private void AlConfirmarTransmision()
        {
            ComenzarMisionFisicamente();
        }

        private void ComenzarMisionFisicamente()
        {
            if (inputManager != null) inputManager.SetActive(true);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("<color=green>SISTEMA:</color> Entrada de datos habilitada.");
        }
    }
}