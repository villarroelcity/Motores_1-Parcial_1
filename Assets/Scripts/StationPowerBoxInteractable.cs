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
            if (statusLight == null) statusLight = transform.Find("LuzEstado")?.GetComponent<Renderer>();
            if (cableVisual == null) cableVisual = GetComponent<StationPowerCableVisual>();
            if (gameFlow != null)
            {
                gameFlow.GeneratorFuelDepleted += RefreshAfterPowerLoss;
                gameFlow.GameLost += RefreshAfterPowerLoss;
                gameFlow.GameFinished += RefreshAfterPowerLoss;
            }
            RestorePlayModeControls();
            RefreshVisualState();
        }

        private void OnDestroy()
        {
            if (gameFlow == null) return;
            gameFlow.GeneratorFuelDepleted -= RefreshAfterPowerLoss;
            gameFlow.GameLost -= RefreshAfterPowerLoss;
            gameFlow.GameFinished -= RefreshAfterPowerLoss;
        }

        private void Start()
        {
            // The board is assembled by editor setup scripts. Re-enable its controls
            // and single-sided labels at runtime so they remain visible in Game view.
            RestorePlayModeControls();
            RefreshVisualState();
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
            RefreshVisualState();
        }

        private void RestorePlayModeControls()
        {
            string[] visibleParts =
            {
                "TableroFrontal", "InterruptorGeneral", "LlaveTermica", "LuzEstado", "RótuloCaja", "RótuloToma"
            };

            foreach (string partName in visibleParts)
            {
                Transform part = transform.Find(partName);
                if (part == null) continue;
                if (!part.gameObject.activeSelf) part.gameObject.SetActive(true);

                foreach (Renderer partRenderer in part.GetComponentsInChildren<Renderer>(true))
                {
                    if (partRenderer == null) continue;
                    if (!partRenderer.gameObject.activeSelf) partRenderer.gameObject.SetActive(true);
                    partRenderer.enabled = true;
                    partRenderer.forceRenderingOff = false;
                }

                TextMesh label = part.GetComponent<TextMesh>();
                if (label != null)
                {
                    label.color = Color.white;
                    MeshRenderer labelRenderer = label.GetComponent<MeshRenderer>();
                    if (labelRenderer != null)
                    {
                        labelRenderer.enabled = true;
                        labelRenderer.forceRenderingOff = false;
                    }
                    // TextMesh's front face points along +Z; aim it at the front
                    // of the cabinet for the project's Cull Back text material.
                    Vector3 front = -transform.forward;
                    if (front.sqrMagnitude > 0.001f)
                        label.transform.rotation = Quaternion.LookRotation(front, transform.up);
                }
            }
        }

        private void RefreshVisualState()
        {
            if (statusLight != null)
            {
                Color color = gameFlow != null && gameFlow.stationPowerConnected
                    ? new Color(0.15f, 0.95f, 0.28f)
                    : new Color(0.9f, 0.12f, 0.04f);
                foreach (Material material in statusLight.materials)
                {
                    if (material == null) continue;
                    if (material.HasProperty("_Color")) material.color = color;
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                    if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 0.45f);
                }
            }

            Transform breaker = transform.Find("LlaveTermica");
            if (breaker != null)
                breaker.localRotation = Quaternion.Euler(0f, 0f,
                    gameFlow != null && gameFlow.stationPowerConnected ? -24f : 24f);
        }

        private void RefreshAfterPowerLoss()
        {
            if (cableVisual != null) cableVisual.SetConnected(false);
            RefreshVisualState();
        }
    }
}
