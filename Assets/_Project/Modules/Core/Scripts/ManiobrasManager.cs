using UnityEngine;
using System.Collections.Generic;

namespace Simulador.Core
{
    // Gestor de progresión secuencial de ejercicios para el circuito de maniobras.
    // Administra la visibilidad de los hitos físicos y visuales en la escena (balizas,
    // zonas de detención y áreas de aparcamiento) en función de la etapa activa,
    // actualizando las instrucciones pedagógicas proyectadas en la interfaz (HUD)
    // a través del canal de datos objectiveSO.
    public class ManiobrasManager : MonoBehaviour
    {
        [Tooltip("Canal de datos ScriptableObject donde se escribe la instrucción activa para el HUD.")]
        public StringVariable objectiveSO;

        // Diccionario que clasifica y almacena en memoria los objetos visuales según su etiqueta de fase
        private Dictionary<string, List<GameObject>> gruposHitos = new Dictionary<string, List<GameObject>>();

        // Secuencia lógica de etiquetas que define el orden del examen de maniobras
        private string[] ordenTags = { "Marker_Stop", "Marker_Eslalon", "Marker_Linea", "Marker_Bateria" };

        [Tooltip("Índice de la fase actual del circuito en ejecución.")]
        public int faseActual = 0; 

        // Rastrea y clasifica al iniciar la escena todos los objetos de referencia vinculados a cada prueba.
        private void Awake()
        {
            gruposHitos.Clear();
            foreach (string tag in ordenTags)
            {
                GameObject[] encontrados = GameObject.FindGameObjectsWithTag(tag);
                gruposHitos.Add(tag, new List<GameObject>(encontrados));
            }
        }

        // Inicializa los marcadores visuales del escenario en la primera fase.
        private void Start() 
        { 
            ActualizarVisualesMision(); 
        }

        // Avanza el progreso de la prueba a la siguiente fase y actualiza el entorno y la interfaz.
        // Este método actúa como receptor directo del bus de eventos (GameEventListener).
        // Parámetros:
        //   data: Carga útil del evento disparador (mantenida para compatibilidad con la firma del Listener).
        public void AvanzarFase(object data)
        {
            faseActual++;
            Debug.Log("<color=yellow>MANAGER:</color> ¡Señal de avance recibida! Nueva fase: " + faseActual);
            ActualizarVisualesMision();
        }

        // Oculta los elementos de fases inactivas y activa únicamente los hitos espaciales del ejercicio actual.
        // Si el alumno supera todos los hitos, marca la sesión como completada.
        private void ActualizarVisualesMision()
        {
            // Oculta todos los elementos visuales registrados en el diccionario
            foreach (var lista in gruposHitos.Values)
            {
                foreach (GameObject obj in lista) 
                {
                    if (obj) obj.SetActive(false);
                }
            }

            // Activa exclusivamente los elementos correspondientes a la fase activa
            if (faseActual < ordenTags.Length)
            {
                string tagActual = ordenTags[faseActual];
                foreach (GameObject hito in gruposHitos[tagActual]) 
                {
                    hito.SetActive(true);
                }
                SetObjectiveText();
            }
            else 
            { 
                if (objectiveSO != null) objectiveSO.Value = "PRÁCTICA FINALIZADA"; 
            }
        }

        // Asigna la guía textual correspondiente a la fase activa en la variable compartida de la interfaz.
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