using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Simulador.HUD
{
    public class MissionUI : MonoBehaviour
    {
        [Header("Ajustes de Notificación")]
        public float messageDuration = 5f;
        public GameObject notificationPanel;
        public TextMeshProUGUI notificationText;

        [Header("Panel Final (Resultados)")]
        public GameObject summaryPanel;
        public TextMeshProUGUI finalTitleText;

        [Header("Guía de Misión (SOA)")]
        public TextMeshProUGUI objectiveText;
        public Core.StringVariable objectiveSO; 

        [Header("Referencias de Evaluación")]
        public Evaluation.DGTEvaluator evaluator;

        private void Start()
        {
            // Inicialización de estado de interfaz
            if (notificationPanel != null) notificationPanel.SetActive(false);
            if (summaryPanel != null) summaryPanel.SetActive(false);
            
            ActualizarTextoObjetivo();
        }

        private void Update()
        {
            // Sincronización automática con la capa de datos (Core)
            ActualizarTextoObjetivo();
        }

        private void ActualizarTextoObjetivo()
        {
            if (objectiveSO != null && objectiveText != null)
            {
                objectiveText.text = objectiveSO.Value;
            }
        }

        #region Notificaciones Temporales
        public void MostrarExitoManiobra(object data = null)
        {
            StopAllCoroutines();
            StartCoroutine(NotificationRoutine("¡APARCAMIENTO CORRECTO!"));
        }

        private IEnumerator NotificationRoutine(string message)
        {
            if (notificationPanel == null) yield break;

            notificationPanel.SetActive(true);
            notificationText.text = message;
            yield return new WaitForSeconds(messageDuration); 
            notificationPanel.SetActive(false);
        }
        #endregion

        #region Gestión de Estados de Fin de Sesión
        public void MostrarPantallaFinal(object data = null)
        {
            summaryPanel.SetActive(true);
            
            // 1. Consultar el veredicto
            bool aprobado = evaluator.EsApto();

            // 2. Formatear el texto de resultados
            string veredicto = aprobado ? "<color=green>APTO</color>" : "<color=red>NO APTO</color>";
            
            finalTitleText.text = $"RESULTADO: {veredicto}\n\n" +
                                $"<size=60%>Faltas Eliminatorias: {evaluator.faltasEliminatorias}\n" +
                                $"Faltas Deficientes: {evaluator.faltasDeficientes}\n" +
                                $"Faltas Leves: {evaluator.faltasLeves}</size>";

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void BotonReintentar()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Debug.Log("<color=cyan>SISTEMA:</color> Reiniciando sesión...");
        }

        public void BotonSalir()
        {
            Debug.Log("<color=red>SISTEMA:</color> Finalizando ejecución...");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        #endregion
    }
}