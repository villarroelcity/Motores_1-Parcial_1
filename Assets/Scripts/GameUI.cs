using UnityEngine;
using UnityEngine.UI;

namespace EnElCamino
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text promptText;
        [SerializeField] private Text messageText;
        [SerializeField] private GameObject completedPanel;
        [SerializeField] private Text finalText;

        private GameObject generatorFuelPanel;
        private Text generatorFuelText;
        private int displayedGeneratorFuelSeconds = -1;
        private CanvasScaler canvasScaler;
        private RectTransform clockPanel;
        private Text clockText;
        private RectTransform objectivePanel;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private CanvasGroup gameplayVisibility;
        private bool gameplayVisible;
        private float messageExpiresAt;

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
        }

        private void Awake()
        {
            canvasScaler = GetComponent<CanvasScaler>();
            clockPanel = FindRectTransform("RelojPanel");
            clockText = FindText("RelojTexto");
            objectivePanel = FindRectTransform("PanelObjetivo");
            HidePrompt();
            if (completedPanel != null) completedPanel.SetActive(false);
            if (messageText != null) messageText.gameObject.SetActive(false);
            CreateGeneratorFuelPanel();
            ApplyResponsiveLayout();
            SetGameplayVisible(false);
        }

        private void LateUpdate()
        {
            if (messageText != null && messageText.gameObject.activeSelf && Time.time >= messageExpiresAt)
                messageText.gameObject.SetActive(false);
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

        public void ShowCompleted()
        {
            if (finalText != null) finalText.text = "¡LLEGASTE A LA RUTA!\nVICTORIA";
            if (completedPanel != null) completedPanel.SetActive(true);
            HideGeneratorFuelTime();
            HidePrompt();
            if (messageText != null) messageText.gameObject.SetActive(false);
        }

        public void ShowGameOver()
        {
            if (finalText != null) finalText.text = "GAME OVER";
            if (completedPanel != null) completedPanel.SetActive(true);
            HideGeneratorFuelTime();
            HidePrompt();
            if (messageText != null) messageText.gameObject.SetActive(false);
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
            generatorFuelText.font = clockText != null ? clockText.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
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
                clockPanel.gameObject.SetActive(true);
            }
            if (clockText != null)
            {
                clockText.enabled = true;
                if (!clockText.gameObject.activeSelf) clockText.gameObject.SetActive(true);
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
