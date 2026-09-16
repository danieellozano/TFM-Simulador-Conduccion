using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Simulador.Core;

namespace Simulador.Infrastructure
{
    // Gestor de intersecciones complejas y secuencia semafórica sincronizada.
    // Administra los ciclos de paso de vehículos en cruces mediante fases temporizadas (Verde, Ámbar, Rojo)
    // e introduce intervalos de seguridad (Red Overlap) para evacuar la zona de conflicto y prevenir colisiones.
    public class IntersectionManager : MonoBehaviour
    {
        [Header("Configuración de Tiempos")]
        [Tooltip("Duración en segundos del estado Verde para la fase activa.")]
        public float greenTime = 8f;
        [Tooltip("Duración en segundos del estado Ámbar para la fase activa.")]
        public float amberTime = 3f;
        [Tooltip("Tiempo de solapamiento en Rojo para evacuar la intersección antes de habilitar el siguiente flujo.")]
        public float redOverlap = 1.5f; 

        [Header("Grupos de Semáforos")]
        [Tooltip("Lista de controladores semafóricos asociados a la arteria principal/avenida.")]
        public List<TrafficLightController> grupoAvenida;
        [Tooltip("Lista de controladores semafóricos asociados al acceso izquierdo.")]
        public List<TrafficLightController> grupoIzquierda;
        [Tooltip("Lista de controladores semafóricos asociados al acceso derecho.")]
        public List<TrafficLightController> grupoDerecha;

        // Comprueba la asignación de elementos e inicia la corrutina del ciclo semafórico.
        // Parámetros: Ninguno.
        // Salida: Ninguna.
        private void Start()
        {
            if (grupoAvenida.Count > 0 || grupoIzquierda.Count > 0 || grupoDerecha.Count > 0)
            {
                StartCoroutine(IntersectionLoop());
            }
            else
            {
                Debug.LogWarning("IntersectionManager: No hay semáforos asignados en las listas.");
            }
        }

        // Corrutina principal que ejecuta de forma secuencial e indefinida el bucle de fases de la intersección.
        // Parámetros: Ninguno.
        // Salida: Iterador IEnumerador para el control del bucle asíncrono.
        private IEnumerator IntersectionLoop()
        {
            while (true)
            {
                // FASE 1: AVENIDA
                yield return StartCoroutine(ActivarFase(grupoAvenida, new List<List<TrafficLightController>> { grupoIzquierda, grupoDerecha }));

                // FASE 2: IZQUIERDA
                yield return StartCoroutine(ActivarFase(grupoIzquierda, new List<List<TrafficLightController>> { grupoAvenida, grupoDerecha }));

                // FASE 3: DERECHA
                yield return StartCoroutine(ActivarFase(grupoDerecha, new List<List<TrafficLightController>> { grupoAvenida, grupoIzquierda }));
            }
        }

        // Modula la transición de estados semafóricos para un grupo prioritario frente a los grupos detenidos.
        // Parámetros:
        //   - grupoVerde: Conjunto de semáforos que recibirán la secuencia Verde -> Ámbar -> Rojo.
        //   - gruposRojos: Lista de grupos de semáforos que deben permanecer detenidos en estado Rojo.
        // Salida: Iterador IEnumerador para controlar la temporización de los cambios de estado.
        private IEnumerator ActivarFase(List<TrafficLightController> grupoVerde, List<List<TrafficLightController>> gruposRojos)
        {
            // Poner todos los demás en Rojo
            foreach (var grupo in gruposRojos) SetGroupState(grupo, LightState.Red);
            
            // Verde actual
            SetGroupState(grupoVerde, LightState.Green);
            yield return new WaitForSeconds(greenTime);

            // Ámbar actual
            SetGroupState(grupoVerde, LightState.Amber);
            yield return new WaitForSeconds(amberTime);

            // Rojo actual y espera de seguridad
            SetGroupState(grupoVerde, LightState.Red);
            yield return new WaitForSeconds(redOverlap);
        }

        // Aplica de forma masiva un nuevo estado óptico (Rojo, Ámbar, Verde) a todos los semáforos de un grupo.
        // Parámetros:
        //   - group: Lista de controladores semafóricos a actualizar.
        //   - state: Estado semafórico objetivo a aplicar.
        // Salida: Ninguna.
        private void SetGroupState(List<TrafficLightController> group, LightState state)
        {
            foreach (var light in group)
            {
                if (light != null) light.SetState(state);
            }
        }
    }
}