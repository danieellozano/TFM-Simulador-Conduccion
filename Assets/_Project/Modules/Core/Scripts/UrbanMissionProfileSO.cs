using UnityEngine;
using System.Collections.Generic;

namespace Simulador.Core
{
    public enum UrbanTaskType { ConduccionLibre, ConduccionGuiada, Estacionamiento }

    [System.Serializable]
    public class UrbanTask
    {
        public UrbanTaskType tipo;
        public string instruccion;
        public float tiempoMaximo; 
        public GameEvent eventoExito; 
        public string tagDestino; 
        public string tagContenedor; 
    }

    [CreateAssetMenu(fileName = "NuevoPerfilUrbano", menuName = "Simulador/Misiones/Perfil Urbano")]
    public class UrbanMissionProfileSO : ScriptableObject
    {
        public List<UrbanTask> tareas;
    }
}