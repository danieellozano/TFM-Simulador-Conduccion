using UnityEngine;
using UnityEngine.Events;

namespace Simulador.Core
{
    // Componente mediador que implementa la parte receptora.
    // Vincula un canal de datos ScriptableObject (GameEvent) con respuestas en el entorno de Unity
    // mediante eventos parametrizados (UnityEvent), permitiendo que los módulos
    // reaccionen a cambios globales sin acoplarse directamente a los emisores del suceso.
    public class GameEventListener : MonoBehaviour
    {
        [Tooltip("El archivo de evento ScriptableObject que este componente va a escuchar.")]
        public GameEvent Event;

        [Tooltip("Acción o método de Unity que se ejecutará en respuesta al disparo del evento.")]
        public UnityEvent<object> Response;

        // Suscribe automáticamente el componente al canal de eventos cuando el GameObject se activa en la escena.
        private void OnEnable()
        {
            if (Event != null) Event.RegisterListener(this);
        }

        // Da de baja la suscripción cuando el GameObject se desactiva o destruye,
        // garantizando la liberación de referencias y evitando fugas de memoria o llamadas a objetos inactivos.
        private void OnDisable()
        {
            if (Event != null) Event.UnregisterListener(this);
        }

        // Método invocado directamente por la clase GameEvent al ejecutar su llamada .Raise().
        // Propaga los datos del evento ejecutando todas las acciones enlazadas en el inspector de Unity.
        // Parámetros:
        //   data: Información o carga útil emitida junto con el evento (ej. objeto de infracción o nulo).
        public void OnEventRaised(object data)
        {
            Response?.Invoke(data);
        }
    }
}