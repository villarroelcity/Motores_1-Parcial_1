using UnityEngine;

namespace EnElCamino
{
    public class TableroElectrico : MonoBehaviour, IInteractable
    {
        [SerializeField] private Juego gameFlow;
        [SerializeField] private Renderer statusLight;
        [SerializeField] private Light statusLamp;

        private void Awake()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<Juego>();
            if (statusLight == null) statusLight = transform.Find("LuzEstado")?.GetComponent<Renderer>();
            if (statusLamp == null) statusLamp = transform.Find("LuzTablero")?.GetComponent<Light>();
            if (gameFlow != null)
            {
                gameFlow.GeneratorFuelDepleted += RefreshAfterPowerLoss;
                gameFlow.GameLost += RefreshAfterPowerLoss;
                gameFlow.GameFinished += RefreshAfterPowerLoss;
            }
            RefreshVisualState();
        }

        private void OnDestroy()
        {
            if (gameFlow == null) return;
            gameFlow.GeneratorFuelDepleted -= RefreshAfterPowerLoss;
            gameFlow.GameLost -= RefreshAfterPowerLoss;
            gameFlow.GameFinished -= RefreshAfterPowerLoss;
        }

        public string Prompt => gameFlow == null || !gameFlow.generatorOn
            ? "Primero encende el generador"
            : gameFlow.stationPowerConnected ? "Switch encendido: estacion con energia" : "[E] Enciende la caja electrica";

        public void Interact()
        {
            if (gameFlow == null || gameFlow.gameOver || gameFlow.finished) return;
            if (!gameFlow.generatorOn)
            {
                gameFlow.ShowMessage("Primero encende el generador.");
                return;
            }
            if (!gameFlow.insideGarage)
            {
                gameFlow.ShowMessage("Conecta el generador a esta caja desde adentro del taller.");
                return;
            }
            if (gameFlow.stationPowerConnected) return;

            gameFlow.ConnectStationPower();
            RefreshVisualState();
        }

        private void RefreshVisualState()
        {
            if (statusLight != null)
            {
                Color color = gameFlow != null && gameFlow.stationPowerConnected
                    ? new Color(0.15f, 0.95f, 0.28f)
                    : new Color(0.9f, 0.12f, 0.04f);
                VehicleLightUtility.SetAppearance(statusLight, color, color * 1.8f);
            }

            if (statusLamp != null)
            {
                bool powered = gameFlow != null && gameFlow.stationPowerConnected;
                statusLamp.enabled = powered;
                statusLamp.color = new Color(0.05f, 1f, 0.12f);
                statusLamp.intensity = powered ? 2.5f : 0f;
                statusLamp.range = 3f;
            }

            Transform breaker = transform.Find("LlaveTermica");
            if (breaker != null)
                breaker.localRotation = Quaternion.Euler(0f, 0f,
                    gameFlow != null && gameFlow.stationPowerConnected ? -24f : 24f);
        }

        private void RefreshAfterPowerLoss()
        {
            // El agotamiento del generador corta la electricidad, pero no
            // desenchufa fisicamente el cable: debe seguir conectado a la caja.
            RefreshVisualState();
        }

    }
}
