using System;
using UnityEngine;

namespace EnElCamino
{
    public class GameFlow : MonoBehaviour
    {
        [SerializeField] private GameUI gameUI;
        [SerializeField] private AudioSource gameOverSource;
        [SerializeField] private AudioSource victorySource;
        [SerializeField] private float generatorFuelSeconds = 60f;

        public event Action GeneratorStarted;
        public event Action StationPowerConnected;
        public event Action GarageDoorMotionStarted;
        public event Action GarageOpened;
        public event Action FuelCanTaken;
        public event Action FuelCanFilled;
        public event Action CarFueled;
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

        private void Start()
        {
            AudioListener.pause = false;
            if (gameUI == null) gameUI = FindFirstObjectByType<GameUI>();
            if (gameOverSource == null) gameOverSource = FindSource("GameOverSource");
            if (victorySource == null) victorySource = FindSource("VictorySource");
            if (gameUI != null)
            {
                gameUI.SetObjective("OBJETIVO 1/8\nSUBÍ AL TECHO Y BAJÁ POR EL HUECO\nAcercate al borde, colgate y descendé lentamente al taller.");
                gameUI.ShowMessage("El generador está dentro del taller. Primero tenés que entrar por el techo.");
            }
        }

        private void Update()
        {
            if (!generatorOn || fuelCanFull || gameOver || finished) return;
            generatorFuelRemaining -= Time.deltaTime;
            if (gameUI != null) gameUI.SetGeneratorFuelTime(generatorFuelRemaining);
            if (generatorFuelRemaining <= 0f)
            {
                ShowMessage("El generador se quedó sin combustible antes de llenar el bidón.");
                GameOver();
            }
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
            if (gameUI != null)
            {
                gameUI.SetGeneratorFuelTime(generatorFuelRemaining);
                gameUI.SetObjective("OBJETIVO 3/8\nCONECTÁ EL GENERADOR\nUsá la caja eléctrica para darle energía a la estación.");
                gameUI.ShowMessage("Tenés 60 segundos de combustible. Conectalo a la caja eléctrica, abrí el portón y llená el bidón.");
            }
            GeneratorStarted?.Invoke();
            return true;
        }

        public void ConnectStationPower()
        {
            if (stationPowerConnected || gameOver || finished || !generatorOn || !insideGarage) return;
            stationPowerConnected = true;
            if (gameUI != null)
            {
                gameUI.SetObjective("OBJETIVO 4/8\nABRÍ EL PORTÓN\nUsá el interruptor junto al portón.");
                gameUI.ShowMessage("La estación tiene energía. Abrí el portón y buscá el bidón.");
            }
            StationPowerConnected?.Invoke();
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
            GarageOpened?.Invoke();
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
            FuelCanTaken?.Invoke();
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
            FuelCanFilled?.Invoke();
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
            CarFueled?.Invoke();
        }

        public void StartDriving()
        {
            if (driving || gameOver) return;

            driving = true;
            if (gameUI != null)
            {
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
            AdaptiveMusicController adaptiveMusic = FindFirstObjectByType<AdaptiveMusicController>();
            if (adaptiveMusic != null) adaptiveMusic.StopMusic();
        }

        public void ShowMessage(string text)
        {
            if (gameUI != null) gameUI.ShowMessage(text);
        }

    }
}
