using UnityEngine;

namespace EnElCamino
{
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }
        public bool GeneratorActive { get; private set; }
        public bool FuelLoaded { get; private set; }
        public bool Finished { get; private set; }

        [SerializeField] private GameObject generatorMarker;
        [SerializeField] private GameObject fuelMarker;
        [SerializeField] private GameObject exitMarker;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            UpdateMarkers();
            GameUI.Instance?.SetObjective("OBJETIVO ACTUAL  ·  1/3\nENCENDÉ EL GENERADOR\nSeguí el haz amarillo detrás de la estación y presioná [E].");
            GameUI.Instance?.ShowMessage("MISIÓN: Hacé que la estación vuelva a tener energía.");
        }

        public void ConfigureObjectiveMarkers(GameObject generator, GameObject fuel, GameObject exit)
        {
            generatorMarker = generator;
            fuelMarker = fuel;
            exitMarker = exit;
            UpdateMarkers();
        }

        public void ActivateGenerator()
        {
            if (GeneratorActive) return;
            GeneratorActive = true;
            UpdateMarkers();
            GameUI.Instance?.SetObjective("OBJETIVO ACTUAL  ·  2/3\nCARGÁ COMBUSTIBLE\nSeguí el haz celeste hasta el surtidor y presioná [E].");
            GameUI.Instance?.ShowMessage("Generador encendido. El surtidor ya tiene energía.");
        }

        public void LoadFuel()
        {
            if (!GeneratorActive || FuelLoaded) return;
            FuelLoaded = true;
            UpdateMarkers();
            GameUI.Instance?.SetObjective("OBJETIVO ACTUAL  ·  3/3\nVOLVÉ AL CAMINO\nSeguí el haz verde hasta la salida de la estación.");
            GameUI.Instance?.ShowMessage("Combustible cargado. Ya podés continuar el viaje.");
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

        private void UpdateMarkers()
        {
            SetMarker(generatorMarker, !GeneratorActive);
            SetMarker(fuelMarker, GeneratorActive && !FuelLoaded);
            SetMarker(exitMarker, FuelLoaded && !Finished);
        }

        private static void SetMarker(GameObject marker, bool visible)
        {
            if (marker != null) marker.SetActive(visible);
        }
    }
}
