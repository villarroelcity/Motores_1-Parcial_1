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
        [SerializeField] private Material skyboxMaterial;
        private float timeLeft;
        private float startTime;
        private float lastSkyProgress = -1f;

        private void Start()
        {
            timeLeft = duration;
            startTime = Time.realtimeSinceStartup;
            if (skyboxMaterial == null) skyboxMaterial = RenderSettings.skybox;
        }

        private void Update()
        {
            if (gameFlow.finished || gameFlow.gameOver) return;

            timeLeft = duration - (Time.realtimeSinceStartup - startTime);
            float progress = 1f - timeLeft / duration;
            sun.intensity = Mathf.Lerp(1.1f, 0.05f, progress);
            RenderSettings.ambientLight = Color.Lerp(new Color(0.55f, 0.55f, 0.55f), new Color(0.02f, 0.02f, 0.06f), progress);

            if (skyboxMaterial != null && skyboxMaterial.HasProperty("_SkyTint"))
            {
                skyboxMaterial.SetColor("_SkyTint", Color.Lerp(new Color(0.48f, 0.56f, 0.72f), new Color(0.035f, 0.045f, 0.12f), progress));
                skyboxMaterial.SetColor("_GroundColor", Color.Lerp(new Color(0.32f, 0.34f, 0.38f), new Color(0.006f, 0.008f, 0.02f), progress));
                skyboxMaterial.SetFloat("_Exposure", Mathf.Lerp(1.1f, 0.28f, progress));
                if (Mathf.Abs(progress - lastSkyProgress) > 0.02f)
                {
                    DynamicGI.UpdateEnvironment();
                    lastSkyProgress = progress;
                }
            }

            int seconds = Mathf.CeilToInt(timeLeft);
            timerText.text = "TIEMPO RESTANTE\n" + seconds / 60 + ":" + (seconds % 60).ToString("00");

            if (timeLeft <= 0f) gameFlow.GameOver();
        }
    }
}
