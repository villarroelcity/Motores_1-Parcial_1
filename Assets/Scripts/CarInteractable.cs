using UnityEngine;

namespace EnElCamino
{
    public class CarInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private FuelCan fuelCan;
        [SerializeField] private CarDriver carDriver;
        private bool used;

        public string Prompt
        {
            get
            {
                if (used) return "[E] Subir al auto";
                if (!gameFlow.fuelCanFull) return "Falta llenar el bidón";
                return "[E] Cargar tanque del auto";
            }
        }

        public void Interact()
        {
            if (used)
            {
                carDriver.EnterCar();
                return;
            }

            if (!gameFlow.fuelCanFull)
            {
                gameFlow.ShowMessage("Primero llená el bidón en el surtidor.");
                return;
            }

            used = true;
            gameFlow.FillCar();
            fuelCan.Use();
        }
    }
}
