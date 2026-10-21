using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using VehiclePhysics;

namespace EnElCamino
{
    [DefaultExecutionOrder(-100)]
    public class CarDriver : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private GameObject player;
        [SerializeField] private CamaraJugador cameraFollow;
        [SerializeField] private Transform stopPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private float cruiseSpeed = 12f;
        [SerializeField] private float coastDistance = 180f;
        [SerializeField] private float fuelStarvationDuration = 1.2f;
        [SerializeField] private Surtidor fuelCan;

        private VPVehicleController vehicleController;
        private bool driving;
        private bool vehicleReady = true;
        private bool engineRequested;
        private float starterUntil;
        private float throttleInput;
        private float steerInput;
        private bool introFullThrottle;
        private readonly List<StartupAudioClip> startupAudioClips = new List<StartupAudioClip>();
        private Rigidbody vehicleBody;
        private Engine introEngine;
        private bool seedRunningEngine;
        private float fuelRatio = 1f;
        private bool arrivalParkingHeld;
        private RigidbodyConstraints drivingConstraints;
        private bool arrivalControl;
        private float brakeInput;
        private VPStandardInput nativeInput;
        private bool used;

        private void SetNativeControls(bool enabled)
        {
            if (nativeInput == null) return;
            nativeInput.externalThrottle = 0f;
            nativeInput.externalBrake = 0f;
            nativeInput.externalHandbrake = 0f;
            nativeInput.externalSteer = 0f;
            nativeInput.externalClutch = 0f;
            nativeInput.lockBrakesOnEnable = false;
            // Do not overwrite any of the steering or pedal settings authored
            // in Vehicle Physics Pro. VPStandardInput is the sole owner of the
            // controls after the player gets in the car.
            // Keep VPStandardInput alive for the whole scene. Disabling and
            // re-enabling it after the scripted arrival can leave VPP's input
            // registration stale, which makes the car accelerate but not steer.
            nativeInput.enabled = true;
            nativeInput.disableSteerInput = !enabled;
            nativeInput.disableThrottleInput = !enabled;
            nativeInput.disableBrakeInput = !enabled;
            nativeInput.disableHandbrakeInput = !enabled;
            nativeInput.disableClutchInput = !enabled;
            nativeInput.disableGearShiftInputs = !enabled;

            // The isolated VPP test uses the asset's automatic transmission.
            // Apply the same setting only when player control is handed over;
            // the scripted arrival keeps its own drive/coast behavior.
            if (enabled && vehicleController != null)
                vehicleController.gearbox.type = Gearbox.Type.Automatic;

            if (!enabled)
            {
                nativeInput.externalIgnition = VPStandardInput.IgnitionKey.Off;
                return;
            }

            nativeInput.externalIgnition = engineRequested
                ? Time.time < starterUntil ? VPStandardInput.IgnitionKey.Start : VPStandardInput.IgnitionKey.AccOn
                : VPStandardInput.IgnitionKey.Off;

            if (vehicleController == null || !vehicleController.initialized) return;

            int[] input = vehicleController.data.Get(Channel.Input);
            input[InputData.Brake] = 0;
            input[InputData.Handbrake] = 0;
            input[InputData.Clutch] = 0;
            input[InputData.Steer] = 0;
            input[InputData.AutomaticGear] = 4;
        }

        private void PrepareCoastControls()
        {
            if (nativeInput != null)
            {
                // Keep the VPP input component alive, but let this small bridge
                // be the only writer of the driver input bus. VPP still owns
                // the engine, wheels, suspension and steering geometry.
                nativeInput.enabled = true;
                nativeInput.disableSteerInput = true;
                nativeInput.disableThrottleInput = true;
                nativeInput.disableBrakeInput = true;
                nativeInput.disableHandbrakeInput = true;
                nativeInput.disableClutchInput = true;
                nativeInput.disableGearShiftInputs = true;
                nativeInput.externalThrottle = 0f;
                nativeInput.externalBrake = 0f;
                nativeInput.externalHandbrake = 0f;
                nativeInput.externalSteer = 0f;
                nativeInput.externalClutch = 0f;
                nativeInput.externalIgnition = engineRequested
                    ? Time.time < starterUntil ? VPStandardInput.IgnitionKey.Start : VPStandardInput.IgnitionKey.AccOn
                    : VPStandardInput.IgnitionKey.Off;
            }

            if (vehicleController == null || !vehicleController.initialized) return;

            int[] input = vehicleController.data.Get(Channel.Input);
            input[InputData.Throttle] = 0;
            input[InputData.Steer] = 0;
            input[InputData.Brake] = 0;
            input[InputData.Handbrake] = 0;
            input[InputData.Clutch] = 0;
            input[InputData.ManualGear] = 0;
            input[InputData.AutomaticGear] = 4;
        }

