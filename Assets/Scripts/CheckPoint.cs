using UnityEngine;

namespace EnElCamino
{
    public class CheckPoint : MonoBehaviour
    {
        [SerializeField] private Juego gameFlow;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<AutoJugador>() == null || gameFlow == null) return;

            if (name == "Persecucion")
                gameFlow.NotifyPursuitTrigger();
            else if (name == "SalidaAlCamino")
            {
                // This checkpoint only ends the first daylight section. It
                // never displays victory or freezes the chase.
                gameFlow.ReachRoad();
            }
            else
                gameFlow.TryFinish();
        }
    }
}
