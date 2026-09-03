using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Simulador.Core;

namespace Simulador.Infrastructure
{
    public class IntersectionManager : MonoBehaviour
    {
        [Header("Configuración de Tiempos")]
        public float greenTime = 8f;
        public float amberTime = 3f;
        public float redOverlap = 1.5f; 

        [Header("Grupos de Semáforos")]
        public List<TrafficLightController> grupoAvenida;
        public List<TrafficLightController> grupoIzquierda;
        public List<TrafficLightController> grupoDerecha;

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

        private void SetGroupState(List<TrafficLightController> group, LightState state)
        {
            foreach (var light in group)
            {
                if (light != null) light.SetState(state);
            }
        }
    }
}