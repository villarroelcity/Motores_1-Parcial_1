using System.Collections;
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
                // The arrival helper is only for the scripted approach. From
                // the moment the player boards, Vehicle Physics Pro must be
                // the sole owner of steering, throttle, wheels and yaw.
                EnterNativeVppCar();
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
            if (carDriver != null) carDriver.RestoreFuelForNativeControl();
            if (carDriver != null) carDriver.enabled = false;
            CarArrivalSequence arrival = GetComponent<CarArrivalSequence>();
            if (arrival != null) arrival.enabled = false;

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
                input[InputData.Steer] = 0;
                input[InputData.Throttle] = 0;
                input[InputData.AutomaticGear] = 4;
                input[InputData.Brake] = 0;
                input[InputData.Handbrake] = 0;
                input[InputData.Clutch] = 0;
                input[InputData.Key] = 0;
            }

            VPStandardInput nativeInput = GetComponent<VPStandardInput>();
            if (nativeInput != null)
            {
                nativeInput.enabled = true;
                nativeInput.disableSteerInput = false;
                nativeInput.disableThrottleInput = false;
                nativeInput.disableBrakeInput = false;
                nativeInput.disableHandbrakeInput = false;
                nativeInput.disableClutchInput = false;
                nativeInput.disableGearShiftInputs = false;
                nativeInput.externalThrottle = 0f;
                nativeInput.externalBrake = 0f;
                nativeInput.externalHandbrake = 0f;
                nativeInput.externalSteer = 0f;
                nativeInput.externalIgnition = VPStandardInput.IgnitionKey.AccOn;
                StartCoroutine(StartNativeEngine(nativeInput));
            }

            GameObject player = GameObject.Find("Jugador");
            if (player != null) player.SetActive(false);

            ThirdPersonCamera camera = Camera.main != null
                ? Camera.main.GetComponent<ThirdPersonCamera>()
                : FindFirstObjectByType<ThirdPersonCamera>();
            if (camera != null)
            {
                camera.enabled = true;
                camera.SetTarget(transform);
                camera.SnapToTarget();
            }

            gameFlow.StartDriving();
        }

        private IEnumerator StartNativeEngine(VPStandardInput nativeInput)
        {
            nativeInput.externalIgnition = VPStandardInput.IgnitionKey.Start;
            yield return new WaitForSeconds(0.8f);
            if (nativeInput != null && nativeInput.enabled)
                nativeInput.externalIgnition = VPStandardInput.IgnitionKey.AccOn;
        }
    }
}
