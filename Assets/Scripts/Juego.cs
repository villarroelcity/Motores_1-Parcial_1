using System;
using UnityEngine;
using UnityEngine.UI;

namespace EnElCamino
{
    [DefaultExecutionOrder(-200)]
    public class Juego : MonoBehaviour
    {
        [SerializeField] private UI gameUI;
        [SerializeField] private AudioSource gameOverSource;
        [SerializeField] private AudioSource victorySource;
        private const float GeneratorFuelSeconds = 30f;
        [SerializeField] private Light sun;
        [SerializeField] private Text timerText;
        [SerializeField] private float duration = 120f;
        [SerializeField] private float pursuitDurationSeconds = 60f;
        [SerializeField] private Material skyboxMaterial;

        public event Action GeneratorFuelDepleted;
        public event Action GarageDoorMotionStarted;
        public event Action DrivingStarted;
        public event Action TensionStarted;
        public event Action PursuitStarted;
        public event Action GameFinished;
        public event Action GameLost;

        public UI UI => gameUI;

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
        private float pursuitTimeRemaining;
        private float startTime;
        private float lastSkyProgress = -1f;
        private bool countdownStarted;
        private AutoJugador playerCar;
        private float nextHeadlightRefresh;
        private bool tensionStarted;
        [SerializeField] private PhysicsMaterial grassPhysicMaterial;

        [SerializeField] private Light[] stationLights;
        [SerializeField] private Renderer[] stationLampRenderers;

        private void Start()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (gameUI == null) gameUI = FindFirstObjectByType<UI>();
            if (playerCar == null) playerCar = FindFirstObjectByType<AutoJugador>();
            stationPowerConnected = false;
            SetStreetLights(false);
            roadReached = false;
            // VPP vehicles keep their running lights on from the start; the
            // day/night transition only changes the world lighting.
            if (playerCar != null) playerCar.SetHeadLights(true);
            tensionStarted = false;
            timeLeft = duration;
            startTime = Time.time;
            if (skyboxMaterial == null) skyboxMaterial = RenderSettings.skybox;
            if (timerText == null) timerText = FindFirstObjectByType<Text>(FindObjectsInactive.Include);
            ApplyDaylight(0f);
            if (timerText != null)
            {
                timerText.enabled = true;
                if (!timerText.gameObject.activeSelf) timerText.gameObject.SetActive(true);
            }
        }

