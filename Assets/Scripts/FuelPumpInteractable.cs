using UnityEngine;

namespace EnElCamino
{
    public class FuelPumpInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameObject fuelCanPrefab;
        [SerializeField] private Transform spawnPoint;
        private bool used;

        public string Prompt
        {
            get
            {
                if (used) return "Surtidor utilizado";
                if (GameFlow.Instance != null && !GameFlow.Instance.GeneratorActive) return "Surtidor sin energía";
                return "[E] Cargar combustible";
            }
        }

        public void Configure(GameObject prefab, Transform point)
        {
            fuelCanPrefab = prefab;
            spawnPoint = point;
        }

        public void Interact()
        {
            if (used) return;
            if (GameFlow.Instance == null || !GameFlow.Instance.GeneratorActive)
            {
                GameUI.Instance?.ShowMessage("El surtidor necesita energía.");
                return;
            }

            used = true;
            if (fuelCanPrefab != null && spawnPoint != null)
                Instantiate(fuelCanPrefab, spawnPoint.position, spawnPoint.rotation);
            GameFlow.Instance.LoadFuel();
        }
    }
}
