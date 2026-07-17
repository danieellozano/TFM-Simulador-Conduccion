using UnityEngine;
using System.Collections.Generic;

namespace Simulador.Core
{
    public class ManiobrasManager : MonoBehaviour
    {
        public StringVariable objectiveSO;
        private Dictionary<string, List<GameObject>> gruposHitos = new Dictionary<string, List<GameObject>>();
        private string[] ordenTags = { "Marker_Stop", "Marker_Eslalon", "Marker_Linea", "Marker_Bateria" };
        public int faseActual = 0; 

        private void Awake()
        {
            gruposHitos.Clear();
            foreach (string tag in ordenTags)
            {
                GameObject[] encontrados = GameObject.FindGameObjectsWithTag(tag);
                gruposHitos.Add(tag, new List<GameObject>(encontrados));
            }
        }

        private void Start() { ActualizarVisualesMision(); }

        // Mantenemos el (object data) para que sea compatible con el Listener
        public void AvanzarFase(object data)
        {
            faseActual++;
            Debug.Log("<color=yellow>MANAGER:</color> ¡Señal de avance recibida! Nueva fase: " + faseActual);
            ActualizarVisualesMision();
        }

        private void ActualizarVisualesMision()
        {
            // Apagar todo
            foreach (var lista in gruposHitos.Values)
                foreach (GameObject obj in lista) if(obj) obj.SetActive(false);

            // Encender lo que toca
            if (faseActual < ordenTags.Length)
            {
                string tagActual = ordenTags[faseActual];
                foreach (GameObject hito in gruposHitos[tagActual]) hito.SetActive(true);
                SetObjectiveText();
            }
            else { if(objectiveSO != null) objectiveSO.Value = "PRÁCTICA FINALIZADA"; }
        }

        private void SetObjectiveText()
        {
            if (objectiveSO == null) return;
            switch (faseActual)
            {
                case 0: objectiveSO.Value = "1. Deténgase en el STOP."; break;
                case 1: objectiveSO.Value = "2. Supere el eslalon y deténgase en el STOP."; break;
                case 2: objectiveSO.Value = "3. Aparque en el estacionamiento en LÍNEA."; break;
                case 3: objectiveSO.Value = "4. Continúe y estacione en el aparcamiento en BATERÍA."; break;
                default: objectiveSO.Value = "¡PRÁCTICA FINALIZADA!"; break;
            }
        }
    }
}