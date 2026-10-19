using System;
using UnityEngine;

namespace EnElCamino
{
    public class GameFlow : MonoBehaviour
    {
        [SerializeField] private GameUI gameUI;
        [SerializeField] private AudioSource gameOverSource;
        [SerializeField] private AudioSource victorySource;

        public event Action GeneratorStarted;
        public event Action GarageOpened;
        public event Action FuelCanTaken;
        public event Action FuelCanFilled;
        public event Action CarFueled;
        public event Action DrivingStarted;
        public event Action GameFinished;
        public event Action GameLost;

        public GameUI UI => gameUI;

        public bool generatorOn;
        public bool hasFuelCan;
        public bool fuelCanFull;
        public bool carHasFuel;
        public bool insideGarage;
        public bool garageOpen;
        public bool driving;
        public bool fueling;
        public bool movementLocked;
        public bool exteriorLadderCarried;
        public bool exteriorLadderPlaced;
        public bool gameOver;
        public bool finished;

        private void Start()
        {
            if (gameOverSource == null) gameOverSource = FindSource("GameOverSource");
            if (victorySource == null) victorySource = FindSource("VictorySource");
            gameUI.SetObjective("OBJETIVO 1/7\nENCENDÉ EL GENERADOR\nEl botón rojo está fijo en el costado derecho del generador.");
            gameUI.ShowMessage("La estación está sin energía.");
        }

        private static AudioSource FindSource(string objectName)
        {
            GameObject sourceObject = GameObject.Find(objectName);
            return sourceObject != null ? sourceObject.GetComponent<AudioSource>() : null;
        }

        public void TurnOnGenerator()
        {
            if (generatorOn) return;

            generatorOn = true;
            gameUI.SetObjective("OBJETIVO 2/7\nENTRÁ AL GARAGE\nBuscá una forma de subir al techo del garage.");
            gameUI.ShowMessage("Generador encendido. El bidón está dentro del garage cerrado.");
            GeneratorStarted?.Invoke();
        }

        public void EnterGarage()
        {
            if (insideGarage || gameOver) return;

            insideGarage = true;
            gameUI.SetObjective("OBJETIVO 3/7\nABRÍ EL PORTÓN\nBajá por la escalera interior y presioná el botón rojo de adentro.");
            gameUI.ShowMessage("Ya estás adentro. Abrí el portón desde este lado.");
        }

        public void OpenGarage()
        {
            if (garageOpen || gameOver) return;

            garageOpen = true;
            gameUI.SetObjective("OBJETIVO 4/7\nTOMÁ EL BIDÓN VACÍO\nEl bidón está dentro del garage. Acercate y presioná [E].");
            gameUI.ShowMessage("Portón abierto. Ahora podés tomar el bidón.");
            GarageOpened?.Invoke();
        }

        public void TakeFuelCan()
        {
            if (hasFuelCan) return;

            hasFuelCan = true;
            gameUI.SetObjective("OBJETIVO 5/7\nLLENÁ EL BIDÓN\nLlevalo al surtidor y presioná [E].");
            gameUI.ShowMessage("Tenés el bidón vacío. Falta llenarlo.");
            FuelCanTaken?.Invoke();
        }

        public void FillFuelCan()
        {
            if (fuelCanFull) return;

            fuelCanFull = true;
            gameUI.SetObjective("OBJETIVO 6/7\nCARGÁ EL AUTO\nLlevá el bidón lleno hasta el auto y presioná [E].");
            gameUI.ShowMessage("Bidón lleno. Ahora cargá el tanque del auto.");
            FuelCanFilled?.Invoke();
        }

        public void FillCar()
        {
            if (carHasFuel) return;

            carHasFuel = true;
            gameUI.SetObjective("OBJETIVO 7/7\nSUBITE AL AUTO\nAcercate al auto y presioná [E] para arrancarlo.");
            gameUI.ShowMessage("Auto cargado. Subite para salir de la estación.");
            CarFueled?.Invoke();
        }

        public void StartDriving()
        {
            if (driving || gameOver) return;

            driving = true;
            gameUI.SetObjective("OBJETIVO 7/7\nLLEGÁ A LA RUTA\nManejá el auto hasta la salida de la estación.");
            gameUI.ShowMessage("Auto arrancado. Llegá a la ruta antes de que anochezca.");
            DrivingStarted?.Invoke();
        }

        public void TryFinish()
        {
            if (finished || gameOver) return;

            if (!carHasFuel)
            {
                gameUI.ShowMessage("Primero necesitás cargar el auto.");
                return;
            }

            if (!driving)
            {
                gameUI.ShowMessage("Tenés que salir manejando el auto.");
                return;
            }

            finished = true;
            StopAllGameplayAudio(victorySource);
            StopTensionMusic();
            if (victorySource != null) victorySource.Play();
            gameUI.ShowCompleted();
            GameFinished?.Invoke();
        }

        public void GameOver()
        {
            if (finished || gameOver) return;

            gameOver = true;
            StopAllGameplayAudio(gameOverSource);
            StopTensionMusic();
            if (gameOverSource != null) gameOverSource.Play();
            gameUI.ShowGameOver();
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

        private static void StopTensionMusic()
        {
            AdaptiveMusicController adaptiveMusic = FindFirstObjectByType<AdaptiveMusicController>();
            if (adaptiveMusic != null) adaptiveMusic.StopMusic();
        }

        public void ShowMessage(string text)
        {
            gameUI.ShowMessage(text);
        }

    }
}
