using UnityEngine;

namespace EnElCamino
{
    public class LadderInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Transform player;
        [SerializeField] private Transform destination;
        [SerializeField] private bool entersGarage;
        [SerializeField] private string promptText;

        public string Prompt
        {
            get
            {
                return promptText;
            }
        }

        public void Interact()
        {
            if (!gameFlow.generatorOn)
            {
                gameFlow.ShowMessage("Primero encendé el generador.");
                return;
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.position = destination.position;
            player.rotation = destination.rotation;
            controller.enabled = true;

            if (entersGarage) gameFlow.EnterGarage();
        }
    }
}
