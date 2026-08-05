using UnityEngine;

namespace Simulador.Core
{
    public class UrbanMissionManager : MonoBehaviour
    {
        [Header("Configuración")]
        public UrbanMissionProfileSO perfilExamen;
        
        [Header("Canales de Salida (SOA)")]
        public StringVariable objectiveSO;
        public Vector3Variable gpsTargetSO;
        public GameEvent onFinalComplete;

        private int taskIndex = -1;
        private float taskTimer = 0;
        private bool isExamRunning = false;

        private void Awake()
        {
            if (gpsTargetSO != null) gpsTargetSO.Value = Vector3.zero;
            taskIndex = -1;
            isExamRunning = false;
        }

        public void IniciarExamen(UrbanMissionProfileSO perfil)
        {
            perfilExamen = perfil;
            taskIndex = 0;
            isExamRunning = true;
            ConfigurarFase();
        }

        private void Update()
        {
            if (!isExamRunning || perfilExamen == null) return;

            var currentTask = perfilExamen.tareas[taskIndex];

            if (currentTask.tiempoMaximo > 0)
            {
                taskTimer += Time.deltaTime;

                if (currentTask.tipo == UrbanTaskType.ConduccionLibre)
                {
                    if (taskTimer >= currentTask.tiempoMaximo) AvanzarTarea();
                }
                else if (currentTask.tipo == UrbanTaskType.Estacionamiento)
                {
                    if (taskTimer >= currentTask.tiempoMaximo)
                    {
                        // Si se acaba el tiempo de parking, podrías suspender al alumno
                        Debug.Log("<color=red>TIEMPO AGOTADO</color>");
                        TerminarExamen(); 
                    }
                }
            }
        }

        public void AvanzarTarea(object data = null)
        {
            taskIndex++;
            if (taskIndex < perfilExamen.tareas.Count) ConfigurarFase();
            else TerminarExamen();
        }

        private void ConfigurarFase()
        {
            taskTimer = 0;
            var currentTask = perfilExamen.tareas[taskIndex];

            if (objectiveSO != null) objectiveSO.Value = currentTask.instruccion;

            // --- LÓGICA DE GPS POR TAG ---
            if (gpsTargetSO != null)
            {
                if (!string.IsNullOrEmpty(currentTask.tagDestino))
                {
                    // Buscamos el objeto que tiene ese Tag en la escena
                    GameObject targetObj = GameObject.FindWithTag(currentTask.tagDestino);
                    if (targetObj != null)
                    {
                        gpsTargetSO.Value = targetObj.transform.position;
                    }
                }
                else
                {
                    gpsTargetSO.Value = Vector3.zero; // Apaga la flecha si no hay tag
                }
            }

            // Activación de contenedores (como ya lo tenías)
            if (!string.IsNullOrEmpty(currentTask.tagContenedor))
            {
                GameObject group = GameObject.FindGameObjectWithTag(currentTask.tagContenedor);
                if (group != null) group.SetActive(true);
            }
        }

        private void TerminarExamen()
        {
            isExamRunning = false;
            if (gpsTargetSO != null) gpsTargetSO.Value = Vector3.zero;
            if (onFinalComplete != null) onFinalComplete.Raise();
        }
    }
}