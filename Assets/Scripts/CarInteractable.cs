using UnityEngine;

namespace EnElCamino
{
    [RequireComponent(typeof(AudioSource))]
    public class CarInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private FuelCan fuelCan;
        [SerializeField] private CarDriver carDriver;
        [SerializeField] private AudioClip startSound;
        private AudioSource audioSource;
        private bool used;

        private void Awake() => audioSource = GetComponent<AudioSource>();

        public string Prompt
        {
            get
            {
                if (used) return "[E] Subir al auto";
                if (!gameFlow.fuelCanFull) return "Falta llenar el bidón";
                return "[E] Cargar tanque del auto";
            }
        }

        public void Interact()
        {
            if (used)
            {
                if (gameFlow.gameOver || gameFlow.finished) return;
                if (startSound != null) audioSource.PlayOneShot(startSound);
                carDriver.EnterCar();
                return;
            }

            if (!gameFlow.fuelCanFull)
            {
                gameFlow.ShowMessage("Primero llená el bidón en el surtidor.");
                return;
            }

            used = true;
            gameFlow.FillCar();
            fuelCan.Use();
        }
    }
}
