using UnityEngine;

namespace EnElCamino
{
    public class LadderPlacementInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private LadderInteractable exteriorLadder;

        public bool CanInteract => gameFlow != null && exteriorLadder != null && (gameFlow.exteriorLadderCarried || gameFlow.exteriorLadderPlaced);

        public string Prompt
        {
            get
            {
                if (gameFlow != null && gameFlow.exteriorLadderPlaced) return "[E] Bajar por la escalera";
                return "[E] Colocar la escalera en el hueco";
            }
        }

        public void Interact()
        {
            if (gameFlow == null || exteriorLadder == null || gameFlow.gameOver || gameFlow.finished) return;

            if (gameFlow.exteriorLadderCarried)
            {
                exteriorLadder.PlaceAtHole(transform);
                return;
            }

            if (gameFlow.exteriorLadderPlaced)
                exteriorLadder.Interact();
        }
    }
}
