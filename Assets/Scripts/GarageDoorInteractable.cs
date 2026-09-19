using UnityEngine;

namespace EnElCamino
{
    public class GarageDoorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private GameObject garageDoor;
        private bool open;

        public string Prompt
        {
            get
            {
                if (open) return "Portón abierto";
                if (!gameFlow.insideGarage) return "El portón se abre desde adentro";
                return "[E] Abrir portón";
            }
        }

        public void Interact()
        {
            if (open) return;

            if (!gameFlow.insideGarage)
            {
                gameFlow.ShowMessage("Tenés que entrar al garage para abrir el portón.");
                return;
            }

            open = true;
            garageDoor.SetActive(false);
            gameFlow.OpenGarage();
        }
    }
}
