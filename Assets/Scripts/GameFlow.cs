using System;
using UnityEngine;
using UnityEngine.UI;
using Enviro;

namespace EnElCamino
{
    [DefaultExecutionOrder(-200)]
    public class GameFlow : MonoBehaviour
    {
        [SerializeField] private GameUI gameUI;
        [SerializeField] private AudioSource gameOverSource;
        [SerializeField] private AudioSource victorySource;
        [SerializeField] private float generatorFuelSeconds = 60f;
        [SerializeField] private Transform streetLightsRoot;
        [SerializeField] private Light sun;
        [SerializeField] private Text timerText;
        [SerializeField] private float duration = 180f;
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private bool controlLegacyLighting;
        [SerializeField] private float enviroStartHour = 17.25f;
        [SerializeField] private float enviroNightHour = 23f;

        public event Action GeneratorFuelDepleted;
        public event Action GarageDoorMotionStarted;
        public event Action DrivingStarted;
        public event Action GameFinished;
        public event Action GameLost;

        public GameUI UI => gameUI;

        public bool generatorOn;
        public bool stationPowerConnected;
        public bool hasFuelCan;
        public bool fuelCanFull;
        public bool carHasFuel;
        public bool insideGarage;
        public bool garageOpen;
        public bool driving;
        public bool fueling;
        public bool movementLocked;
        public bool gameOver;
        public bool finished;
        private float generatorFuelRemaining;
        private float timeLeft;
        private float startTime;
        private float lastSkyProgress = -1f;
        private bool legacySunWasEnabled;
        private bool legacySunSuppressed;
        private EnviroTimeModule enviroTime;
        private EnviroTimeModule originalEnviroTime;
        private EnviroManager enviroManager;
        private bool countdownStarted;

        private void Start()
        {
            AudioListener.pause = false;
            if (gameUI == null) gameUI = FindFirstObjectByType<GameUI>();
            if (gameOverSource == null) gameOverSource = FindSource("GameOverSource");
            if (victorySource == null) victorySource = FindSource("VictorySource");
            stationPowerConnected = false;
            SetStreetLights(false);
            timeLeft = duration;
            startTime = Time.realtimeSinceStartup;
            if (skyboxMaterial == null) skyboxMaterial = RenderSettings.skybox;
            if (timerText == null) timerText = FindFirstObjectByType<Text>(FindObjectsInactive.Include);
            EnsureEnviroClock();
            if (timerText != null)
            {
                timerText.enabled = true;
                if (!timerText.gameObject.activeSelf) timerText.gameObject.SetActive(true);
            }
            if (gameUI != null)
            {
                gameUI.SetObjective("OBJETIVO 1/8\nSUBÍ AL TECHO Y BAJÁ POR EL HUECO\nAcercate al borde, colgate y descendé lentamente al taller.");
                gameUI.ShowMessage("El generador está dentro del taller. Primero tenés que entrar por el techo.");
            }
        }

        private void Update()
        {
            if (gameOver || finished) return;
            UpdateDayTimer();
            if (!generatorOn) return;
            generatorFuelRemaining = Mathf.Max(0f, generatorFuelRemaining - Time.deltaTime);
            // The HUD is intentionally hidden after filling the can, but the
            // generator keeps consuming fuel internally until it reaches zero.
            if (gameUI != null && !fuelCanFull) gameUI.SetGeneratorFuelTime(generatorFuelRemaining);
            if (generatorFuelRemaining <= 0f)
            {
                // Stop the generator state before raising the outcome. This gives every
                // dependent system (including the looped engine sound) one explicit
                // shutdown signal instead of relying on the global audio pause.
                generatorOn = false;
                stationPowerConnected = false;
                SetStreetLights(false);
                GeneratorFuelDepleted?.Invoke();
                if (!fuelCanFull)
                {
                    ShowMessage("El generador se quedó sin combustible antes de llenar el bidón.");
                    GameOver();
                }
                else
                {
                    ShowMessage("El generador se quedó sin combustible. La estación quedó sin electricidad.");
                }
            }
        }

        private void LateUpdate()
        {
            if (controlLegacyLighting || gameOver || finished) return;
            float progress = Mathf.Clamp01(1f - timeLeft / Mathf.Max(1f, duration));
            ApplyEnviroTime(progress);
        }

        public void BeginAtFuelCutoff()
        {
            if (countdownStarted) return;
            countdownStarted = true;
            startTime = Time.realtimeSinceStartup;
            timeLeft = duration;
            if (gameUI != null) gameUI.SetGameplayVisible(true);
        }

