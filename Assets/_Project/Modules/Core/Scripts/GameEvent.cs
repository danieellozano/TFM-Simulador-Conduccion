using System.Collections.Generic;
using UnityEngine;

namespace Simulador.Core
{
    // Canal de comunicación reactiva basado en ScriptableObject que implementa el patrón Observer.
    // Permite la emisión y recepción de sucesos globales de forma asíncrona y desacoplada,
    // evitando referencias directas entre los módulos del simulador.
    [CreateAssetMenu(fileName = "New Game Event", menuName = "Simulador/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        // Registro en memoria de todos los observadores de la escena actualmente suscritos a este canal
        private readonly List<GameEventListener> eventListeners = new List<GameEventListener>();

        // Notifica y propaga el evento a todos los componentes observadores registrados.
        // La lista se recorre en sentido inverso (desde el último al primero) para garantizar la
        // estabilidad en tiempo de ejecución, evitando errores de desbordamiento si un oyente
        // decide desuscribirse de manera reactiva en el mismo instante en que recibe la llamada.
        // Parámetros:
        //   data: Objeto opcional con la carga útil del evento (por ejemplo, una infracción InfraccionSO o nulo).
        public void Raise(object data = null)
        {
            for (int i = eventListeners.Count - 1; i >= 0; i--)
            {
                eventListeners[i].OnEventRaised(data);
            }
        }

        // Suscribe un nuevo componente observador a la lista de escucha de este evento.
        // Parámetros:
        //   listener: Componente GameEventListener de la escena que desea recibir las notificaciones.
        public void RegisterListener(GameEventListener listener) => eventListeners.Add(listener);

        // Da de baja a un observador de la lista, previniendo llamadas a objetos destruidos y fugas de memoria.
        // Parámetros:
        //   listener: Componente GameEventListener que se desactiva o destruye en la escena.
        public void UnregisterListener(GameEventListener listener) => eventListeners.Remove(listener);
    }
}