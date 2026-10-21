using UnityEngine;

namespace EnElCamino
{
    public class TableroElectrico : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Renderer statusLight;
        [SerializeField] private Light statusLamp;

        private void Awake()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            if (statusLight == null) statusLight = transform.Find("LuzEstado")?.GetComponent<Renderer>();
            if (statusLamp == null) statusLamp = transform.Find("LuzTablero")?.GetComponent<Light>();
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
            ? "Primero encende el generador"
            : gameFlow.stationPowerConnected ? "Generador conectado: estacion con energia" : "[E] Conectar generador a la caja electrica";

        public void Interact()
        {
            if (gameFlow == null || gameFlow.gameOver || gameFlow.finished) return;
            if (!gameFlow.generatorOn)
            {
                gameFlow.ShowMessage("Primero encende el generador del taller.");
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

        private void RestorePlayModeControls()
        {
            string[] visibleParts =
            {
                "TableroFrontal", "InterruptorGeneral", "LlaveTermica", "LuzEstado", "RotuloCaja", "RotuloToma"
            };

            foreach (string partName in visibleParts)
            {
                Transform part = FindPart(partName);
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
                    if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 1.8f);
                }
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

        private Transform FindPart(string expectedName)
        {
            string normalizedExpected = RemoveAccents(expectedName);
            foreach (Transform part in GetComponentsInChildren<Transform>(true))
                if (RemoveAccents(part.name) == normalizedExpected) return part;
            return null;
        }

        private static string RemoveAccents(string value)
        {
            return value.ToLowerInvariant()
                .Replace("\u00e1", "a")
                .Replace("\u00e9", "e")
                .Replace("\u00ed", "i")
                .Replace("\u00f3", "o")
                .Replace("\u00fa", "u")
                .Replace("\u00f1", "n");
        }

    }
}