        private void UpdateDayTimer()
        {
            timeLeft = countdownStarted ? duration - (Time.realtimeSinceStartup - startTime) : duration;
            float progress = Mathf.Clamp01(1f - timeLeft / Mathf.Max(1f, duration));
            ApplyEnviroTime(progress);
            if (controlLegacyLighting && sun != null)
                sun.intensity = Mathf.Lerp(1.1f, 0.05f, progress);
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
            if (timerText != null)
            {
                timerText.enabled = true;
                if (!timerText.gameObject.activeSelf) timerText.gameObject.SetActive(true);
                int seconds = Mathf.Max(0, Mathf.CeilToInt(timeLeft));
                timerText.text = "TIEMPO RESTANTE\n" + seconds / 60 + ":" + (seconds % 60).ToString("00");
            }
            if (countdownStarted && timeLeft <= 0f) GameOver();
        }

        private void ApplyEnviroTime(float progress)
        {
            if (controlLegacyLighting) return;
            EnsureEnviroClock();
            if (enviroTime == null) return;
            float hour;
            if (progress <= 0.7f)
                hour = Mathf.Lerp(enviroStartHour, 17.8f, progress / 0.7f);
            else if (progress <= 0.85f)
                hour = Mathf.Lerp(17.8f, 18f, (progress - 0.7f) / 0.15f);
            else
                hour = Mathf.Lerp(18f, enviroNightHour, (progress - 0.85f) / 0.15f);
            enviroTime.SetTimeOfDay(hour);
            enviroTime.UpdateModule();
        }

        private void EnsureEnviroClock()
        {
            if (controlLegacyLighting) return;
            if (enviroManager == null) enviroManager = FindFirstObjectByType<EnviroManager>();
            if (enviroManager == null || enviroManager.Time == null) return;
            if (!legacySunSuppressed && sun != null && !sun.transform.IsChildOf(enviroManager.transform))
            {
                legacySunWasEnabled = sun.enabled;
                sun.enabled = false;
                legacySunSuppressed = true;
            }
            if (enviroTime == null)
            {
                originalEnviroTime = enviroManager.Time;
                enviroTime = Instantiate(originalEnviroTime);
                enviroTime.name = "Reloj de partida (runtime)";
                enviroTime.Settings = JsonUtility.FromJson<EnviroTime>(JsonUtility.ToJson(originalEnviroTime.Settings));
                enviroTime.active = true;
                enviroTime.Settings.simulate = false;
                enviroTime.Settings.calenderType = EnviroTime.CalenderType.Custom;
                enviroTime.Settings.customSunOffset = -6f;
                enviroTime.SetTimeOfDay(enviroStartHour);
            }
            enviroManager.Time = enviroTime;
        }

        private static AudioSource FindSource(string objectName)
        {
            GameObject sourceObject = GameObject.Find(objectName);
            return sourceObject != null ? sourceObject.GetComponent<AudioSource>() : null;
        }

        public bool TurnOnGenerator()
        {
            if (generatorOn || gameOver || finished) return false;
            if (!insideGarage)
            {
                ShowMessage("Primero subí al techo y descendé por el hueco hasta el taller.");
                return false;
            }

            generatorOn = true;
            generatorFuelRemaining = generatorFuelSeconds;
            stationPowerConnected = false;
            SetStreetLights(false);
            if (gameUI != null)
            {
                gameUI.SetGeneratorFuelTime(generatorFuelRemaining);
                gameUI.SetObjective("OBJETIVO 3/8\nCONECTÁ EL GENERADOR\nUsá la caja eléctrica para darle energía a la estación.");
                gameUI.ShowMessage("Tenés 60 segundos de combustible. Conectalo a la caja eléctrica, abrí el portón y llená el bidón.");
            }
            return true;
        }

        public void ConnectStationPower()
        {
            if (stationPowerConnected || gameOver || finished || !generatorOn || !insideGarage) return;
            stationPowerConnected = true;
            SetStreetLights(true);
            if (gameUI != null)
            {
                gameUI.SetObjective("OBJETIVO 4/8\nABRÍ EL PORTÓN\nUsá el interruptor junto al portón.");
                gameUI.ShowMessage("La estación tiene energía. Abrí el portón y buscá el bidón.");
            }
        }

        public void EnterGarage()
        {
            if (insideGarage || gameOver) return;

            insideGarage = true;
            if (gameUI != null)
            {
                gameUI.SetObjective("OBJETIVO 2/8\nENCENDÉ EL GENERADOR\nEstá dentro del taller; presioná [E] en el botón rojo.");
                gameUI.ShowMessage("Bajaste al taller. Ahora podés encender el generador.");
            }
        }

        public void OpenGarage()
        {
            if (garageOpen || gameOver) return;

            garageOpen = true;
            if (gameUI != null)
            {
                gameUI.SetObjective("OBJETIVO 5/8\nTOMÁ EL BIDÓN VACÍO\nAcercate y presioná [E].");
                gameUI.ShowMessage("Portón abierto. Ahora podés tomar el bidón.");
            }
        }

