using UnityEngine;
using UnityEngine.UI;

namespace EnElCamino
{
    public class DayTimer : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Light sun;
        [SerializeField] private Text timerText;
        [SerializeField] private float duration = 120f;
        private float timeLeft;
        private float startTime;

        private void Start()
        {
            timeLeft = duration;
            startTime = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            if (gameFlow.finished || gameFlow.gameOver) return;

            timeLeft = duration - (Time.realtimeSinceStartup - startTime);
            float progress = 1f - timeLeft / duration;
            sun.intensity = Mathf.Lerp(1.1f, 0.05f, progress);
            RenderSettings.ambientLight = Color.Lerp(new Color(0.55f, 0.55f, 0.55f), new Color(0.02f, 0.02f, 0.06f), progress);

            int seconds = Mathf.CeilToInt(timeLeft);
            timerText.text = "TIEMPO RESTANTE\n" + seconds / 60 + ":" + (seconds % 60).ToString("00");

            if (timeLeft <= 0f) gameFlow.GameOver();
        }
    }
}