        public void BeginFuelCutoffControl()
        {
            arrivalControl = true;
            introFullThrottle = false;
            PrepareCoastControls();
        }

        public void EndFuelCutoffControl()
        {
            arrivalControl = false;
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
            nativeInput = GetComponent<VPStandardInput>();
            if (nativeInput == null) nativeInput = gameObject.AddComponent<VPStandardInput>();
            // Keep the prefab's native VPP input untouched during the arrival.
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

        public string Prompt
        {
            get
            {
                if (!vehicleReady) return "El auto está llegando...";
                if (used) return "[E] Subir al auto";
                return gameFlow != null && gameFlow.fuelCanFull
                    ? "[E] Cargar tanque del auto"
                    : "Falta llenar el bidón";
            }
        }

        public void Interact()
        {
            if (gameFlow == null || gameFlow.gameOver || gameFlow.finished) return;
            if (used)
            {
                EnterCar();
                return;
            }
            if (!gameFlow.fuelCanFull)
            {
                gameFlow.ShowMessage("Primero llená el bidón en el surtidor.");
                return;
            }
            used = true;
            gameFlow.FillCar();
            if (fuelCan != null) fuelCan.Use();
        }

        private IEnumerator Start()
        {
            SetVehicleReady(false);
            SetIntroFullThrottle(true);
            StartEngineAudio(false);
            if (gameFlow != null) gameFlow.movementLocked = true;
            if (player != null) player.SetActive(false);
            CamaraJugador legacyCamera = cameraFollow != null
                ? cameraFollow
                : FindFirstObjectByType<CamaraJugador>(FindObjectsInactive.Include);
            if (legacyCamera != null) legacyCamera.SetTarget(transform);

            if (vehicleBody == null) vehicleBody = GetComponent<Rigidbody>();
            if (vehicleController == null) vehicleController = GetComponent<VPVehicleController>();
            if (vehicleBody == null) yield break;
            vehicleBody.isKinematic = false;
            vehicleBody.detectCollisions = true;
            Vector3 destination = stopPoint != null ? stopPoint.position : transform.position;
            Vector3 laneOrigin = vehicleBody.position;
            GameObject road = GameObject.Find("Ruta");
            Vector3 direction = Vector3.ProjectOnPlane(road != null ? road.transform.forward : transform.forward, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
            if (Vector3.Dot(destination - laneOrigin, direction) < 0f) direction = -direction;
            destination = laneOrigin + direction * Mathf.Max(0f, Vector3.Dot(destination - laneOrigin, direction));
            float fuelCutoffDistance = Mathf.Min(coastDistance, Vector3.Distance(laneOrigin, destination) * 0.8f);
            RigidbodyConstraints originalConstraints = vehicleBody.constraints;
            vehicleBody.rotation = Quaternion.LookRotation(direction, Vector3.up);
            vehicleBody.constraints = originalConstraints | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            vehicleBody.linearVelocity = direction * cruiseSpeed;
            vehicleBody.WakeUp();
            while (vehicleController != null && !vehicleController.initialized) yield return new WaitForFixedUpdate();

            bool starved = false;
            bool engineStopped = false;
            float starvationTime = 0f;
            while (true)
            {
                yield return new WaitForFixedUpdate();
                if (gameFlow != null && (gameFlow.gameOver || gameFlow.finished)) yield break;
                Vector3 horizontal = Vector3.ProjectOnPlane(vehicleBody.linearVelocity, Vector3.up);
                float remaining = Vector3.Dot(destination - vehicleBody.position, direction);
                float currentSpeed = Mathf.Max(0f, Vector3.Dot(horizontal, direction));
                if (!starved && remaining <= fuelCutoffDistance)
                {
                    starved = true;
                    vehicleBody.constraints = originalConstraints;
                    BeginFuelCutoffControl();
                    if (gameFlow != null) gameFlow.BeginAtFuelCutoff();
                }
                if (starved)
                {
                    starvationTime += Time.fixedDeltaTime;
                    float fuel = 1f - Mathf.Clamp01(starvationTime / Mathf.Max(0.1f, fuelStarvationDuration));
                    SetArrivalFuel(fuel);
                    if (fuel <= 0f && !engineStopped)
                    {
                        StopEngineAudio();
                        engineStopped = true;
                    }
                    if (fuel <= 0f && horizontal.magnitude <= 0.25f) break;
                    continue;
                }
                float desiredSpeed = Mathf.Min(cruiseSpeed, Mathf.Max(0f, remaining) / Time.fixedDeltaTime);
                float longitudinalSpeed = Vector3.Dot(horizontal, direction);
                vehicleBody.AddForce(direction * (desiredSpeed - longitudinalSpeed) / Time.fixedDeltaTime, ForceMode.Acceleration);
                if (remaining <= 0.12f && currentSpeed <= 0.35f) break;
            }

            SetArrivalFuel(0f);
            StopEngineAudio();
            EndFuelCutoffControl();
            SetVehicleReady(true);
            float settle = 0f;
            while (settle < 0.5f)
            {
                settle += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            vehicleBody.constraints = originalConstraints;
            HoldArrivalParking();
            enabled = false;
            if (player != null)
            {
                PlacePlayerBesideCar();
                player.SetActive(true);
            }
            if (gameFlow != null)
            {
                gameFlow.movementLocked = false;
                gameFlow.ShowMessage("El auto se quedó sin combustible. Acercate a la estación abandonada.");
            }
            if (legacyCamera != null && player != null) legacyCamera.SetTarget(player.transform);
        }

        private void PlacePlayerBesideCar()
        {
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            foreach (Collider item in GetComponentsInChildren<Collider>())
                if (item.enabled && !item.isTrigger) bounds.Encapsulate(item.bounds);
            Vector3 side = Vector3.ProjectOnPlane(-transform.right, Vector3.up).normalized;
            CharacterController controller = player.GetComponent<CharacterController>();
            float radius = controller != null ? controller.radius * Mathf.Abs(player.transform.lossyScale.x) : 0.4f;
            float halfWidth = Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(side.x), 0f, Mathf.Abs(side.z)));
            Vector3 position = bounds.center + side * (halfWidth + radius + 0.65f);
            RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * 5f, Vector3.down, 30f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float ground = float.NegativeInfinity;
            foreach (RaycastHit hit in hits)
            {
                if (hit.normal.y < 0.5f || hit.transform == transform || hit.transform.IsChildOf(transform) ||
                    hit.transform == player.transform || hit.transform.IsChildOf(player.transform)) continue;
                ground = Mathf.Max(ground, hit.point.y);
            }
            if (!float.IsNegativeInfinity(ground))
            {
                float footOffset = controller != null
                    ? (controller.center.y - controller.height * 0.5f) * player.transform.lossyScale.y : 0f;
                position.y = ground - footOffset + 0.04f;
            }
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            player.transform.SetPositionAndRotation(position, exitPoint != null ? exitPoint.rotation : transform.rotation);
            PlayerController movement = player.GetComponent<PlayerController>();
            if (movement != null) movement.ResetFallTracking();
            if (controller != null) controller.enabled = wasEnabled;
        }

        public void SetArrivalFuel(float ratio) => fuelRatio = Mathf.Clamp01(ratio);

        public void RestoreFuelForNativeControl()
        {
            fuelRatio = 1f;
            if (vehicleController == null) return;
            if (introEngine == null)
            {
                foreach (FieldInfo field in typeof(VPVehicleController).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (field.FieldType == typeof(Engine))
                    {
                        introEngine = field.GetValue(vehicleController) as Engine;
                        break;
                    }
                }
            }
            if (introEngine != null) introEngine.allowedFuelRatio = 1f;
        }

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
            // From this point on the car is controlled by Vehicle Physics Pro.
            // The arrival/coast bridge is only for the scripted fuel cut-off;
            // after refuelling VPP must receive its own steering and gearbox
            // state again.
            driving = true;
            arrivalControl = false;
            if (vehicleBody != null)
            {
                // Restore the prefab's original constraints. Releasing every
                // axis here made the parked car slide/rotate as a solid block
                // after refuelling, unlike during the initial coast.
                vehicleBody.constraints = drivingConstraints;
                vehicleBody.angularVelocity = Vector3.zero;
                vehicleBody.WakeUp();
            }
            RestoreFuelForNativeControl();
            RestoreStartupAudioClip();
            StartEngineAudio(true);
            RearmNativeVehicleController();
            if (player != null) player.SetActive(false);
            CamaraJugador legacyCamera = cameraFollow != null
                ? cameraFollow
                : FindFirstObjectByType<CamaraJugador>(FindObjectsInactive.Include);
            if (legacyCamera != null)
            {
                legacyCamera.enabled = true;
                legacyCamera.SetVehicleTarget(transform);
                legacyCamera.SnapToTarget();
            }
            gameFlow.StartDriving();

            // The arrival sequence disabled this component while the car was
            // parked. Re-enable it so the VPP bus receives the player controls.
            enabled = true;
        }

