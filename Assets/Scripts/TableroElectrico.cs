using UnityEngine;

namespace EnElCamino
{
    public class TableroElectrico : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Renderer statusLight;
        [SerializeField] private Transform generatorOutlet;
        [SerializeField] private Transform boxSocket;
        [SerializeField] private Transform wallPassThrough;
        [SerializeField] private float wallThickness = 0.4f;
        [SerializeField] private LineRenderer cable;
        [SerializeField] private Renderer plugIndicator;
        [SerializeField] private Light statusLamp;
        [SerializeField, Min(0.1f)] private float unpluggedCableLength = 1.15f;
        [SerializeField, Min(0.05f)] private float connectionDuration = 0.6f;
        [SerializeField] private bool routeThroughWall;

        private bool connected;
        private float connectionProgress;

        private void Awake()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            if (statusLight == null) statusLight = transform.Find("LuzEstado")?.GetComponent<Renderer>();
            if (gameFlow != null)
            {
                gameFlow.GeneratorFuelDepleted += RefreshAfterPowerLoss;
                gameFlow.GameLost += RefreshAfterPowerLoss;
                gameFlow.GameFinished += RefreshAfterPowerLoss;
            }
            connected = gameFlow != null && gameFlow.stationPowerConnected;
            connectionProgress = connected ? 1f : 0f;
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

        private void LateUpdate()
        {
            if (cable == null || generatorOutlet == null || boxSocket == null) return;
            Vector3 start = generatorOutlet.position;
            Vector3 towardGenerator = (start - boxSocket.position).normalized;
            float targetProgress = connected ? 1f : 0f;
            connectionProgress = Mathf.MoveTowards(connectionProgress, targetProgress,
                Time.deltaTime / Mathf.Max(0.05f, connectionDuration));
            Vector3 unpluggedEnd = boxSocket.position + towardGenerator * unpluggedCableLength;
            Vector3 end = Vector3.Lerp(unpluggedEnd, boxSocket.position,
                connectionProgress * connectionProgress * (3f - 2f * connectionProgress));

            if (routeThroughWall && wallPassThrough != null)
            {
                Vector3 wallNormal = wallPassThrough.up.normalized;
                float outsideSign = Mathf.Sign(Vector3.Dot(start - wallPassThrough.position, wallNormal));
                if (Mathf.Approximately(outsideSign, 0f)) outsideSign = 1f;
                Vector3 outsidePoint = wallPassThrough.position + wallNormal * outsideSign * (wallThickness * 0.5f + 0.035f);
                Vector3 insideDirection = -wallNormal * outsideSign;
                Vector3 insidePoint = wallPassThrough.position + insideDirection * (wallThickness * 0.5f + 0.035f);
                Vector3 unpluggedWallEnd = boxSocket.position + insideDirection * unpluggedCableLength;
                end = Vector3.Lerp(unpluggedWallEnd, boxSocket.position,
                    connectionProgress * connectionProgress * (3f - 2f * connectionProgress));
                if (cable.positionCount != 5) cable.positionCount = 5;
                Vector3 outsideSag = Vector3.down * Mathf.Min(0.08f, Vector3.Distance(start, outsidePoint) * 0.02f);
                cable.SetPosition(0, start);
                cable.SetPosition(1, Vector3.Lerp(start, outsidePoint, 0.55f) + outsideSag);
                cable.SetPosition(2, outsidePoint);
                cable.SetPosition(3, insidePoint);
                cable.SetPosition(4, end);
            }
            else
            {
                if (cable.positionCount != 4) cable.positionCount = 4;
                Vector3 sag = Vector3.down * Mathf.Min(0.3f, Vector3.Distance(start, end) * 0.08f);
                cable.SetPosition(0, start);
                cable.SetPosition(1, Vector3.Lerp(start, end, 0.33f) + sag);
                cable.SetPosition(2, Vector3.Lerp(start, end, 0.67f) + sag);
                cable.SetPosition(3, end);
            }

            if (plugIndicator != null) plugIndicator.transform.position = end;
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
            SetConnected(true);
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
            // El agotamiento del generador corta la electricidad, pero no
            // desenchufa físicamente el cable: debe seguir conectado a la caja.
            RefreshVisualState();
        }

        public void SetConnected(bool value)
        {
            connected = value;
            Color color = value ? new Color(0.15f, 0.95f, 0.28f) : new Color(0.9f, 0.12f, 0.04f);
            if (plugIndicator != null)
            {
                foreach (Material material in plugIndicator.materials)
                {
                    if (material == null) continue;
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                    if (material.HasProperty("_Color")) material.color = color;
                    if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", value ? color * 1.5f : Color.black);
                }
            }
            if (statusLamp != null)
            {
                statusLamp.color = color;
                statusLamp.enabled = value;
            }
        }
    }
}
