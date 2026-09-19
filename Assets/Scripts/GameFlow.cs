using UnityEngine;

namespace EnElCamino
{
    public class GameFlow : MonoBehaviour
    {
        [SerializeField] private GameUI gameUI;

        public bool generatorOn;
        public bool hasFuelCan;
        public bool fuelCanFull;
        public bool carHasFuel;
        public bool insideGarage;
        public bool garageOpen;
        public bool driving;
        public bool gameOver;
        public bool finished;

        private void Start()
        {
            gameUI.SetObjective("OBJETIVO 1/7\nENCENDÉ EL GENERADOR\nEl botón rojo está fijo en el costado derecho del generador.");
            gameUI.ShowMessage("La estación está sin energía.");
        }

        public void TurnOnGenerator()
        {
            if (generatorOn) return;

            generatorOn = true;
            gameUI.SetObjective("OBJETIVO 2/7\nENTRÁ AL GARAGE\nBuscá una forma de subir al techo del garage.");
            gameUI.ShowMessage("Generador encendido. El bidón está dentro del garage cerrado.");
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
        }

        public void TakeFuelCan()
        {
            if (hasFuelCan) return;

            hasFuelCan = true;
            gameUI.SetObjective("OBJETIVO 5/7\nLLENÁ EL BIDÓN\nLlevalo al surtidor y presioná [E].");
            gameUI.ShowMessage("Tenés el bidón vacío. Falta llenarlo.");
        }

        public void FillFuelCan()
        {
            if (fuelCanFull) return;

            fuelCanFull = true;
            gameUI.SetObjective("OBJETIVO 6/7\nCARGÁ EL AUTO\nLlevá el bidón lleno hasta el auto y presioná [E].");
            gameUI.ShowMessage("Bidón lleno. Ahora cargá el tanque del auto.");
        }

        public void FillCar()
        {
            if (carHasFuel) return;

            carHasFuel = true;
            gameUI.SetObjective("OBJETIVO 7/7\nSUBITE AL AUTO\nAcercate al auto y presioná [E] para arrancarlo.");
            gameUI.ShowMessage("Auto cargado. Subite para salir de la estación.");
        }

        public void StartDriving()
        {
            if (driving || gameOver) return;

            driving = true;
            gameUI.SetObjective("OBJETIVO 7/7\nLLEGÁ A LA RUTA\nManejá el auto hasta la salida de la estación.");
            gameUI.ShowMessage("Auto arrancado. Llegá a la ruta antes de que anochezca.");
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
            gameUI.ShowCompleted();
        }

        public void GameOver()
        {
            if (finished || gameOver) return;

            gameOver = true;
            gameUI.ShowGameOver();
        }

        public void ShowMessage(string text)
        {
            gameUI.ShowMessage(text);
        }

    }
}