        private void RearmNativeVehicleController()
        {
            if (nativeInput != null)
                nativeInput.enabled = false;
            if (vehicleController != null)
                vehicleController.enabled = false;
            StartCoroutine(EnableNativeVehicleController());
        }

        private IEnumerator EnableNativeVehicleController()
        {
            // Let VPP run its disable callbacks first. This clears the previous
            // stalled input/gear state instead of carrying it into the new trip.
            yield return null;

            if (vehicleController != null)
            {
                vehicleController.gearbox.type = Gearbox.Type.Automatic;
                vehicleController.enabled = true;
            }
            if (nativeInput != null)
            {
                nativeInput.enabled = true;
                SetNativeControls(true);
            }
        }

        private void Update()
        {
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
            if (!driving && !arrivalControl) return;
            if (driving)
            {
                // Keep VPP as the only system that applies forces and turns the
                // front wheels. The asset reads the old Unity input axes, while
                // this project also uses the new Input System for the player;
                // feed only the missing steering value through VPP's supported
                // external input, instead of rotating the car transform.
                if (Keyboard.current == null)
                {
                    if (nativeInput != null)
                    {
                        nativeInput.externalThrottle = 0f;
                        nativeInput.externalSteer = 0f;
                        nativeInput.externalBrake = 0f;
                        nativeInput.reverse = false;
                    }
                    return;
                }

                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) steerInput = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) steerInput = 1f;
                if (Keyboard.current.spaceKey.isPressed) brakeInput = 1f;

                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                    brakeInput = 1f;

