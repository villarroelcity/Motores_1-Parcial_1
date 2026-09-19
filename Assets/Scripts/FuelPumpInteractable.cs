using UnityEngine;

namespace EnElCamino
{
    public class FuelPumpInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private FuelCan fuelCan;
        private bool used;

        public string Prompt
        {
            get
            {
                if (!gameFlow.generatorOn) return "Surtidor sin energía";
                if (!gameFlow.hasFuelCan) return "Necesitás un bidón";
                if (used) return "Bidón lleno";
                return "[E] Llenar bidón";
            }
        }

        public void Interact()
        {
            if (used) return;
            if (!gameFlow.generatorOn)
            {
                gameFlow.ShowMessage("El surtidor necesita energía.");
                return;
            }

            if (!gameFlow.hasFuelCan)
            {
                gameFlow.ShowMessage("Necesitás traer el bidón vacío.");
                return;
            }

            used = true;
            fuelCan.Fill();
            gameFlow.FillFuelCan();
        }
    }
}
