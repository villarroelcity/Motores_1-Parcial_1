using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EnElCamino
{
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        [SerializeField] private Text objectiveText;
        [SerializeField] private Text promptText;
        [SerializeField] private Text messageText;
        [SerializeField] private GameObject completedPanel;
        private Coroutine messageRoutine;

        private void Awake()
        {
            Instance = this;
            HidePrompt();
            if (completedPanel != null) completedPanel.SetActive(false);
            if (messageText != null) messageText.gameObject.SetActive(false);
        }

        public void Configure(Text objective, Text prompt, Text message, GameObject completed)
        {
            objectiveText = objective;
            promptText = prompt;
            messageText = message;
            completedPanel = completed;
        }

        public void SetObjective(string text)
        {
            if (objectiveText != null) objectiveText.text = text;
        }

        public void ShowPrompt(string text)
        {
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
            if (messageText == null) return;
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(ShowMessageRoutine(text));
        }

        public void ShowCompleted()
        {
            if (completedPanel != null) completedPanel.SetActive(true);
            HidePrompt();
        }

        private IEnumerator ShowMessageRoutine(string text)
        {
            messageText.text = text;
            messageText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2.5f);
            messageText.gameObject.SetActive(false);
        }
    }
}