        private void Update()
        {
            if (Time.time >= nextHeadlightRefresh)
            {
                if (playerCar == null) playerCar = FindFirstObjectByType<AutoJugador>();
                if (playerCar != null) playerCar.SetHeadLights(true);
                nextHeadlightRefresh = Time.time + 1f;
            }

            if (gameOver || finished) return;
            // Keep the same daylight clock running after the road checkpoint.
            // Only its HUD panel is hidden; the transition to night remains
            // continuous and no second time-out is generated after the road.
            UpdateDayTimer();
            TryStartPursuit();
            if (pursuitActive)
            {
                pursuitTimeRemaining = Mathf.Max(0f, pursuitTimeRemaining - Time.deltaTime);
                if (gameUI != null) gameUI.SetPursuitTimeRemaining(pursuitTimeRemaining);
                if (pursuitTimeRemaining <= 0f)
                {
                    GameOver();
                    return;
                }
            }
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

        public void BeginAtFuelCutoff()
        {
            if (countdownStarted) return;
            countdownStarted = true;
            roadReached = false;
            startTime = Time.time;
            timeLeft = duration;
            if (gameUI != null) gameUI.SetGameplayVisible(true);
        }

        public void ShowInitialMission()
        {
            if (gameOver || finished || gameUI == null) return;

            gameUI.SetObjective("OBJETIVO 1/8\nBUSCAR UN BIDON\nAcercate al taller a ver si encuentras alguno.");
        }

        public void ShowFuelCutoffMessage()
        {
            if (!gameOver && !finished && gameUI != null)
                gameUI.ShowMessage("El auto se quedo sin combustible.");
        }

        private void UpdateDayTimer()
        {
            timeLeft = countdownStarted ? duration - (Time.time - startTime) : duration;
            float progress = Mathf.Clamp01(1f - timeLeft / Mathf.Max(1f, duration));
            ApplyDaylight(progress);
            if (!roadReached && timerText != null)
            {
                timerText.enabled = true;
                if (!timerText.gameObject.activeSelf) timerText.gameObject.SetActive(true);
                int seconds = Mathf.Max(0, Mathf.CeilToInt(timeLeft));
                timerText.text = "ANOCHECE EN\n" + seconds / 60 + ":" + (seconds % 60).ToString("00");
            }
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
                    gameUI.SetObjective("VICTORIA\nLLEGASTE A LA RUTA");
                    gameUI.HideMessage();
                }
            }
        }

        public void NotifyPoliceSirenStarted()
        {
            if (!pursuitActive || gameOver || finished || tensionStarted) return;
            tensionStarted = true;
            TensionStarted?.Invoke();
        }

        public void ApplyDaylight(float progress)
        {
            float night = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, progress));
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(18f, -8f, night), -80f, 0f);
                sun.intensity = Mathf.Lerp(1.1f, 0.04f, night);
                sun.color = Color.Lerp(new Color(1f, 0.86f, 0.68f), new Color(0.45f, 0.6f, 1f), night);
            }
            RenderSettings.ambientLight = Color.Lerp(new Color(0.42f, 0.47f, 0.55f), new Color(0.07f, 0.085f, 0.13f), night);
            RenderSettings.fogColor = Color.Lerp(new Color(0.66f, 0.7f, 0.76f), new Color(0.045f, 0.06f, 0.1f), night);
            if (skyboxMaterial != null)
            {
                skyboxMaterial.SetFloat("_Exposure", Mathf.Lerp(1.1f, 0.35f, night));
                if (Mathf.Abs(progress - lastSkyProgress) > 0.04f)
                {
                    DynamicGI.UpdateEnvironment();
                    lastSkyProgress = progress;
                }
            }
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
                gameUI.SetObjective("OBJETIVO 3/8\nGIRA EL INTERRUPTOR\nGira el interruptor de la caja electrica para darle energia a la estacion.");
                gameUI.ShowMessage("Tenes 30 segundos de combustible. Gira el interruptor de la caja electrica, abri el porton y llena el bidon.");
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
                gameUI.ShowMessage("La estacion ya tiene energia. Abri el porton y agarra el bidon.");
            }
        }

        public void EnterGarage()
        {
            if (insideGarage || gameOver) return;

            insideGarage = true;
            if (gameUI != null)
            {
                gameUI.SetObjective("OBJETIVO 2/8\nENCENDE EL GENERADOR\npresiona [E] en el generador.");
            }
        }

        public void OpenGarage()
        {
            if (garageOpen || gameOver) return;

            garageOpen = true;
            if (gameUI != null)
            {
                gameUI.SetObjective("OBJETIVO 5/8\nTOMA EL BIDON VACIO\nAcercate y presiona [E].");
                gameUI.ShowMessage("Porton abierto. Ahora podes agarrar el bidon.");
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
                gameUI.SetObjective("OBJETIVO 6/8\nLLENA EL BIDON\nLlevalo al surtidor antes de que se apague el generador.");
                gameUI.ShowMessage("Tenes el bidon vacio. El generador se esta por apagar.");
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
                gameUI.SetObjective("OBJETIVO 8/8\nSUBITE AL AUTO\nPresiona [E] para arrancar.");
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
            if (playerCar == null) playerCar = FindFirstObjectByType<AutoJugador>();
            if (playerCar == null) return;

            pursuitActive = true;
            pursuitTimeRemaining = Mathf.Max(1f, pursuitDurationSeconds);
            if (gameUI != null)
            {
                gameUI.SetObjective("PERSECUCION\nEL PATRULLERO TE SIGUE\nSaca 200 m de ventaja antes de quedarte sin nafta.");
                gameUI.ShowMessage("Te queda un minuto de combustible. Escapa del patrullero.");
                gameUI.SetPursuitDistance(0f, 200f);
                gameUI.SetPursuitTimeRemaining(pursuitTimeRemaining);
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
                return;
            }

            finished = true;
            Time.timeScale = 0f;
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
            foreach (Light lamp in stationLights)
            {
                lamp.enabled = powered;
                lamp.shadows = powered ? LightShadows.Soft : LightShadows.None;
            }
            foreach (Renderer renderer in stationLampRenderers)
                VehicleLightUtility.SetAppearance(renderer, Color.white,
                    powered ? new Color(1f, .72f, .38f) * 7f : Color.black, false);
        }

        public bool IsGrassSurface(Collider collider)
        {
            return collider != null && grassPhysicMaterial != null &&
                   collider.sharedMaterial == grassPhysicMaterial;
        }

        public void ShowMessage(string text)
        {
            if (gameUI != null) gameUI.ShowMessage(text);
        }

    }
}
