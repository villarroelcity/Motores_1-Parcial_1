using UnityEngine;

namespace EnElCamino
{
    public class LadderPlacementInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private LadderInteractable exteriorLadder;

        public bool CanInteract => gameFlow != null && exteriorLadder != null && exteriorLadder.IsAtRoof
            && !gameFlow.insideGarage && !gameFlow.gameOver && !gameFlow.finished;

        public string Prompt
        {
            get
            {
                return "[E] Colgarse del borde y bajar";
            }
        }

        public void Interact()
        {
            if (gameFlow == null || exteriorLadder == null || gameFlow.gameOver || gameFlow.finished) return;

            if (CanInteract) exteriorLadder.DescendThroughHole(transform);
        }
    }
}
