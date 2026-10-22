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
        private const float GeneratorFuelSeconds = 30f;
        [SerializeField] private Transform streetLightsRoot;
        [SerializeField] private Light sun;
        [SerializeField] private Text timerText;
        [SerializeField] private float duration = 180f;
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private bool controlLegacyLighting;
        [SerializeField] private float enviroStartHour = 17.25f;
        [SerializeField] private float enviroNightHour = 23f;
        [SerializeField] private float pursuitSpeedLimitKmh = 60f;

        public event Action GeneratorFuelDepleted;
        public event Action GarageDoorMotionStarted;
        public event Action DrivingStarted;
        public event Action TensionStarted;
        public event Action PursuitStarted;
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
        public bool pursuitActive;
        public bool pursuitTriggerReached;
        public bool roadReached;
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
        private CarDriver playerCar;
        private bool carHeadLightsOn;
        private bool tensionStarted;
        private PhysicsMaterial grassPhysicMaterial;

        private void Start()
        {
            AudioListener.pause = false;
            if (gameUI == null) gameUI = FindFirstObjectByType<GameUI>();
            if (playerCar == null) playerCar = FindFirstObjectByType<CarDriver>();
            if (gameOverSource == null) gameOverSource = FindSource("GameOverSource");
            if (victorySource == null) victorySource = FindSource("VictorySource");
            stationPowerConnected = false;
            SetStreetLights(false);
            ConfigureGroundGrip();
            roadReached = false;
            carHeadLightsOn = false;
            tensionStarted = false;
            timeLeft = duration;
            startTime = Time.time;
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
                gameUI.SetObjective("OBJETIVO 1/8\nSUBI AL TECHO Y BAJA POR EL HUECO\nAcercate al borde, colgate y descende lentamente al taller.");
                gameUI.ShowMessage("El generador esta dentro del taller. Primero tenes que entrar por el techo.");
            }
        }

        private void Update()
        {
            if (gameOver || finished) return;
            // Keep the same Enviro clock running after the road checkpoint.
            // Only its HUD panel is hidden; the transition to night remains
            // continuous and no second time-out is generated after the road.
            UpdateDayTimer();
            TryStartPursuit();
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
                    ShowMessage("El generador se quedo sin combustible antes de llenar el bidon.");
                    GameOver();
                }
            }
        }

        private void LateUpdate()
        {
            if (controlLegacyLighting || gameOver || finished) return;
            float progress = Mathf.Clamp01(1f - timeLeft / Mathf.Max(1f, duration));
            ApplyEnviroTime(progress);
            SetPlayerHeadLights(progress >= 0.84f);
        }

        public void BeginAtFuelCutoff()
        {
            if (countdownStarted) return;
            countdownStarted = true;
            roadReached = false;
            startTime = Time.time;
            timeLeft = duration;
            if (gameUI != null) gameUI.SetGameplayVisible(true);
        }

        private void UpdateDayTimer()
        {
            timeLeft = countdownStarted ? duration - (Time.time - startTime) : duration;
            float progress = Mathf.Clamp01(1f - timeLeft / Mathf.Max(1f, duration));
            ApplyEnviroTime(progress);
            SetPlayerHeadLights(progress >= 0.84f);
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
            if (!roadReached && timerText != null)
            {
                timerText.enabled = true;
                if (!timerText.gameObject.activeSelf) timerText.gameObject.SetActive(true);
                int seconds = Mathf.Max(0, Mathf.CeilToInt(timeLeft));
                timerText.text = "TIEMPO RESTANTE\n" + seconds / 60 + ":" + (seconds % 60).ToString("00");
            }
            if (countdownStarted && timeLeft <= 20f) RequestTension();
            if (countdownStarted && timeLeft <= 0f && !roadReached) GameOver();
        }

        public void ReachRoad()
        {
            if (roadReached || gameOver || finished) return;

            roadReached = true;

            if (gameUI != null)
            {
                gameUI.HideCountdown();
                if (!pursuitActive)
                {
                    gameUI.SetObjective("RUTA ALCANZADA\nSEGUI POR LA RUTA\nLa persecucion comienza mas adelante.");
                    gameUI.ShowMessage("Llegaste a la ruta. El reloj ya no provoca derrota; segui avanzando.");
                }
            }
        }

        private void RequestTension()
        {
            if (tensionStarted) return;
            tensionStarted = true;
            TensionStarted?.Invoke();
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
                ShowMessage("Primero subi al techo y descende por el hueco hasta el taller.");
                return false;
            }

            generatorOn = true;
            generatorFuelRemaining = GeneratorFuelSeconds;
            stationPowerConnected = false;
            SetStreetLights(true);
            if (gameUI != null)
            {
                gameUI.SetGeneratorFuelTime(generatorFuelRemaining);
                gameUI.SetObjective("OBJETIVO 3/8\nCONECTA EL GENERADOR\nUsa la caja electrica para darle energia a la estacion.");
                gameUI.ShowMessage("Tenes 60 segundos de combustible. Conectalo a la caja electrica, abri el porton y llena el bidon.");
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
                gameUI.SetObjective("OBJETIVO 4/8\nABRI EL PORTON\nUsa el interruptor junto al porton.");
                gameUI.ShowMessage("La estacion tiene energia. Abri el porton y busca el bidon.");
            }
        }

        public void EnterGarage()
        {
            if (insideGarage || gameOver) return;

            insideGarage = true;
            if (gameUI != null)
            {
                gameUI.SetObjective("OBJETIVO 2/8\nENCENDE EL GENERADOR\nEsta dentro del taller; presiona [E] en el boton rojo.");
                gameUI.ShowMessage("Bajaste al taller. Ahora podes encender el generador.");
            }
        }

        public void OpenGarage()
        {
            if (garageOpen || gameOver) return;

            garageOpen = true;
            if (gameUI != null)
            {
                gameUI.SetObjective("OBJETIVO 5/8\nTOMA EL BIDON VACIO\nAcercate y presiona [E].");
                gameUI.ShowMessage("Porton abierto. Ahora podes tomar el bidon.");
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
                gameUI.SetObjective("OBJETIVO 6/8\nLLENA EL BIDON\nLlevalo al surtidor antes de que se agote el generador.");
                gameUI.ShowMessage("Tenes el bidon vacio. El generador se esta consumiendo.");
            }
        }

        public void FillFuelCan()
        {
            if (fuelCanFull) return;

            fuelCanFull = true;
            if (gameUI != null)
            {
                gameUI.HideGeneratorFuelTime();
                gameUI.SetObjective("OBJETIVO 7/8\nCARGA EL AUTO\nLleva el bidon lleno hasta el auto y presiona [E].");
                gameUI.ShowMessage("Bidon lleno. Ahora carga el tanque del auto.");
            }
        }

        public void FillCar()
        {
            if (carHasFuel) return;

            carHasFuel = true;
            if (gameUI != null)
            {
                gameUI.SetObjective("OBJETIVO 8/8\nSUBITE AL AUTO\nAcercate al auto y presiona [E] para arrancarlo.");
                gameUI.ShowMessage("Auto cargado. Subite para salir de la estacion.");
            }
        }

        public void StartDriving()
        {
            if (driving || gameOver) return;

            driving = true;
            if (gameUI != null)
            {
                gameUI.HidePrompt();
                gameUI.SetObjective("OBJETIVO FINAL\nLLEGA A LA RUTA\nManeja hasta la salida antes de que anochezca.");
                gameUI.ShowMessage("Auto arrancado. Llega a la ruta antes de que anochezca.");
            }
            DrivingStarted?.Invoke();
        }

        public void NotifyPursuitTrigger()
        {
            if (gameOver || finished) return;
            pursuitTriggerReached = true;
            TryStartPursuit();
        }

        private void TryStartPursuit()
        {
            if (pursuitActive || !pursuitTriggerReached || !driving || gameOver || finished) return;
            if (playerCar == null) playerCar = FindFirstObjectByType<CarDriver>();
            if (playerCar == null) return;
            if (!roadReached && playerCar.SpeedKmh < pursuitSpeedLimitKmh) return;

            pursuitActive = true;
            RequestTension();
            if (gameUI != null)
            {
                gameUI.SetObjective("PERSECUCION ACTIVADA\nEL PATRULLERO TE SIGUE\nEscapa y llega a la salida.");
                gameUI.ShowMessage("El patrullero te detecto. Acelera y escapa.");
                gameUI.SetPursuitDistance(0f, 100f);
            }
            PursuitStarted?.Invoke();
        }

        public void UpdatePursuitDistance(float currentDistance, float requiredDistance)
        {
            if (!pursuitActive || gameOver || finished || gameUI == null) return;
            gameUI.SetPursuitDistance(currentDistance, requiredDistance);
        }

        public void TryFinish()
        {
            if (finished || gameOver) return;

            if (!roadReached)
            {
                ShowMessage("Primero llega a la ruta y escapa del patrullero.");
                return;
            }

            if (!carHasFuel)
            {
                ShowMessage("Primero necesitas cargar el auto.");
                return;
            }

            if (!driving)
            {
                ShowMessage("Tenes que salir manejando el auto.");
                return;
            }

            if (!pursuitActive)
            {
                ShowMessage("Primero activa la persecucion del patrullero.");
                return;
            }

            finished = true;
            stationPowerConnected = false;
            SetStreetLights(false);
            StopAllGameplayAudio(victorySource);
            StopTensionMusic();
            PlayOutcomeSound(victorySource);
            if (gameUI != null)
            {
                gameUI.HidePursuitDistance();
                gameUI.ShowCompleted();
            }
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
            if (gameUI != null)
            {
                gameUI.HidePursuitDistance();
                gameUI.ShowGameOver();
            }
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

            if (streetLightsRoot != null)
            {
                foreach (Light streetLight in streetLightsRoot.GetComponentsInChildren<Light>(true))
                {
                    if (streetLight == null) continue;
                    streetLight.enabled = powered;
                }
            }

            // LampPost-1 has no Light component: its visible lamp is the
            // child mesh named "layer 6". Add the real light to all four posts
            // without changing their transforms or the source prefab.
            foreach (Transform part in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!string.Equals(part.name, "layer 6", StringComparison.OrdinalIgnoreCase) || !IsLampPostPart(part))
                    continue;

                ConfigureLampPostLight(part, powered);
            }
        }

        private static bool IsLampPostPart(Transform part)
        {
            Transform current = part.parent;
            while (current != null)
            {
                if (current.name.StartsWith("LampPost-1", StringComparison.OrdinalIgnoreCase)) return true;
                current = current.parent;
            }
            return false;
        }

        private static void ConfigureLampPostLight(Transform part, bool powered)
        {
            const float lampRange = 20f;
            const float lampIntensity = 9f;
            Color lampColor = new Color(1f, 0.72f, 0.38f);

            Light lamp = part.GetComponent<Light>();
            if (lamp == null) lamp = part.gameObject.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = lampColor;
            lamp.range = lampRange;
            lamp.intensity = lampIntensity;
            lamp.bounceIntensity = 1.5f;
            lamp.renderMode = LightRenderMode.ForcePixel;
            lamp.shadows = powered ? LightShadows.Soft : LightShadows.None;
            lamp.enabled = powered;

            foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                renderer.enabled = true;
                Material material = renderer.material;
                if (!material.HasProperty("_EmissionColor")) continue;

                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", powered ? lampColor * 7f : Color.black);
            }
        }

        private void ConfigureGroundGrip()
        {
            if (grassPhysicMaterial == null)
            {
                grassPhysicMaterial = new PhysicsMaterial("PastoGripBajo")
                {
                    staticFriction = 0.18f,
                    dynamicFriction = 0.10f,
                    bounciness = 0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };
            }

            foreach (Transform item in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string objectName = item.name.ToLowerInvariant();
                if (!objectName.Contains("terreno") && !objectName.Contains("grass") && !objectName.Contains("pasto"))
                    continue;

                foreach (Collider collider in item.GetComponentsInChildren<Collider>(true))
                    if (collider != null && !collider.isTrigger) collider.sharedMaterial = grassPhysicMaterial;
            }
        }

        private void SetPlayerHeadLights(bool enabled)
        {
            if (playerCar == null) playerCar = FindFirstObjectByType<CarDriver>();
            if (playerCar == null || carHeadLightsOn == enabled) return;

            carHeadLightsOn = enabled;
            playerCar.SetHeadLights(enabled);
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
