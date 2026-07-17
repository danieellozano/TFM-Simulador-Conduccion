using System.Collections.Generic;
using UnityEngine;

namespace Simulador.Core
{
    [CreateAssetMenu(fileName = "New Game Event", menuName = "Simulador/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        // Lista de todos los que están escuchando este evento
        private readonly List<GameEventListener> eventListeners = new List<GameEventListener>();

        // Método para "gritar" el evento
        public void Raise(object data = null)
        {
            // Recorremos la lista al revés por seguridad si alguien se desuscribe al recibir el aviso
            for (int i = eventListeners.Count - 1; i >= 0; i--)
            {
                eventListeners[i].OnEventRaised(data);
            }
        }

        public void RegisterListener(GameEventListener listener) => eventListeners.Add(listener);
        public void UnregisterListener(GameEventListener listener) => eventListeners.Remove(listener);
    }
}