using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Simulador.AI
{
    public class AIStopManager : MonoBehaviour
    {
        public GameObject stopBar;         
        public float mandatoryWaitTime = 3.0f; // Tiempo mínimo de observación
        public BoxCollider[] dangerZones;  
        public LayerMask vehicleLayers;    

        private List<TrafficAIController> queue = new List<TrafficAIController>();
        private bool isProcessingQueue = false;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Traffic"))
            {
                TrafficAIController ai = other.GetComponent<TrafficAIController>();
                if (ai != null && !queue.Contains(ai))
                {
                    queue.Add(ai);
                    if (!isProcessingQueue) StartCoroutine(ProcessQueue());
                }
            }
        }

        private IEnumerator ProcessQueue()
        {
            isProcessingQueue = true;

            while (queue.Count > 0)
            {
                TrafficAIController currentCar = queue[0];
                if (currentCar == null) { queue.RemoveAt(0); continue; }

                if (stopBar != null) stopBar.SetActive(true);

                // 1. ESPERAR A QUE EL COCHE SE DETENGA
                while (currentCar != null && currentCar.GetCurrentSpeed() > 0.2f) yield return null;

                if (currentCar == null) continue;

                Debug.Log($"<color=cyan>IA STOP:</color> {currentCar.name} detenido. Iniciando protocolo de seguridad.");

                // --- LÓGICA DE VIGILANCIA DINÁMICA ---
                float timer = 0f;
                
                // El bucle se mantiene mientras NO hayan pasado los 3s 
                // O mientras HAYA una amenaza en la Danger Zone.
                while (timer < mandatoryWaitTime || IsAnyThreatApproaching(currentCar.transform))
                {
                    // Solo sumamos tiempo si NO hay nadie viniendo. 
                    // Esto obliga a que los 3s sean de "observación limpia".
                    if (!IsAnyThreatApproaching(currentCar.transform))
                    {
                        timer += Time.deltaTime;
                    }
                    else
                    {
                        // Si aparece alguien, el coche se bloquea (no suma tiempo de espera legal)
                        // Debug.Log("<color=orange>STOP:</color> Espera bloqueada por tráfico detectado.");
                    }

                    yield return null; // Comprobación cada frame
                }

                // 2. ABRIR PASO
                if (stopBar != null) stopBar.SetActive(false);
                Debug.Log($"<color=green>IA STOP:</color> Vía libre para {currentCar.name}.");

                // 3. ESPERAR SALIDA DEL VEHÍCULO
                float timeout = 0f;
                while (queue.Count > 0 && queue[0] == currentCar && timeout < 5f) 
                {
                    timeout += Time.deltaTime;
                    yield return null; 
                }

                if (stopBar != null) stopBar.SetActive(true);
            }

            isProcessingQueue = false;
        }

        private void OnTriggerExit(Collider other)
        {
            TrafficAIController ai = other.GetComponent<TrafficAIController>();
            if (ai != null && queue.Contains(ai))
            {
                queue.Remove(ai);
            }
        }

        private bool IsAnyThreatApproaching(Transform currentCar)
        {
            foreach (var zone in dangerZones)
            {
                Collider[] vehicles = Physics.OverlapBox(zone.bounds.center, zone.bounds.extents, zone.transform.rotation, vehicleLayers);
                foreach (var v in vehicles)
                {
                    if (v.transform.root == currentCar.root) continue;
                    if (v.CompareTag("Player") || v.CompareTag("Traffic"))
                    {
                        Vector3 directionToStop = transform.position - v.transform.position;
                        float approachCheck = Vector3.Dot(v.transform.forward, directionToStop.normalized);

                        // Si el valor es positivo, el coche viene hacia nosotros
                        if (approachCheck > 0.1f) return true;
                    }
                }
            }
            return false;
        }
    }
}