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

        private void Awake()
        {
            HidePrompt();
            completedPanel.SetActive(false);
            messageText.gameObject.SetActive(false);
            CreateGeneratorFuelPanel();
        }

        public void SetObjective(string text)
        {
            objectiveText.text = text;
        }

        public void ShowPrompt(string text)
        {
            promptText.text = text;
            promptText.gameObject.SetActive(true);
        }

        public void HidePrompt()
        {
            promptText.gameObject.SetActive(false);
        }

        public void ShowMessage(string text)
        {
            messageText.text = text;
            messageText.gameObject.SetActive(true);
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
            generatorFuelPanel.SetActive(true);
        }

        public void HideGeneratorFuelTime()
        {
            if (generatorFuelPanel != null) generatorFuelPanel.SetActive(false);
            displayedGeneratorFuelSeconds = -1;
        }

        public void ShowCompleted()
        {
            finalText.text = "¡LLEGASTE A LA RUTA!\nVICTORIA";
            completedPanel.SetActive(true);
            HideGeneratorFuelTime();
            HidePrompt();
            messageText.gameObject.SetActive(false);
        }

        public void ShowGameOver()
        {
            finalText.text = "GAME OVER";
            completedPanel.SetActive(true);
            HideGeneratorFuelTime();
            HidePrompt();
            messageText.gameObject.SetActive(false);
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
            panelRect.anchoredPosition = new Vector2(24f, -132f);
            panelRect.sizeDelta = new Vector2(270f, 64f);

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
            generatorFuelText.fontSize = 21;
            generatorFuelText.fontStyle = FontStyle.Bold;
            generatorFuelText.alignment = TextAnchor.MiddleLeft;
            generatorFuelText.color = new Color(1f, 0.78f, 0.16f, 1f);
            generatorFuelText.raycastTarget = false;

            generatorFuelPanel = panel;
            generatorFuelPanel.SetActive(false);
        }
    }
}
