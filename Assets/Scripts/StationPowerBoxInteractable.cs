using UnityEngine;

namespace EnElCamino
{
    public class StationPowerBoxInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Renderer statusLight;
        [SerializeField] private StationPowerCableVisual cableVisual;

        private void Awake()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            if (statusLight == null) statusLight = GetComponentInChildren<Renderer>(true);
            if (cableVisual == null) cableVisual = GetComponent<StationPowerCableVisual>();
        }

        public string Prompt => gameFlow == null || !gameFlow.generatorOn
            ? "Primero encendé el generador"
            : gameFlow.stationPowerConnected ? "Generador enchufado: estación con energía" : "[E] Enchufar generador a la caja eléctrica";

        public void Interact()
        {
            if (gameFlow == null || gameFlow.gameOver || gameFlow.finished) return;
            if (!gameFlow.generatorOn)
            {
                gameFlow.ShowMessage("Primero encendé el generador del taller.");
                return;
            }
            if (!gameFlow.insideGarage)
            {
                gameFlow.ShowMessage("Agarrá el cable del generador y enchufalo a esta caja desde adentro del taller.");
                return;
            }
            if (gameFlow.stationPowerConnected) return;

            gameFlow.ConnectStationPower();
            if (cableVisual != null) cableVisual.SetConnected(true);
            if (statusLight != null)
            {
                foreach (Material material in statusLight.materials)
                {
                    if (material != null && material.HasProperty("_Color")) material.color = Color.green;
                    if (material != null && material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.green);
                }
            }
        }
    }
}
