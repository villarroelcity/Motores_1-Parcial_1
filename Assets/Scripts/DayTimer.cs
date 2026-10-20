using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace EnElCamino
{
    public class DayTimer : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Light sun;
        [SerializeField] private Text timerText;
        [SerializeField] private float duration = 120f;
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private bool controlLegacyLighting = true;
        [SerializeField] private float enviroStartHour = 17.25f;
        [SerializeField] private float enviroNightHour = 21.5f;
        private float timeLeft;
        private float startTime;
        private float lastSkyProgress = -1f;
        private Light enviroSun;
        private object enviroTime;
        private MethodInfo setEnviroTime;

        private void Start()
        {
            timeLeft = duration;
            startTime = Time.realtimeSinceStartup;
            if (skyboxMaterial == null) skyboxMaterial = RenderSettings.skybox;
            if (timerText == null) timerText = GetComponentInChildren<Text>(true);
            foreach (Light candidate in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name.Contains("Sun/Moon")) { enviroSun = candidate; break; }
            }
            EnsureEnviroClock();
        }

        private void Update()
        {
            if (gameFlow == null || gameFlow.finished || gameFlow.gameOver) return;

            timeLeft = duration - (Time.realtimeSinceStartup - startTime);
            float progress = Mathf.Clamp01(1f - timeLeft / Mathf.Max(1f, duration));
            if (!controlLegacyLighting && enviroTime == null) EnsureEnviroClock();
            if (!controlLegacyLighting && enviroTime != null && setEnviroTime != null)
            {
                float hour = Mathf.Lerp(enviroStartHour, enviroNightHour, progress);
                setEnviroTime.Invoke(enviroTime, new object[] { hour });
            }
            if (controlLegacyLighting && sun != null)
                sun.intensity = Mathf.Lerp(1.1f, 0.05f, progress);
            if (!controlLegacyLighting && enviroSun != null)
            {
                enviroSun.intensity = Mathf.Lerp(0.9f, 0.03f, Mathf.SmoothStep(0.3f, 1f, progress));
                RenderSettings.ambientIntensity = Mathf.Lerp(1f, 0.08f, Mathf.SmoothStep(0.3f, 1f, progress));
            }

            if (controlLegacyLighting)
                RenderSettings.ambientLight = Color.Lerp(new Color(0.55f, 0.55f, 0.55f), new Color(0.02f, 0.02f, 0.06f), progress);

            if (controlLegacyLighting && skyboxMaterial != null && skyboxMaterial.HasProperty("_SkyTint"))
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
            if (timerText != null) timerText.text = "TIEMPO RESTANTE\n" + seconds / 60 + ":" + (seconds % 60).ToString("00");

            if (timeLeft <= 0f) gameFlow.GameOver();
        }

        private void EnsureEnviroClock()
        {
            if (enviroTime == null)
            {
                foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (behaviour == null || behaviour.GetType().Name != "EnviroManager") continue;
                    PropertyInfo timeProperty = behaviour.GetType().GetProperty("Time", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    enviroTime = timeProperty != null ? timeProperty.GetValue(behaviour) : null;
                    if (enviroTime == null)
                    {
                        FieldInfo timeField = behaviour.GetType().GetField("Time", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        enviroTime = timeField != null ? timeField.GetValue(behaviour) : null;
                    }
                    if (enviroTime != null) setEnviroTime = enviroTime.GetType().GetMethod("SetTimeOfDay", BindingFlags.Instance | BindingFlags.Public);
                    break;
                }
            }

            // Enviro's manager may initialize after this component's Start; set the
            // afternoon when its clock becomes available, then Update drives dusk.
            if (!controlLegacyLighting && enviroTime != null && setEnviroTime != null)
                setEnviroTime.Invoke(enviroTime, new object[] { enviroStartHour });
        }

    }
}
