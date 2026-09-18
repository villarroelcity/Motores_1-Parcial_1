using UnityEngine;

namespace EnElCamino
{
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }
        public bool GeneratorActive { get; private set; }
        public bool FuelLoaded { get; private set; }
        public bool Finished { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            GameUI.Instance?.SetObjective("1. Explorá la estación y encontrá el generador.");
        }

        public void ActivateGenerator()
        {
            if (GeneratorActive) return;
            GeneratorActive = true;
            GameUI.Instance?.SetObjective("2. Volvé al surtidor y cargá combustible.");
            GameUI.Instance?.ShowMessage("La estación volvió a tener energía.");
        }

        public void LoadFuel()
        {
            if (!GeneratorActive || FuelLoaded) return;
            FuelLoaded = true;
            GameUI.Instance?.SetObjective("3. Salí de la estación y volvé al camino.");
            GameUI.Instance?.ShowMessage("Combustible cargado. Ya podés continuar.");
        }

        public void TryFinish()
        {
            if (Finished) return;
            if (!FuelLoaded)
            {
                GameUI.Instance?.ShowMessage("Primero necesitás cargar combustible.");
                return;
            }

            Finished = true;
            GameUI.Instance?.ShowCompleted();
        }
    }
}