                if (nativeInput != null)
                {
                    bool reverse = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;
                    bool forward = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed;
                    nativeInput.externalSteer = steerInput;
                    nativeInput.externalThrottle = forward || reverse ? 1f : 0f;
                    nativeInput.externalBrake = brakeInput;
                    nativeInput.reverse = reverse && !forward;
                    nativeInput.externalIgnition = engineRequested
                        ? Time.time < starterUntil ? VPStandardInput.IgnitionKey.Start : VPStandardInput.IgnitionKey.AccOn
                        : VPStandardInput.IgnitionKey.Off;
                }
                return;
            }
            if (Keyboard.current == null) return;
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
        }

        private void FixedUpdate()
        {
            if (vehicleController == null) return;
            if (!vehicleController.initialized)
                return;

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

            // During player driving VPStandardInput is the only writer of VPP's
            // input channel. Mixing this scripted bridge with it makes the car
            // steer as a rigid block after refuelling.
            if (driving)
                return;

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
            input[InputData.Handbrake] = !driving && !arrivalControl && vehicleReady ? 10000 : 0;
            input[InputData.Clutch] = arrivalControl && fuelRatio <= 0f ? 10000 : 0;
            input[InputData.ManualGear] = 0;
            // VPP automatic modes: P=1, R=2, N=3, D=4. The scripted arrival
            // stays in Drive even after fuel starvation, so the motor cuts out
            // while the car visibly coasts instead of dropping to Neutral.
            input[InputData.AutomaticGear] = driving || arrivalControl
                ? (reversing ? 2 : 4)
                : vehicleReady ? 1 : 4;
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
        }
    }
}