        public void NotifyGarageDoorMotionStarted()
        {
            if (!gameOver && !finished) GarageDoorMotionStarted?.Invoke();
        }

        public void TakeFuelCan()
        {
            if (hasFuelCan) return;

            hasFuelCan = true;
            if (gameUI != null)
            {
                gameUI.SetObjective("OBJETIVO 6/8\nLLENÁ EL BIDÓN\nLlevalo al surtidor antes de que se agote el generador.");
                gameUI.ShowMessage("Tenés el bidón vacío. El generador se está consumiendo.");
            }
        }

        public void FillFuelCan()
        {
            if (fuelCanFull) return;

            fuelCanFull = true;
            if (gameUI != null)
            {
                gameUI.HideGeneratorFuelTime();
                gameUI.SetObjective("OBJETIVO 7/8\nCARGÁ EL AUTO\nLlevá el bidón lleno hasta el auto y presioná [E].");
                gameUI.ShowMessage("Bidón lleno. Ahora cargá el tanque del auto.");
            }
        }

        public void FillCar()
        {
            if (carHasFuel) return;

            carHasFuel = true;
            if (gameUI != null)
            {
                gameUI.SetObjective("OBJETIVO 8/8\nSUBITE AL AUTO\nAcercate al auto y presioná [E] para arrancarlo.");
                gameUI.ShowMessage("Auto cargado. Subite para salir de la estación.");
            }
        }

        public void StartDriving()
        {
            if (driving || gameOver) return;

            driving = true;
            if (gameUI != null)
            {
                gameUI.HidePrompt();
                gameUI.SetObjective("OBJETIVO FINAL\nLLEGÁ A LA RUTA\nManejá hasta la salida antes de que anochezca.");
                gameUI.ShowMessage("Auto arrancado. Llegá a la ruta antes de que anochezca.");
            }
            DrivingStarted?.Invoke();
        }

        public void TryFinish()
        {
            if (finished || gameOver) return;

            if (!carHasFuel)
            {
                ShowMessage("Primero necesitás cargar el auto.");
                return;
            }

            if (!driving)
            {
                ShowMessage("Tenés que salir manejando el auto.");
                return;
            }

            finished = true;
            stationPowerConnected = false;
            SetStreetLights(false);
            StopAllGameplayAudio(victorySource);
            StopTensionMusic();
            PlayOutcomeSound(victorySource);
            if (gameUI != null) gameUI.ShowCompleted();
            GameFinished?.Invoke();
        }

        public void GameOver()
        {
            if (finished || gameOver) return;

            gameOver = true;
            stationPowerConnected = false;
            SetStreetLights(false);
            StopAllGameplayAudio(gameOverSource);
            StopTensionMusic();
            PlayOutcomeSound(gameOverSource);
            if (gameUI != null) gameUI.ShowGameOver();
            GameLost?.Invoke();
        }

        private static void StopAllGameplayAudio(AudioSource keepSource)
        {
            AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (AudioSource source in allSources)
            {
                if (source == null || source == keepSource) continue;
                source.Stop();
            }
        }

        private static void PlayOutcomeSound(AudioSource outcomeSource)
        {
            // Freeze every future AudioSource too, including PlayClipAtPoint-created sources.
            AudioListener.pause = true;
            if (outcomeSource == null) return;

            outcomeSource.ignoreListenerPause = true;
            outcomeSource.Stop();
            outcomeSource.Play();
        }

        private static void StopTensionMusic()
        {
            AudioController audio = FindFirstObjectByType<AudioController>();
            if (audio != null) audio.StopMusic();
        }

        private void SetStreetLights(bool powered)
        {
            if (streetLightsRoot == null)
            {
                GameObject lightsObject = GameObject.Find("StreetLights");
                if (lightsObject != null) streetLightsRoot = lightsObject.transform;
            }

            if (streetLightsRoot == null) return;
            foreach (Light streetLight in streetLightsRoot.GetComponentsInChildren<Light>(true))
            {
                if (streetLight == null) continue;

                if (powered)
                {
                    // Some generated posts are disabled as prefab instances,
                    // not just as Light components. Reactivate that post when
                    // the station receives power.
                    Transform post = streetLight.transform;
                    while (post.parent != null && post.parent != streetLightsRoot)
                        post = post.parent;
                    post.gameObject.SetActive(true);
                }

                streetLight.enabled = powered;
            }
        }

        public void ShowMessage(string text)
        {
            if (gameUI != null) gameUI.ShowMessage(text);
        }

        private void OnDestroy()
        {
            if (legacySunSuppressed && sun != null) sun.enabled = legacySunWasEnabled;
            if (enviroManager != null && enviroManager.Time == enviroTime)
                enviroManager.Time = originalEnviroTime;
            if (enviroTime != null) Destroy(enviroTime);
        }

    }
}
