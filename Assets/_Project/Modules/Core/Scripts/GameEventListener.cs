using UnityEngine;
using UnityEngine.Events;

namespace Simulador.Core
{
    public class GameEventListener : MonoBehaviour
    {
        [Tooltip("El archivo de evento que este componente va a vigilar")]
        public GameEvent Event;

        [Tooltip("Acción que se ejecutará en Unity cuando el evento ocurra")]
        public UnityEvent<object> Response;

        // Se registra al activarse el objeto en la escena
        private void OnEnable()
        {
            if (Event != null) Event.RegisterListener(this);
        }

        // Se desregistra al desactivarse para no dar errores de memoria
        private void OnDisable()
        {
            if (Event != null) Event.UnregisterListener(this);
        }

        // Este método lo llama el GameEvent cuando alguien hace .Raise()
        public void OnEventRaised(object data)
        {
            Response.Invoke(data);
        }
    }
}