using UnityEngine;
using VehiclePhysics;

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
                if (carDriver != null && !carDriver.IsReady) return "El auto está llegando...";
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
                if (carDriver != null && carDriver.enabled)
                {
                    carDriver.EnterCar();
                }
                else
                {
                    EnterNativeVppCar();
                }
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

        private void EnterNativeVppCar()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.constraints = RigidbodyConstraints.None;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }

            VPVehicleController vehicle = GetComponent<VPVehicleController>();
            if (vehicle != null)
            {
                vehicle.enabled = true;
                vehicle.gearbox.type = Gearbox.Type.Automatic;
                int[] input = vehicle.data.Get(Channel.Input);
                input[InputData.AutomaticGear] = 4;
                input[InputData.Brake] = 0;
                input[InputData.Handbrake] = 0;
            }

            VPStandardInput nativeInput = GetComponent<VPStandardInput>();
            if (nativeInput != null)
            {
                nativeInput.enabled = true;
                nativeInput.externalIgnition = VPStandardInput.IgnitionKey.AccOn;
            }

            GameObject player = GameObject.Find("Jugador");
            if (player != null) player.SetActive(false);

            ThirdPersonCamera camera = FindFirstObjectByType<ThirdPersonCamera>();
            if (camera != null) camera.SetTarget(transform);

            gameFlow.StartDriving();
        }
    }
}
