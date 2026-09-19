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

        private void Awake()
        {
            HidePrompt();
            completedPanel.SetActive(false);
            messageText.gameObject.SetActive(false);
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

        public void ShowCompleted()
        {
            finalText.text = "¡LLEGASTE A LA RUTA!\nVICTORIA";
            completedPanel.SetActive(true);
            HidePrompt();
            messageText.gameObject.SetActive(false);
        }

        public void ShowGameOver()
        {
            finalText.text = "SE HIZO DE NOCHE\nGAME OVER";
            completedPanel.SetActive(true);
            HidePrompt();
            messageText.gameObject.SetActive(false);
        }
    }
}
