using UnityEngine;

namespace Simulador.Core
{
    public class UrbanMissionManager : MonoBehaviour
    {
        [Header("Configuración de Perfil")]
        public UrbanMissionProfileSO perfilExamen;
        
        [Header("Canales de Salida (SOA)")]
        public StringVariable objectiveSO;
        public Vector3Variable gpsTargetSO; 
        public FloatVariable currentSpeedLimitSO; // Referencia para resetear a 0
        public GameEvent onFinalComplete;

        [Header("Estado de la Misión")]
        public int taskIndex = -1;
        private float taskTimer = 0;
        private bool isExamRunning = false;

        private void Awake()
        {
            // Reset de seguridad al iniciar el Play
            if (gpsTargetSO != null) gpsTargetSO.Value = Vector3.zero;
            if (objectiveSO != null) objectiveSO.Value = "";
            if (currentSpeedLimitSO != null) currentSpeedLimitSO.Value = 0;

            taskIndex = -1;
            isExamRunning = false;
        }

        public void IniciarExamen(UrbanMissionProfileSO perfil)
        {
            if (perfil == null || perfil.tareas.Count == 0) return;

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

            // Escribimos la instrucción en el archivo compartido
            if (objectiveSO != null) 
            {
                objectiveSO.Value = currentTask.instruccion;
            }

            // Actualizamos destino GPS
            if (gpsTargetSO != null)
            {
                if (!string.IsNullOrEmpty(currentTask.tagDestino))
                {
                    GameObject targetObj = GameObject.FindWithTag(currentTask.tagDestino);
                    if (targetObj != null) gpsTargetSO.Value = targetObj.transform.position;
                    else gpsTargetSO.Value = Vector3.zero;
                }
                else gpsTargetSO.Value = Vector3.zero;
            }

            // Activación de Triggers por Tag
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
            if (objectiveSO != null) objectiveSO.Value = "PRÁCTICA FINALIZADA";
            if (onFinalComplete != null) onFinalComplete.Raise();
        }
    }
}