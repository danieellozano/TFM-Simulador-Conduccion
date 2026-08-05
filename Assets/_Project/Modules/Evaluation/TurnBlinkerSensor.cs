using UnityEngine;
using Simulador.Core;

namespace Simulador.Evaluation
{
    public class SmartIntersectionEvaluator : MonoBehaviour
    {
        [Header("Referencias")]
        public InputDataSO inputData;
        public GameEvent infractionEvent;
        public InfraccionSO blinkerInfraction; // INT-IND (Leve)

        private Vector3 directionAtEntry;
        private int blinkerAtEntry;
        private bool isEvaluating = false;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // Guardamos la dirección en la que entraba y qué intermitente tenía
                directionAtEntry = other.transform.forward;
                blinkerAtEntry = inputData.ActiveBlinker;
                isEvaluating = true;
                
                Debug.Log("<color=white>DGT:</color> Evaluando maniobra en intersección...");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && isEvaluating)
            {
                Vector3 directionAtExit = other.transform.forward;
                
                // Calculamos el ángulo entre la entrada y la salida
                // Un ángulo positivo suele ser derecha, negativo izquierda
                float turnAngle = Vector3.SignedAngle(directionAtEntry, directionAtExit, Vector3.up);

                ValidarManiobra(turnAngle);
                isEvaluating = false;
            }
        }

        private void ValidarManiobra(float angle)
        {
            int requiredBlinker = 0; // Por defecto, seguir recto

            if (angle > 45f) requiredBlinker = 1;      // Giro a la derecha
            else if (angle < -45f) requiredBlinker = -1; // Giro a la izquierda
            else requiredBlinker = 0;                  // Seguir recto

            // Comparar con el intermitente que tenía al entrar
            if (blinkerAtEntry != requiredBlinker)
            {
                infractionEvent.Raise(blinkerInfraction);
                string maniobra = requiredBlinker == 1 ? "Derecha" : (requiredBlinker == -1 ? "Izquierda" : "Recto");
                Debug.Log($"<color=red>DGT:</color> Maniobra de giro a la {maniobra} mal señalizada.");
            }
            else
            {
                Debug.Log("<color=green>DGT:</color> Maniobra señalizada correctamente.");
            }
        }
    }
}