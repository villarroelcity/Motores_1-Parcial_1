using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace EnElCamino
{
    [ExecuteAlways]
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text promptText;
        [SerializeField] private Text messageText;
        [SerializeField] private GameObject completedPanel;
        [SerializeField] private Text finalText;

        private GameObject generatorFuelPanel;
        private Text generatorFuelText;
        private GameObject vehicleSpeedPanel;
        private Text vehicleSpeedText;
        private int displayedVehicleSpeed = -1;
        private GameObject pursuitDistancePanel;
        private Text pursuitDistanceText;
        private GameObject rearViewPanel;
        private RawImage rearViewImage;
        private RenderTexture rearViewTexture;
        private Camera rearViewCamera;
        private CarDriver rearViewTarget;
        private int displayedGeneratorFuelSeconds = -1;
        private CanvasScaler canvasScaler;
        private RectTransform clockPanel;
        private Text clockText;
        private RectTransform objectivePanel;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private CanvasGroup gameplayVisibility;
        private bool gameplayVisible;
        private bool countdownVisible = true;
        private float messageExpiresAt;
        private GameFlow gameFlow;
        private GameObject pausePanel;
        private GameObject controlsPanel;
        private Text pauseText;
        private Text controlsText;
        private bool paused;
        private GameObject outcomeRestartButton;
        private Vector2 rearViewPanelPosition = new Vector2(0f, -6f);
        private Vector2 pursuitDistancePanelPosition = new Vector2(0f, -655f);

        public void SetGameplayVisible(bool visible)
        {
            gameplayVisible = visible;
            if (gameplayVisibility == null)
            {
                gameplayVisibility = GetComponent<CanvasGroup>();
                if (gameplayVisibility == null) gameplayVisibility = gameObject.AddComponent<CanvasGroup>();
            }
            gameplayVisibility.alpha = visible ? 1f : 0f;
            gameplayVisibility.interactable = visible;
            gameplayVisibility.blocksRaycasts = visible;
            if (!visible && messageText != null) messageText.gameObject.SetActive(false);
            if (!visible) HidePursuitDistance();
            if (visible) ShowCountdown();
        }

        private void Awake()
        {
            // These two objects were only editor helpers for positioning. They
            // are not part of the runtime HUD and must not remain in the scene.
            RemoveEditorPreviewPanels();

            if (!Application.isPlaying)
            {
                return;
            }

            EnsureUiEventSystem();
            canvasScaler = GetComponent<CanvasScaler>();
            gameFlow = FindFirstObjectByType<GameFlow>();
            clockPanel = FindRectTransform("RelojPanel");
            clockText = FindText("RelojTexto");
            objectivePanel = FindRectTransform("PanelObjetivo");
            CreatePauseMenu();
            CreateOutcomePanel();
            CreateOutcomeRestartButton();
            HidePrompt();
            if (completedPanel != null) completedPanel.SetActive(false);
            if (messageText != null) messageText.gameObject.SetActive(false);
            CreateGeneratorFuelPanel();
            CreateVehicleSpeedPanel();
            CreatePursuitDistancePanel();
            CreateRearViewMirror();
            ApplyResponsiveLayout();
            SetGameplayVisible(false);
        }

        private static void EnsureUiEventSystem()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null) inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.enabled = true;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (controlsPanel != null && controlsPanel.activeSelf)
                {
                    controlsPanel.SetActive(false);
                    return;
                }
                TogglePause();
            }

            if (Keyboard.current.rKey.wasPressedThisFrame &&
                (paused || completedPanel != null && completedPanel.activeSelf))
                RestartGame();
        }

        private void TogglePause()
        {
            if (!gameplayVisible || pausePanel == null) return;
            if (gameFlow != null && (gameFlow.gameOver || gameFlow.finished)) return;

            paused = !paused;
            pausePanel.SetActive(paused);
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
        }

        private void ResumeGame()
        {
            if (!paused) return;
            paused = false;
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void RestartGame()
        {
            paused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ShowControls()
        {
            if (controlsPanel == null) return;
            controlsPanel.SetActive(true);
            if (controlsText != null)
                controlsText.text = "CONTROLES\n\nWASD / FLECHAS: mover\nSHIFT: correr\nE: interactuar\nFLECHAS: conducir el auto\nESPACIO: frenar\nESC: pausa / volver";
        }

        private void CreatePauseMenu()
        {
            pausePanel = CreateUiObject("MenuPausa", transform, typeof(Image));
            RectTransform pauseRect = pausePanel.GetComponent<RectTransform>();
            pauseRect.anchorMin = Vector2.zero;
            pauseRect.anchorMax = Vector2.one;
            pauseRect.offsetMin = Vector2.zero;
            pauseRect.offsetMax = Vector2.zero;
            Image overlay = pausePanel.GetComponent<Image>();
            overlay.color = new Color(0.015f, 0.02f, 0.03f, 0.92f);
            overlay.raycastTarget = true;

            pauseText = CreateUiText("PauseText", pausePanel.transform, "PAUSA\n\nESC: continuar\nR: reiniciar partida", 32, TextAnchor.MiddleCenter);
            SetCenteredRect(pauseText.rectTransform, new Vector2(420f, 210f), new Vector2(0f, 60f));

            CreateMenuButton("ContinuarButton", "CONTINUAR", pausePanel.transform, new Vector2(0f, -45f), ResumeGame);
            CreateMenuButton("RestartButton", "REINICIAR", pausePanel.transform, new Vector2(0f, -105f), RestartGame);
            CreateMenuButton("ControlsButton", "CONTROLES", pausePanel.transform, new Vector2(0f, -165f), ShowControls);

            controlsPanel = CreateUiObject("ControlesPanel", pausePanel.transform, typeof(Image));
            RectTransform controlsRect = controlsPanel.GetComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(0.5f, 0.5f);
            controlsRect.anchorMax = new Vector2(0.5f, 0.5f);
            controlsRect.pivot = new Vector2(0.5f, 0.5f);
            controlsRect.anchoredPosition = Vector2.zero;
            controlsRect.sizeDelta = new Vector2(500f, 350f);
            controlsPanel.GetComponent<Image>().color = new Color(0.06f, 0.075f, 0.1f, 0.98f);

            controlsText = CreateUiText("ControlesText", controlsPanel.transform, "", 24, TextAnchor.MiddleCenter);
            controlsText.rectTransform.anchorMin = Vector2.zero;
            controlsText.rectTransform.anchorMax = Vector2.one;
            controlsText.rectTransform.offsetMin = new Vector2(24f, 60f);
            controlsText.rectTransform.offsetMax = new Vector2(-24f, -24f);
            CreateMenuButton("BackControlsButton", "VOLVER", controlsPanel.transform, new Vector2(0f, -140f), () => controlsPanel.SetActive(false));

            controlsPanel.SetActive(false);
            pausePanel.SetActive(false);
        }

        private GameObject CreateUiObject(string objectName, Transform parent, params System.Type[] components)
        {
            GameObject created = new GameObject(objectName, components);
            created.transform.SetParent(parent, false);
            return created;
        }

        private Text CreateUiText(string objectName, Transform parent, string value, int size, TextAnchor alignment)
        {
            GameObject textObject = CreateUiObject(objectName, parent, typeof(Text));
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = size;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private GameObject CreateMenuButton(string objectName, string label, Transform parent, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = CreateUiObject(objectName, parent, typeof(Image), typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(230f, 44f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.22f, 0.28f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            Text buttonText = CreateUiText(label + "Text", buttonObject.transform, label, 20, TextAnchor.MiddleCenter);
            buttonText.rectTransform.anchorMin = Vector2.zero;
            buttonText.rectTransform.anchorMax = Vector2.one;
            buttonText.rectTransform.offsetMin = Vector2.zero;
            buttonText.rectTransform.offsetMax = Vector2.zero;
            return buttonObject;
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (messageText != null && messageText.gameObject.activeSelf && Time.time >= messageExpiresAt)
                messageText.gameObject.SetActive(false);
            UpdateRearViewCamera();
            if (lastScreenWidth == Screen.width && lastScreenHeight == Screen.height) return;
            ApplyResponsiveLayout();
        }

        public void SetObjective(string text)
        {
            if (objectiveText == null) return;
            objectiveText.text = text;
            objectiveText.enabled = true;
            if (!objectiveText.gameObject.activeSelf) objectiveText.gameObject.SetActive(true);
        }

        public void HideCountdown()
        {
            countdownVisible = false;
            if (clockPanel != null) clockPanel.gameObject.SetActive(false);
            else if (clockText != null) clockText.gameObject.SetActive(false);
        }

        public void ShowCountdown()
        {
            countdownVisible = true;
            if (clockPanel != null) clockPanel.gameObject.SetActive(true);
            if (clockText != null)
            {
                clockText.enabled = true;
                clockText.gameObject.SetActive(true);
            }
        }

        public void ShowPrompt(string text)
        {
            if (!gameplayVisible) return;
            if (promptText == null) return;
            promptText.text = text;
            promptText.gameObject.SetActive(true);
        }

        public void HidePrompt()
        {
            if (promptText != null) promptText.gameObject.SetActive(false);
        }

        public void ShowMessage(string text)
        {
            if (!gameplayVisible) return;
            if (messageText == null) return;
            messageText.text = text;
            messageText.gameObject.SetActive(true);
            messageExpiresAt = Time.time + 4.5f;
        }

        public void SetGeneratorFuelTime(float secondsRemaining)
        {
            if (generatorFuelPanel == null) CreateGeneratorFuelPanel();
            int seconds = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
            if (seconds != displayedGeneratorFuelSeconds)
            {
                generatorFuelText.text = $"COMBUSTIBLE GENERADOR\n{seconds / 60}:{seconds % 60:00}";
                displayedGeneratorFuelSeconds = seconds;
            }
            generatorFuelText.enabled = true;
            if (!generatorFuelText.gameObject.activeSelf) generatorFuelText.gameObject.SetActive(true);
            generatorFuelPanel.SetActive(true);
        }

        public void HideGeneratorFuelTime()
        {
            if (generatorFuelPanel != null) generatorFuelPanel.SetActive(false);
            displayedGeneratorFuelSeconds = -1;
        }

        public void SetVehicleSpeed(float speedKmh)
        {
            if (!gameplayVisible) return;
            if (vehicleSpeedPanel == null) CreateVehicleSpeedPanel();

            int speed = Mathf.Max(0, Mathf.RoundToInt(speedKmh));
            if (speed != displayedVehicleSpeed)
            {
                vehicleSpeedText.text = $"VELOCIDAD\n{speed} km/h";
                displayedVehicleSpeed = speed;
            }
            vehicleSpeedPanel.SetActive(true);
        }

        public void HideVehicleSpeed()
        {
            if (vehicleSpeedPanel != null) vehicleSpeedPanel.SetActive(false);
            displayedVehicleSpeed = -1;
        }

        public void SetPursuitDistance(float currentDistance, float requiredDistance)
        {
            EnableGameplayOverlay();
            if (pursuitDistancePanel == null) CreatePursuitDistancePanel();
            if (rearViewPanel == null) CreateRearViewMirror();
            if (pursuitDistanceText == null || rearViewPanel == null) return;

            int current = Mathf.Max(0, Mathf.FloorToInt(currentDistance));
            int required = Mathf.Max(1, Mathf.CeilToInt(requiredDistance));
            pursuitDistanceText.text = $"VENTAJA\n{current} / {required} m";
            pursuitDistancePanel.SetActive(true);
            rearViewPanel.SetActive(true);
            if (rearViewCamera != null) rearViewCamera.enabled = true;
        }

        public void HidePursuitDistance()
        {
            if (pursuitDistancePanel != null) pursuitDistancePanel.SetActive(false);
            if (rearViewPanel != null) rearViewPanel.SetActive(false);
            if (rearViewCamera != null) rearViewCamera.enabled = false;
        }

        public void ShowCompleted()
        {
            CreateOutcomePanel();
            if (finalText != null) finalText.text = "LLEGASTE A LA RUTA\nVICTORIA";
            if (completedPanel != null) completedPanel.SetActive(true);
            if (outcomeRestartButton != null) outcomeRestartButton.SetActive(false);
            HideGeneratorFuelTime();
            HideVehicleSpeed();
            HidePursuitDistance();
            HidePrompt();
            if (messageText != null) messageText.gameObject.SetActive(false);
        }

        public void ShowGameOver()
        {
            CreateOutcomePanel();
            if (finalText != null) finalText.text = "GAME OVER";
            PrepareOutcomeForInput();
            if (completedPanel != null) completedPanel.SetActive(true);
            if (outcomeRestartButton != null)
            {
                outcomeRestartButton.SetActive(true);
                Button button = outcomeRestartButton.GetComponent<Button>();
                if (button != null) button.interactable = true;
            }
            HideGeneratorFuelTime();
            HideVehicleSpeed();
            HidePursuitDistance();
            HidePrompt();
            if (messageText != null) messageText.gameObject.SetActive(false);
        }

        private void CreateOutcomeRestartButton()
        {
            CreateOutcomePanel();
            if (completedPanel == null || outcomeRestartButton != null) return;

            outcomeRestartButton = CreateMenuButton(
                "OutcomeRestartButton",
                "REINICIAR",
                completedPanel.transform,
                new Vector2(0f, -78f),
                RestartGame);
            outcomeRestartButton.SetActive(false);
        }

        private void CreateOutcomePanel()
        {
            if (completedPanel == null)
            {
                completedPanel = CreateUiObject("PanelResultadoRuntime", transform, typeof(Image));
                SetCenteredRect(completedPanel.GetComponent<RectTransform>(), new Vector2(650f, 220f), Vector2.zero);

                Image panelImage = completedPanel.GetComponent<Image>();
                panelImage.color = new Color(0.04f, 0.06f, 0.05f, 0.92f);
                panelImage.raycastTarget = true;
            }

            if (finalText == null)
            {
                finalText = completedPanel.transform.Find("ResultadoFinalText")?.GetComponent<Text>();
                if (finalText == null)
                {
                    finalText = CreateUiText("ResultadoFinalText", completedPanel.transform, "", 36, TextAnchor.MiddleCenter);
                    finalText.rectTransform.anchorMin = Vector2.zero;
                    finalText.rectTransform.anchorMax = Vector2.one;
                    finalText.rectTransform.offsetMin = new Vector2(24f, 38f);
                    finalText.rectTransform.offsetMax = new Vector2(-24f, -24f);
                }
            }
        }

        private void EnableGameplayOverlay()
        {
            gameplayVisible = true;
            if (gameplayVisibility == null)
            {
                gameplayVisibility = GetComponent<CanvasGroup>();
                if (gameplayVisibility == null) gameplayVisibility = gameObject.AddComponent<CanvasGroup>();
            }

            gameplayVisibility.alpha = 1f;
            gameplayVisibility.interactable = true;
            gameplayVisibility.blocksRaycasts = true;
        }

        private void RemoveEditorPreviewPanels()
        {
            RemoveEditorPreviewPanel("RearViewMirrorPanel");
            RemoveEditorPreviewPanel("PursuitDistancePanel");
        }

        private void RemoveEditorPreviewPanel(string objectName)
        {
            Transform preview = transform.Find(objectName);
            if (preview == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(preview.gameObject);
            else
                Destroy(preview.gameObject);
#else
            Destroy(preview.gameObject);
#endif
        }

        private void PrepareOutcomeForInput()
        {
            CreateOutcomePanel();
            if (gameplayVisibility == null)
                gameplayVisibility = GetComponent<CanvasGroup>();

            if (gameplayVisibility != null)
            {
                gameplayVisibility.alpha = 1f;
                gameplayVisibility.interactable = true;
                gameplayVisibility.blocksRaycasts = true;
            }

            if (completedPanel != null)
            {
                Image panelImage = completedPanel.GetComponent<Image>();
                if (panelImage != null) panelImage.raycastTarget = true;
            }
        }

        private void CreateGeneratorFuelPanel()
        {
            if (generatorFuelPanel != null) return;

            GameObject panel = new GameObject("GeneratorFuelPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            panel.transform.SetParent(transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -166f);
            panelRect.sizeDelta = new Vector2(270f, 92f);

            RawImage background = panel.GetComponent<RawImage>();
            background.texture = Texture2D.whiteTexture;
            background.color = new Color(0.035f, 0.045f, 0.06f, 0.82f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("GeneratorFuelText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 5f);
            textRect.offsetMax = new Vector2(-8f, -5f);

            generatorFuelText = textObject.GetComponent<Text>();
            Text clockText = GameObject.Find("RelojTexto")?.GetComponent<Text>();
            generatorFuelText.font = clockText != null ? clockText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            generatorFuelText.fontSize = 20;
            generatorFuelText.resizeTextForBestFit = true;
            generatorFuelText.resizeTextMinSize = 16;
            generatorFuelText.resizeTextMaxSize = 22;
            generatorFuelText.fontStyle = FontStyle.Bold;
            generatorFuelText.alignment = TextAnchor.MiddleLeft;
            generatorFuelText.color = new Color(1f, 0.78f, 0.16f, 1f);
            generatorFuelText.raycastTarget = false;
            generatorFuelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            generatorFuelText.verticalOverflow = VerticalWrapMode.Overflow;

            generatorFuelPanel = panel;
            generatorFuelPanel.SetActive(false);
        }

        private void CreateVehicleSpeedPanel()
        {
            if (vehicleSpeedPanel != null) return;

            GameObject panel = new GameObject("VehicleSpeedPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            panel.transform.SetParent(transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -270f);
            panelRect.sizeDelta = new Vector2(270f, 92f);

            RawImage background = panel.GetComponent<RawImage>();
            background.texture = Texture2D.whiteTexture;
            background.color = new Color(0.035f, 0.045f, 0.06f, 0.82f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("VehicleSpeedText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 5f);
            textRect.offsetMax = new Vector2(-8f, -5f);

            vehicleSpeedText = textObject.GetComponent<Text>();
            Text referenceText = GameObject.Find("RelojTexto")?.GetComponent<Text>();
            vehicleSpeedText.font = referenceText != null ? referenceText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            vehicleSpeedText.fontSize = 20;
            vehicleSpeedText.resizeTextForBestFit = true;
            vehicleSpeedText.resizeTextMinSize = 16;
            vehicleSpeedText.resizeTextMaxSize = 22;
            vehicleSpeedText.fontStyle = FontStyle.Bold;
            vehicleSpeedText.alignment = TextAnchor.MiddleLeft;
            vehicleSpeedText.color = Color.white;
            vehicleSpeedText.raycastTarget = false;
            vehicleSpeedText.horizontalOverflow = HorizontalWrapMode.Wrap;
            vehicleSpeedText.verticalOverflow = VerticalWrapMode.Overflow;

            vehicleSpeedPanel = panel;
            vehicleSpeedPanel.SetActive(false);
        }

        private void CreatePursuitDistancePanel()
        {
            if (pursuitDistancePanel != null) return;

            GameObject panel = new GameObject("PursuitDistancePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            panel.transform.SetParent(transform, false);
            RawImage background = panel.GetComponent<RawImage>();
            background.texture = Texture2D.whiteTexture;
            background.color = new Color(0.035f, 0.045f, 0.06f, 0.88f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("PursuitDistanceText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 5f);
            textRect.offsetMax = new Vector2(-10f, -5f);
            pursuitDistanceText = textObject.GetComponent<Text>();
            pursuitDistanceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pursuitDistanceText.fontSize = 20;
            pursuitDistanceText.resizeTextForBestFit = true;
            pursuitDistanceText.resizeTextMinSize = 15;
            pursuitDistanceText.resizeTextMaxSize = 22;
            pursuitDistanceText.fontStyle = FontStyle.Bold;
            pursuitDistanceText.alignment = TextAnchor.MiddleCenter;
            pursuitDistanceText.color = new Color(0.45f, 0.78f, 1f, 1f);
            pursuitDistanceText.raycastTarget = false;

            pursuitDistancePanel = panel;
            pursuitDistancePanel.SetActive(false);
        }

        private void CreateRearViewMirror()
        {
            if (rearViewPanel != null) return;

            GameObject panel = new GameObject("RearViewMirrorPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            panel.transform.SetParent(transform, false);
            RawImage background = panel.GetComponent<RawImage>();
            background.texture = Texture2D.whiteTexture;
            background.color = Color.clear;
            background.raycastTarget = false;

            GameObject imageObject = new GameObject("RearViewMirrorImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imageObject.transform.SetParent(panel.transform, false);
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            rearViewImage = imageObject.GetComponent<RawImage>();
            rearViewImage.color = Color.white;
            rearViewImage.raycastTarget = false;

            RemovePanelChild(panel.transform, "RearViewMirrorLabel");

            rearViewTexture = new RenderTexture(480, 160, 16, RenderTextureFormat.ARGB32);
            rearViewTexture.name = "RearViewMirrorTexture";
            rearViewTexture.Create();
            rearViewImage.texture = rearViewTexture;

            GameObject cameraObject = new GameObject("CamaraEspejoRetrovisor");
            rearViewCamera = cameraObject.AddComponent<Camera>();
            rearViewCamera.targetTexture = rearViewTexture;
            rearViewCamera.clearFlags = CameraClearFlags.Skybox;
            rearViewCamera.cullingMask = ~0;
            rearViewCamera.fieldOfView = 55f;
            rearViewCamera.nearClipPlane = 0.1f;
            rearViewCamera.farClipPlane = 160f;
            rearViewCamera.enabled = false;

            rearViewPanel = panel;
            rearViewPanel.SetActive(false);
        }

        private static void RemovePanelChild(Transform panel, string childName)
        {
            Transform child = panel != null ? panel.Find(childName) : null;
            if (child != null) Destroy(child.gameObject);
        }

        private void UpdateRearViewCamera()
        {
            if (rearViewCamera == null || !rearViewCamera.enabled) return;
            if (rearViewTarget == null) rearViewTarget = FindFirstObjectByType<CarDriver>();
            if (rearViewTarget == null) return;

            Vector3 forward = Vector3.ProjectOnPlane(rearViewTarget.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) return;

            rearViewCamera.transform.SetPositionAndRotation(
                rearViewTarget.transform.position - forward * 1.5f + Vector3.up * 1.45f,
                Quaternion.LookRotation(-forward, Vector3.up));
        }

        private void ApplyResponsiveLayout()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1280f, 720f);
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = 0.5f;
            }

            float widthScale = Mathf.Max(0.01f, Screen.width / 1280f);
            float heightScale = Mathf.Max(0.01f, Screen.height / 720f);
            // Match CanvasScaler's logarithmic width/height blend instead of
            // measuring its previous-frame scaleFactor during Awake.
            float scale = Mathf.Sqrt(widthScale * heightScale);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            float clockWidth = Mathf.Clamp(width * 0.23f, 250f, 330f);
            float clockHeight = Mathf.Clamp(height * 0.19f, 132f, 150f);

            if (clockPanel != null)
            {
                SetTopLeft(clockPanel, new Vector2(24f, -24f), new Vector2(clockWidth, clockHeight));
                clockPanel.gameObject.SetActive(countdownVisible);
            }
            if (clockText != null)
            {
                clockText.enabled = true;
                if (countdownVisible && !clockText.gameObject.activeSelf) clockText.gameObject.SetActive(true);
                clockText.fontSize = 30;
                clockText.resizeTextForBestFit = true;
                clockText.resizeTextMinSize = 22;
                clockText.resizeTextMaxSize = 32;
                clockText.alignment = TextAnchor.MiddleCenter;
                clockText.horizontalOverflow = HorizontalWrapMode.Wrap;
                clockText.verticalOverflow = VerticalWrapMode.Overflow;
                clockText.raycastTarget = false;
            }

            if (generatorFuelPanel != null)
            {
                RectTransform fuelRect = generatorFuelPanel.GetComponent<RectTransform>();
                SetTopLeft(fuelRect, new Vector2(24f, -24f - clockHeight - 12f), new Vector2(clockWidth, 92f));
            }
            if (vehicleSpeedPanel != null)
            {
                RectTransform speedRect = vehicleSpeedPanel.GetComponent<RectTransform>();
                SetTopLeft(speedRect, new Vector2(24f, -24f - clockHeight - 116f), new Vector2(clockWidth, 92f));
            }
            if (rearViewPanel != null)
            {
                float mirrorWidth = Mathf.Clamp(width * 0.25f, 260f, 340f);
                SetTopCenter(rearViewPanel.GetComponent<RectTransform>(), rearViewPanelPosition, new Vector2(mirrorWidth, 96f));
            }
            if (pursuitDistancePanel != null)
            {
                float mirrorWidth = Mathf.Clamp(width * 0.25f, 260f, 340f);
                SetTopCenter(pursuitDistancePanel.GetComponent<RectTransform>(), pursuitDistancePanelPosition, new Vector2(mirrorWidth, 58f));
            }
            if (objectivePanel != null)
            {
                float panelWidth = Mathf.Clamp(width * 0.40f, 310f, 520f);
                float panelHeight = Mathf.Clamp(height * 0.25f, 168f, 205f);
                RectTransform panelRect = objectivePanel;
                panelRect.anchorMin = Vector2.one;
                panelRect.anchorMax = Vector2.one;
                panelRect.pivot = Vector2.one;
                panelRect.anchoredPosition = new Vector2(-24f, -24f);
                panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
                panelRect.gameObject.SetActive(true);
            }
            if (objectiveText != null)
            {
                RectTransform objectiveRect = objectiveText.rectTransform;
                objectiveRect.anchorMin = Vector2.zero;
                objectiveRect.anchorMax = Vector2.one;
                objectiveRect.offsetMin = new Vector2(16f, 12f);
                objectiveRect.offsetMax = new Vector2(-16f, -42f);
                objectiveText.resizeTextForBestFit = true;
                objectiveText.resizeTextMinSize = 14;
                objectiveText.resizeTextMaxSize = 22;
                objectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
                objectiveText.verticalOverflow = VerticalWrapMode.Overflow;
                objectiveText.raycastTarget = false;
            }

            Text objectiveHeading = FindText("EtiquetaObjetivo");
            if (objectiveHeading != null)
            {
                RectTransform headingRect = objectiveHeading.rectTransform;
                headingRect.anchorMin = new Vector2(0f, 1f);
                headingRect.anchorMax = new Vector2(1f, 1f);
                headingRect.pivot = new Vector2(0f, 1f);
                headingRect.anchoredPosition = new Vector2(16f, -12f);
                headingRect.sizeDelta = new Vector2(-32f, 28f);
                objectiveHeading.resizeTextForBestFit = true;
                objectiveHeading.resizeTextMinSize = 16;
                objectiveHeading.resizeTextMaxSize = 22;
                objectiveHeading.raycastTarget = false;
            }

            if (generatorFuelText != null)
            {
                generatorFuelText.fontSize = 20;
                generatorFuelText.resizeTextForBestFit = true;
                generatorFuelText.resizeTextMinSize = 16;
                generatorFuelText.resizeTextMaxSize = 22;
            }
            if (vehicleSpeedText != null)
            {
                vehicleSpeedText.fontSize = 20;
                vehicleSpeedText.resizeTextForBestFit = true;
                vehicleSpeedText.resizeTextMinSize = 16;
                vehicleSpeedText.resizeTextMaxSize = 22;
            }
        }

        private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void OnDestroy()
        {
            if (rearViewTexture == null) return;
            rearViewTexture.Release();
            Destroy(rearViewTexture);
        }

        private RectTransform FindRectTransform(string objectName)
        {
            foreach (RectTransform candidate in GetComponentsInChildren<RectTransform>(true))
                if (candidate.name == objectName) return candidate;
            return null;
        }

        private Text FindText(string objectName)
        {
            foreach (Text candidate in GetComponentsInChildren<Text>(true))
                if (candidate.name == objectName) return candidate;
            return null;
        }
    }
}
