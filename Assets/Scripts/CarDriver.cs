using System.Collections.Generic;
using System.Reflection;
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
        private bool automaticTransmissionConfigured;
        private bool introFullThrottle;
        private readonly List<StartupAudioClip> startupAudioClips = new List<StartupAudioClip>();
        private float restoreStartupAudioAt = -1f;
        private Rigidbody vehicleBody;
        private Engine introEngine;
        private bool seedRunningEngine;
        private float fuelRatio = 1f;
        private bool arrivalParkingHeld;
        private RigidbodyConstraints drivingConstraints;
        private bool arrivalControl;
        private float brakeInput;
        private VPStandardInput nativeInput;

        private void ConfigureFrontSteeringOnly()
        {
            if (vehicleController == null || vehicleController.axles == null || vehicleController.axles.Length == 0)
                return;

            // VPP builds its steering wheel list from these axle settings.  Set
            // them before initialization as well as at runtime so a prefab or
            // scene override cannot make the rear axle steer.
            vehicleController.axles[0].steeringMode = Steering.SteeringMode.Steerable;
            vehicleController.axles[0].steeringRatio = 1f;
            for (int i = 1; i < vehicleController.axles.Length; i++)
            {
                vehicleController.axles[i].steeringMode = Steering.SteeringMode.Disabled;
                vehicleController.axles[i].steeringRatio = 1f;

                // VPP uses Unity WheelCollider.steerAngle for the visual wheel
                // pose too.  Clear any stale scene value on the non-steering
                // axle so the rear wheels stay straight when turning.
                ClearWheelSteerAngle(vehicleController.axles[i].leftWheel);
                ClearWheelSteerAngle(vehicleController.axles[i].rightWheel);
            }
        }

        private static void ClearWheelSteerAngle(VPWheelCollider wheel)
        {
            if (wheel == null) return;
            WheelCollider unityWheel = wheel.GetComponent<WheelCollider>();
            if (unityWheel != null) unityWheel.steerAngle = 0f;
        }

        private void SetNativeControls(bool enabled)
        {
            if (nativeInput == null) return;
            // The VPP component remains available on the vehicle prefab, but this
            // gameplay controller is the only writer to VPP's input data bus. If
            // both writers run at once, the raw axis and the game input fight each
            // other and the car appears to slide sideways.
            nativeInput.externalThrottle = 0f;
            nativeInput.externalBrake = 0f;
            nativeInput.externalHandbrake = 0f;
            nativeInput.externalSteer = 0f;
            nativeInput.externalClutch = 0f;
            nativeInput.lockBrakesOnEnable = false;
            nativeInput.enabled = false;

            if (!enabled || vehicleController == null || !vehicleController.initialized) return;

            int[] input = vehicleController.data.Get(Channel.Input);
            input[InputData.Brake] = 0;
            input[InputData.Handbrake] = 0;
            input[InputData.Clutch] = 0;
            input[InputData.Steer] = 0;
            input[InputData.AutomaticGear] = 4;
        }

        public void BeginFuelCutoffControl()
        {
            arrivalControl = true;
            introFullThrottle = false;
            SetNativeControls(true);
        }

        public void EndFuelCutoffControl()
        {
            arrivalControl = false;
            SetNativeControls(false);
        }

        private struct StartupAudioClip
        {
            public Component audioComponent;
            public FieldInfo clipField;
            public AudioClip originalClip;
        }

        public bool IsReady => vehicleReady;
        public bool EngineRequested => engineRequested;

        private void Awake()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            vehicleController = GetComponent<VPVehicleController>();
            vehicleBody = GetComponent<Rigidbody>();
            if (vehicleController == null) return;
            ConfigureFrontSteeringOnly();
            vehicleController.gearbox.type = Gearbox.Type.Automatic;
            nativeInput = GetComponent<VPStandardInput>();
            if (nativeInput == null) nativeInput = gameObject.AddComponent<VPStandardInput>();
            SetNativeControls(false);
            vehicleReady = false;
            foreach (VPAudioExtras audio in GetComponentsInChildren<VPAudioExtras>(true))
            {
                FieldInfo startupClip = audio.GetType().GetField("engineStartupClip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (startupClip != null && startupClip.FieldType == typeof(AudioClip))
                {
                    startupAudioClips.Add(new StartupAudioClip
                    {
                        audioComponent = audio,
                        clipField = startupClip,
                        originalClip = startupClip.GetValue(audio) as AudioClip
                    });
                    // VPAudioExtras may create its startup source on initialization.
                    // Remove the clip before that callback, not after sound starts.
                    startupClip.SetValue(audio, null);
                }
            }
        }

        public void SetArrivalFuel(float ratio) => fuelRatio = Mathf.Clamp01(ratio);

        public void HoldArrivalParking()
        {
            if (vehicleBody == null || arrivalParkingHeld) return;
            drivingConstraints = vehicleBody.constraints;
            vehicleBody.linearVelocity = Vector3.zero;
            vehicleBody.angularVelocity = Vector3.zero;
            // Preserve the exact settled pose while the player gets out.
            vehicleBody.constraints = RigidbodyConstraints.FreezeAll;
            arrivalParkingHeld = true;
        }

        public void SetVehicleReady(bool ready)
        {
            vehicleReady = ready;
            if (ready) return;
            driving = false;
            throttleInput = 0f;
            steerInput = 0f;
        }

        public void SetIntroFullThrottle(bool value)
        {
            introFullThrottle = value;
        }

        public void EnterCar()
        {
            if (!vehicleReady || driving || gameFlow == null || !gameFlow.carHasFuel || gameFlow.gameOver || gameFlow.finished) return;
            if (vehicleBody != null && arrivalParkingHeld)
            {
                vehicleBody.constraints = drivingConstraints;
                arrivalParkingHeld = false;
                vehicleBody.WakeUp();
            }
            if (vehicleBody != null && vehicleBody.isKinematic)
            {
                vehicleBody.isKinematic = false;
                vehicleBody.detectCollisions = true;
                vehicleBody.linearVelocity = Vector3.zero;
                vehicleBody.angularVelocity = Vector3.zero;
                vehicleBody.WakeUp();
            }
            driving = true;
            SetNativeControls(true);
            fuelRatio = 1f;
            if (introEngine != null) introEngine.allowedFuelRatio = 1f;
            RestoreStartupAudioClip();
            StartEngineAudio(true);
            if (player != null) player.SetActive(false);
            CinemachineFollowSetup cameraRig = FindFirstObjectByType<CinemachineFollowSetup>();
            if (cameraRig != null) cameraRig.SetTarget(transform);
            else if (cameraFollow != null) cameraFollow.SetTarget(transform);
            gameFlow.StartDriving();
        }

        private void Update()
        {
            if (restoreStartupAudioAt >= 0f && Time.time >= restoreStartupAudioAt)
                RestoreStartupAudioClip();

            if (gameFlow != null && (gameFlow.gameOver || gameFlow.finished))
            {
                StopEngineAudio();
                driving = false;
                SetNativeControls(false);
                return;
            }
            throttleInput = 0f;
            steerInput = 0f;
            brakeInput = 0f;
            if ((!driving && !arrivalControl) || Keyboard.current == null) return;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) throttleInput = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) throttleInput = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) steerInput = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) steerInput = 1f;

            // S/down is a brake while the car is moving forward. It only becomes
            // reverse after the car is practically stopped; this avoids sending
            // throttle and brake at the same time.
            if (throttleInput < 0f && vehicleBody != null &&
                Vector3.Dot(Vector3.ProjectOnPlane(vehicleBody.linearVelocity, Vector3.up),
                    Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized) > 0.5f)
            {
                brakeInput = 1f;
            }
            if (Keyboard.current.spaceKey.isPressed) brakeInput = 1f;
            if (vehicleController == null)
            {
                transform.Rotate(0f, steerInput * turnSpeed * Time.deltaTime, 0f);
                transform.Translate(Vector3.forward * throttleInput * speed * Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (vehicleController == null) return;
            if (!vehicleController.initialized)
            {
                // Apply before VPP initialization as well: this project car prefab
                // is authored with a manual gearbox, but gameplay requires automatic.
                ConfigureFrontSteeringOnly();
                vehicleController.gearbox.type = Gearbox.Type.Automatic;
                return;
            }

            if (!automaticTransmissionConfigured)
            {
                ConfigureFrontSteeringOnly();
                vehicleController.gearbox.type = Gearbox.Type.Automatic;
                automaticTransmissionConfigured = true;
            }

            if (introEngine == null)
            {
                foreach (FieldInfo field in typeof(VPVehicleController).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                    if (field.FieldType == typeof(Engine)) introEngine = field.GetValue(vehicleController) as Engine;
            }
            if (introEngine != null)
            {
                if (seedRunningEngine)
                {
                    Engine.StateVars state = introEngine.GetStateVars();
                    state.L = 2800f * (2f * Mathf.PI / 60f) * vehicleController.engine.inertia;
                    introEngine.SetStateVars(state);
                    seedRunningEngine = false;
                }
                introEngine.allowedFuelRatio = fuelRatio;
            }

            int[] input = vehicleController.data.Get(Channel.Input);
            bool stopped = gameFlow != null && (gameFlow.gameOver || gameFlow.finished);
            bool engineOn = engineRequested && !stopped;
            // VPP bus Key: -1 = off, 0 = ignition on, 1 = starter motor.
            input[InputData.Key] = !engineOn ? -1 : Time.time < starterUntil ? 1 : 0;
            // Send only normalized driver commands to VPP. VPP continues to own
            // the wheels, suspension, tires, steering rack and automatic gearbox.
            int targetSteer = driving || arrivalControl ? Mathf.RoundToInt(steerInput * 10000f) : 0;
            input[InputData.Steer] = Mathf.RoundToInt(Mathf.MoveTowards(input[InputData.Steer], targetSteer, 20000f * Time.fixedDeltaTime));
            Vector3 planarForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            float forwardSpeed = vehicleBody != null
                ? Vector3.Dot(Vector3.ProjectOnPlane(vehicleBody.linearVelocity, Vector3.up), planarForward)
                : 0f;
            bool reversing = (driving || arrivalControl) && throttleInput < -0.05f && forwardSpeed < 0.5f;
            float throttle = driving || arrivalControl
                ? reversing ? Mathf.Abs(throttleInput) : Mathf.Max(0f, throttleInput)
                : !vehicleReady && introFullThrottle ? 0.5f : 0f;
            input[InputData.Throttle] = engineOn ? Mathf.RoundToInt(throttle * 10000f) : 0;
            input[InputData.Brake] = (!driving && !arrivalControl && vehicleReady) || stopped
                ? 10000
                : Mathf.RoundToInt(brakeInput * 10000f);
            input[InputData.Handbrake] = !driving && vehicleReady ? 10000 : 0;
            input[InputData.Clutch] = arrivalControl && fuelRatio <= 0f ? 10000 : 0;
            input[InputData.ManualGear] = 0;
            // VPP automatic modes: P=1, R=2, N=3, D=4. The scripted arrival
            // stays in Drive even after fuel starvation, so the motor cuts out
            // while the car visibly coasts instead of dropping to Neutral.
            input[InputData.AutomaticGear] = driving ? (reversing ? 2 : 4) : vehicleReady ? 1 : 4;
        }

        public void StartEngineAudio(bool playStartSound)
        {
            if (!engineRequested)
            {
                // Player-controlled starts use VPP's starter. The intro instead
                // seeds the running engine state and never sends the starter key.
                starterUntil = Time.time + 1.25f;
                if (!playStartSound)
                {
                    SetStartupAudioClip(null);
                    starterUntil = -1f;
                    seedRunningEngine = true;
                }
            }
            engineRequested = true;
        }

        public void StopEngineAudio()
        {
            engineRequested = false;
            introFullThrottle = false;
            throttleInput = 0f;
            steerInput = 0f;
        }

        private void SetStartupAudioClip(AudioClip clip)
        {
            foreach (StartupAudioClip entry in startupAudioClips)
            {
                if (entry.audioComponent != null && entry.clipField != null)
                    entry.clipField.SetValue(entry.audioComponent, clip);
            }
        }

        private void RestoreStartupAudioClip()
        {
            foreach (StartupAudioClip entry in startupAudioClips)
            {
                if (entry.audioComponent != null && entry.clipField != null)
                    entry.clipField.SetValue(entry.audioComponent, entry.originalClip);
            }
            restoreStartupAudioAt = -1f;
        }
    }
}
