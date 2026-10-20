using UnityEngine;
using UnityEngine.InputSystem;
using VehiclePhysics;

namespace EnElCamino
{
    [DefaultExecutionOrder(-100)]
    public class CarDriver : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private GameObject player;
        [SerializeField] private ThirdPersonCamera cameraFollow;
        [SerializeField] private float speed = 7f;
        [SerializeField] private float turnSpeed = 75f;

        private VPVehicleController vehicleController;
        private bool driving;
        private bool vehicleReady = true;
        private bool engineRequested;
        private float starterUntil;
        private float throttleInput;
        private float steerInput;

        public bool IsReady => vehicleReady;
        public bool EngineRequested => engineRequested;

        private void Awake()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            vehicleController = GetComponent<VPVehicleController>();
            if (vehicleController == null) return;
            // The new Input System writes the VPP bus directly. The legacy input
            // reader would otherwise overwrite these controls every physics step.
            VPStandardInput legacyInput = GetComponent<VPStandardInput>();
            if (legacyInput != null) legacyInput.enabled = false;
            vehicleReady = false;
            foreach (VPAudio audio in GetComponentsInChildren<VPAudio>(true))
            {
                audio.enabled = true;
                foreach (AudioSource source in audio.GetComponentsInChildren<AudioSource>(true))
                {
                    source.enabled = true;
                    source.mute = false;
                }
            }
        }

        public void SetVehicleReady(bool ready)
        {
            vehicleReady = ready;
            if (ready) return;
            driving = false;
            throttleInput = 0f;
            steerInput = 0f;
        }

        public void EnterCar()
        {
            if (!vehicleReady || driving || gameFlow == null || !gameFlow.carHasFuel || gameFlow.gameOver || gameFlow.finished) return;
            driving = true;
            StartEngineAudio(true);
            if (player != null) player.SetActive(false);
            CinemachineFollowSetup cameraRig = FindFirstObjectByType<CinemachineFollowSetup>();
            if (cameraRig != null) cameraRig.SetTarget(transform);
            else if (cameraFollow != null) cameraFollow.SetTarget(transform);
            gameFlow.StartDriving();
        }

        private void Update()
        {
            if (gameFlow != null && (gameFlow.gameOver || gameFlow.finished))
            {
                StopEngineAudio();
                driving = false;
                return;
            }
            throttleInput = 0f;
            steerInput = 0f;
            if (!driving || Keyboard.current == null) return;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) throttleInput = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) throttleInput = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) steerInput = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) steerInput = 1f;
            if (vehicleController == null)
            {
                transform.Rotate(0f, steerInput * turnSpeed * Time.deltaTime, 0f);
                transform.Translate(Vector3.forward * throttleInput * speed * Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (vehicleController == null || !vehicleController.initialized) return;
            int[] input = vehicleController.data.Get(Channel.Input);
            bool stopped = gameFlow != null && (gameFlow.gameOver || gameFlow.finished);
            bool engineOn = engineRequested && !stopped;
            // VPP bus Key: -1 = off, 0 = ignition on, 1 = starter motor.
            input[InputData.Key] = !engineOn ? -1 : Time.time < starterUntil ? 1 : 0;
            input[InputData.Steer] = driving ? Mathf.RoundToInt(steerInput * 10000f) : 0;
            bool reversing = throttleInput < -0.05f;
            float throttle = driving ? Mathf.Abs(throttleInput) : !vehicleReady ? 0.18f : 0f;
            input[InputData.Throttle] = engineOn ? Mathf.RoundToInt(throttle * 10000f) : 0;
            input[InputData.Brake] = (!driving && vehicleReady) || stopped ? 10000 : 0;
            input[InputData.Handbrake] = !driving && vehicleReady ? 10000 : 0;
            input[InputData.Clutch] = driving ? 0 : 10000;
            input[InputData.ManualGear] = driving ? (reversing ? -1 : 1) : 0;
            input[InputData.AutomaticGear] = driving ? (reversing ? 2 : 4) : 3;
        }

        public void StartEngineAudio(bool playStartSound)
        {
            // VPAudio plays the Sport Coupe's original clips from real RPM/load.
            if (playStartSound && !engineRequested) starterUntil = Time.time + 1.4f;
            else if (!playStartSound) starterUntil = 0f;
            engineRequested = true;
        }

        public void StopEngineAudio()
        {
            engineRequested = false;
            throttleInput = 0f;
            steerInput = 0f;
        }
    }
}
