// using UnityEngine;
// using UnityEngine.SceneManagement;

// namespace Simulador.HUD
// {
//     public class BriefingUI : MonoBehaviour
//     {
//         [Header("Configuración de Inicio")]
//         public GameObject briefingPanel; // El Panel de instrucciones
//         public GameObject inputManager;  // El objeto Input_Manager de la jerarquía

//         [Header("Selector de Transmisión Reutilizable")]
//         public SelectorTransmisionUI transmissionSelector; // <--- NUEVO: Arrastra el script del panel

//         private void Awake()
//         {
//             Time.timeScale = 0f;
//             if (inputManager != null) inputManager.SetActive(false);

//             Cursor.lockState = CursorLockMode.None;
//             Cursor.visible = true;
//         }

//         private void Start()
//         {
//             // NUEVO: Nos suscribimos al evento del selector independiente
//             if (transmissionSelector != null)
//             {
//                 transmissionSelector.OnTransmissionSelected += AlConfirmarTransmision;
//             }
//         }

//         private void OnDestroy()
//         {
//             // NUEVO: Nos desuscribimos por seguridad al destruir el objeto
//             if (transmissionSelector != null)
//             {
//                 transmissionSelector.OnTransmissionSelected -= AlConfirmarTransmision;
//             }
//         }

//         public void StartMission()
//         {
//             if (briefingPanel != null) briefingPanel.SetActive(false);

//             // NUEVO: En lugar de iniciar el juego inmediatamente, llamamos al selector de transmisión
//             if (transmissionSelector != null)
//             {
//                 transmissionSelector.MostrarSelector();
//             }
//             else
//             {
//                 // Fallback si olvidas asignar el selector en alguna escena
//                 ComenzarMisionFisicamente();
//             }
//         }

//         private void AlConfirmarTransmision()
//         {
//             // Se ejecuta de forma automática tras pulsar "Manual" o "Automático"
//             ComenzarMisionFisicamente();
//         }

//         private void ComenzarMisionFisicamente()
//         {
//             if (inputManager != null) inputManager.SetActive(true);
//             Time.timeScale = 1f;
//             Cursor.lockState = CursorLockMode.Locked; 
//             Cursor.visible = false;
//             Debug.Log("<color=green>SISTEMA:</color> Entrada de datos habilitada.");
//         }

//         public void BotonSalir()
//         {
//             #if UNITY_EDITOR
//                 UnityEditor.EditorApplication.isPlaying = false; 
//             #else
//                 Application.Quit(); 
//             #endif
//         }
//     }
// }

using UnityEngine;
using UnityEngine.SceneManagement;
using Simulador.Evaluation; // <--- IMPORTANTE: Importa el evaluador y el enumerado 'ModoDeJuego'
using Simulador.Core;       // <--- IMPORTANTE: Importa las variables SOA

namespace Simulador.HUD
{
    public class BriefingUI : MonoBehaviour
    {
        [Header("Configuración de Inicio")]
        public GameObject briefingPanel; // El Panel de instrucciones
        public GameObject inputManager;  // El objeto Input_Manager de la jerarquía

        [Header("Selector de Transmisión Reutilizable")]
        public SelectorTransmisionUI transmissionSelector; // <--- CORREGIDO: Tipo correcto del selector de transmisión

        [Header("Evaluación DGT")]
        public DGTEvaluator dgtEvaluator; // Referencia al componente/asset DGT_System

        private void Awake()
        {
            // 1. Pausamos el simulador
            Time.timeScale = 0f;
            
            // 2. Desactivamos el teclado por completo
            if (inputManager != null) inputManager.SetActive(false);

            // 3. Liberamos el ratón
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Al iniciar el nivel de maniobras, forzamos este modo de juego de forma preventiva
            if (dgtEvaluator != null)
            {
                dgtEvaluator.modoActual = ModoDeJuego.Maniobras;
            }
        }

        private void Start()
        {
            // Sincronizamos la confirmación con el selector de transmisión independiente
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

            // En lugar de iniciar el juego inmediatamente, llamamos al selector de transmisión
            if (transmissionSelector != null)
            {
                transmissionSelector.MostrarSelector();
            }
            else
            {
                // Fallback si no hay selector de transmisión en esta escena
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