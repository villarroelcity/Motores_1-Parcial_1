using UnityEngine;

namespace EnElCamino
{
    public class RoofHoleFallTrigger : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;

        private void OnTriggerEnter(Collider other)
        {
            if (gameFlow == null || gameFlow.gameOver || gameFlow.finished || gameFlow.exteriorLadderPlaced) return;
            if (other.GetComponentInParent<PlayerController>() != null)
                gameFlow.GameOver();
        }
    }
}
