using UnityEngine;

namespace Simulador.Core
{
    // Coordinador central del flujo de tareas y fases para el entorno urbano.
    // Gobierna el progreso del examen o práctica guiada a partir de un perfil configurable,
    // actualizando las instrucciones en la interfaz (HUD), las coordenadas de destino para
    // el sistema de navegación GPS y la activación espacial de desencadenadores en la escena.
    public class UrbanMissionManager : MonoBehaviour
    {
        [Header("Configuración de Perfil")]
        [Tooltip("Perfil de misión ScriptableObject que define la secuencia de tareas del examen.")]
        public UrbanMissionProfileSO perfilExamen;
        
        [Header("Canales de Salida (SOA)")]
        [Tooltip("Canal de datos para comunicar la instrucción textual activa al HUD.")]
        public StringVariable objectiveSO;

        [Tooltip("Canal de datos para transmitir la posición espacial del siguiente hito al GPS.")]
        public Vector3Variable gpsTargetSO; 

        [Tooltip("Referencia al canal de límite de velocidad para restablecer su valor al inicio.")]
        public FloatVariable currentSpeedLimitSO;

        [Tooltip("Evento reactivo disparado al completar con éxito todas las tareas del perfil.")]
        public GameEvent onFinalComplete;

        [Header("Estado de la Misión")]
        [Tooltip("Índice de la tarea actualmente en ejecución (-1 indica inactividad).")]
        public int taskIndex = -1;

        // Cronómetro acumulado de tiempo para la tarea activa
        private float taskTimer = 0;

        // Bandera que indica si la sesión de evaluación urbana está en curso
        private bool isExamRunning = false;

        // Restablece de forma determinista el estado de los canales de datos al inicializar la escena.
        private void Awake()
        {
            if (gpsTargetSO != null) gpsTargetSO.Value = Vector3.zero;
            if (objectiveSO != null) objectiveSO.Value = "";
            if (currentSpeedLimitSO != null) currentSpeedLimitSO.Value = 0;

            taskIndex = -1;
            isExamRunning = false;
        }

        // Inicializa el examen cargando el perfil de tareas pedagógicas proporcionado.
        // Parámetros:
        //   perfil: Activo de datos que contiene el conjunto ordenado de tareas del ejercicio.
        public void IniciarExamen(UrbanMissionProfileSO perfil)
        {
            if (perfil == null || perfil.tareas.Count == 0) return;

            perfilExamen = perfil;
            taskIndex = 0;
            isExamRunning = true;
            ConfigurarFase();
        }

        // Supervisa el cumplimiento de límites temporales en tareas de conducción libre.
        private void Update()
        {
            if (!isExamRunning || perfilExamen == null) return;

            var currentTask = perfilExamen.tareas[taskIndex];

            // Si la tarea tiene un tiempo límite configurado, se evalúa su expiración
            if (currentTask.tiempoMaximo > 0)
            {
                taskTimer += Time.deltaTime;
                if (currentTask.tipo == UrbanTaskType.ConduccionLibre)
                {
                    // Al expirar el tiempo de conducción libre, se transiciona a la siguiente fase
                    if (taskTimer >= currentTask.tiempoMaximo) AvanzarTarea();
                }
            }
        }

        // Avanza el flujo de la prueba hacia la siguiente tarea o concluye la sesión si era la última.
        // Parámetros:
        //   data: Carga útil del evento disparador (compatible con la firma de GameEventListener).
        public void AvanzarTarea(object data = null)
        {
            taskIndex++;
            if (taskIndex < perfilExamen.tareas.Count) ConfigurarFase();
            else TerminarExamen();
        }

        // Prepara los elementos de la escena y la interfaz para la tarea activa del examen.
        private void ConfigurarFase()
        {
            taskTimer = 0;
            var currentTask = perfilExamen.tareas[taskIndex];

            // Actualiza la instrucción textual en el canal de datos compartido del HUD
            if (objectiveSO != null) 
            {
                objectiveSO.Value = currentTask.instruccion;
            }

            // Localiza el objeto de destino en la escena e inyecta su posición en el canal del GPS
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

            // Habilita los volúmenes de control espaciales (triggers) requeridos para esta tarea
            if (!string.IsNullOrEmpty(currentTask.tagContenedor))
            {
                GameObject group = GameObject.FindGameObjectWithTag(currentTask.tagContenedor);
                if (group != null) group.SetActive(true);
            }
        }

        // Concluye la prueba, limpia los canales de navegación y emite el evento global de finalización.
        private void TerminarExamen()
        {
            isExamRunning = false;
            if (gpsTargetSO != null) gpsTargetSO.Value = Vector3.zero;
            if (objectiveSO != null) objectiveSO.Value = "PRÁCTICA FINALIZADA";
            if (onFinalComplete != null) onFinalComplete.Raise();
        }
    }
}