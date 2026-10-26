using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using VehiclePhysics;
using VersionCompatibility;

namespace EnElCamino
{
    [DefaultExecutionOrder(-100)]
    public class AutoJugador : MonoBehaviour, IInteractable
    {
        [SerializeField] private Juego gameFlow;
        [SerializeField] private GameObject player;
        [SerializeField] private CamaraJugador cameraFollow;
        [SerializeField] private Transform stopPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private float cruiseSpeed = 12f;
        [SerializeField] private float coastDistance = 180f;
        [SerializeField] private float fuelStarvationDuration = 1.2f;
        [SerializeField] private Surtidor fuelCan;
        [SerializeField] private ParticleSystem dustParticles;
        [SerializeField] private ParticleSystem impactParticles;
        [SerializeField] private ParticleSystem[] exhaustParticles = System.Array.Empty<ParticleSystem>();

        private VPVehicleController vehicleController;
        private bool driving;
        private bool vehicleReady = true;
        private bool engineRequested;
        private float starterUntil;
        private float stalledTime;
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
        private UnityKey[] originalVppHandbrakeKeys;
        private bool originalVppHandbrakeLegacyMode;
        private bool used;
        private float originalTireGrip;
        private bool originalTireGripSaved;
        private readonly RaycastHit[] surfaceHits = new RaycastHit[16];
        private static readonly Vector3[] grassProbeOffsets =
        {
            new Vector3(-0.65f, 0f, -1.25f), new Vector3(0.65f, 0f, -1.25f),
            new Vector3(-0.65f, 0f, 1.25f), new Vector3(0.65f, 0f, 1.25f)
        };
        private const float GrassGripMultiplier = 0.45f;
        private float grassBumpDistance;
        private int grassBumpWheel;
        private float damage;
        private float nextDamageTime;
        public float DamageHudUntil { get; private set; }
        public float Damage => damage;

        private void OnCollisionEnter(Collision collision)
        {
            if (!HasPlayerControl || gameFlow == null || gameFlow.gameOver || gameFlow.finished ||
                vehicleBody == null || Time.time < nextDamageTime) return;
            float impact = 0f;
            foreach (ContactPoint contact in collision.contacts)
            {
                if (Mathf.Abs(contact.normal.y) > 0.65f) continue;
                impact = Mathf.Max(impact, Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal)));
            }
            if (impact < 3f) return;
            if (impactParticles != null && collision.contactCount > 0)
            {
                impactParticles.transform.position = collision.GetContact(0).point;
                impactParticles.Play();
            }
            nextDamageTime = Time.time + 0.35f;
            damage = Mathf.Min(100f, damage + Mathf.Clamp((impact - 3f) * 2f, 0f, 65f));
            DamageHudUntil = Time.time + 5f;
            if (damage >= 100f) gameFlow.GameOver();
        }

        private void SetNativeControls(bool enabled)
        {
            if (nativeInput == null) return;
            nativeInput.externalThrottle = 0f;
            nativeInput.externalBrake = 0f;
            nativeInput.externalHandbrake = 0f;
            nativeInput.externalSteer = 0f;
            nativeInput.externalClutch = 0f;
            nativeInput.lockBrakesOnEnable = false;
            nativeInput.enabled = true;
            nativeInput.disableSteerInput = !enabled;
            nativeInput.disableThrottleInput = !enabled;
            nativeInput.disableBrakeInput = !enabled;
            nativeInput.disableHandbrakeInput = !enabled;

            if (enabled && vehicleController != null)
            {
                vehicleController.gearbox.type = Gearbox.Type.Automatic;
                nativeInput.throttleAndBrakeMode = VPStandardInput.ThrottleAndBrakeMode.AutoForwardAndReverse;
                nativeInput.brakeOnThrottleBackwards = true;
                nativeInput.reverse = false;
            }

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
                nativeInput.enabled = true;
                nativeInput.disableSteerInput = true;
                nativeInput.disableThrottleInput = true;
                nativeInput.disableBrakeInput = true;
                nativeInput.disableHandbrakeInput = true;
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
            public VPAudioExtras audioComponent;
            public AudioClip originalClip;
        }

        public bool IsReady => vehicleReady;
        public bool HasPlayerControl => driving || arrivalControl;
        public bool CanInteract => vehicleReady &&
            (used || gameFlow != null && gameFlow.fuelCanFull);
        public float SpeedKmh
        {
            get
            {
                if (vehicleBody == null) return 0f;
                return Vector3.ProjectOnPlane(vehicleBody.linearVelocity, Vector3.up).magnitude * 3.6f;
            }
        }

        private void Awake()
        {
            DisableVppShortcuts();
            if (gameFlow == null) gameFlow = FindFirstObjectByType<Juego>();
            vehicleController = GetComponent<VPVehicleController>();
            vehicleBody = GetComponent<Rigidbody>();
            if (vehicleController == null) return;
            nativeInput = GetComponent<VPStandardInput>();
            vehicleReady = false;
            foreach (VPAudioExtras audio in GetComponentsInChildren<VPAudioExtras>(true))
            {
                startupAudioClips.Add(new StartupAudioClip { audioComponent = audio, originalClip = audio.engineStartupClip });
                audio.engineStartupClip = null;
            }
        }

        private void DisableVppShortcuts()
        {
            UnityAxisControl axis = UnityInput.handbrakeAxis;
            originalVppHandbrakeKeys = new[] { axis.negativeButton, axis.positiveButton,
                axis.altNegativeButton, axis.altPositiveButton };
            originalVppHandbrakeLegacyMode = axis.legacyMode;
            axis.negativeButton = axis.positiveButton = UnityKey.None;
            axis.altNegativeButton = axis.altPositiveButton = UnityKey.None;
            axis.legacyMode = false;
        }

        private void OnDestroy()
        {
            if (originalVppHandbrakeKeys == null) return;
            UnityAxisControl axis = UnityInput.handbrakeAxis;
            axis.negativeButton = originalVppHandbrakeKeys[0];
            axis.positiveButton = originalVppHandbrakeKeys[1];
            axis.altNegativeButton = originalVppHandbrakeKeys[2];
            axis.altPositiveButton = originalVppHandbrakeKeys[3];
            axis.legacyMode = originalVppHandbrakeLegacyMode;
        }

        public void SetHeadLights(bool enabled)
        {
            VehicleLightUtility.SetHeadlights(gameObject, enabled);
        }

        public string Prompt
        {
            get
            {
                if (!vehicleReady) return "El auto esta llegando...";
                if (used) return "[E] Subir al auto";
                return gameFlow != null && gameFlow.fuelCanFull
                    ? "[E] Cargar tanque del auto"
                    : string.Empty;
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
                return;
            used = true;
            gameFlow.FillCar();
            if (fuelCan != null) fuelCan.Use();
        }

        private IEnumerator Start()
        {
            SetVehicleReady(false);
            introFullThrottle = true;
            StartEngineAudio(false);
            if (gameFlow != null) gameFlow.movementLocked = true;
            if (player != null) player.SetActive(false);
            if (cameraFollow != null) cameraFollow.SetTarget(transform);

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
                        if (gameFlow != null) gameFlow.ShowFuelCutoffMessage();
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
                gameFlow.ShowInitialMission();
            }
            if (cameraFollow != null && player != null) cameraFollow.SetTarget(player.transform);
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

        private void FindEngine()
        {
            if (introEngine != null || vehicleController == null) return;
            foreach (FieldInfo field in typeof(VPVehicleController).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                if (field.FieldType == typeof(Engine))
                {
                    introEngine = field.GetValue(vehicleController) as Engine;
                    break;
                }
        }

        public void RestoreFuelForNativeControl()
        {
            fuelRatio = 1f;
            if (vehicleController == null) return;
            FindEngine();
            if (introEngine != null) introEngine.allowedFuelRatio = 1f;
        }

        public void HoldArrivalParking()
        {
            if (vehicleBody == null || arrivalParkingHeld) return;
            drivingConstraints = vehicleBody.constraints;
            vehicleBody.linearVelocity = Vector3.zero;
            vehicleBody.angularVelocity = Vector3.zero;
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
            arrivalControl = false;
            if (vehicleBody != null)
            {
                vehicleBody.constraints = drivingConstraints;
                vehicleBody.angularVelocity = Vector3.zero;
                vehicleBody.WakeUp();
            }
            RestoreFuelForNativeControl();
            RestoreStartupAudioClip();
            StartEngineAudio(true);
            RearmNativeVehicleController();
            if (player != null) player.SetActive(false);
            if (cameraFollow != null)
            {
                cameraFollow.enabled = true;
                cameraFollow.SetVehicleTarget(transform);
                cameraFollow.SnapToTarget();
            }
            gameFlow.StartDriving();

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
                if (Keyboard.current == null)
                {
                    if (nativeInput != null)
                    {
                        nativeInput.externalThrottle = 0f;
                        nativeInput.externalSteer = 0f;
                        nativeInput.externalBrake = 0f;
                        nativeInput.externalHandbrake = 0f;
                        nativeInput.reverse = false;
                    }
                    return;
                }

                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) steerInput = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) steerInput = 1f;

                if (nativeInput != null)
                {
                    bool reverse = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;
                    bool forward = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed;
                    nativeInput.externalSteer = steerInput;
                    nativeInput.externalThrottle = forward && !reverse ? 1f : 0f;
                    nativeInput.externalBrake = reverse && !forward ? 1f : 0f;
                    nativeInput.externalHandbrake = Keyboard.current.spaceKey.isPressed ? 1f : 0f;
                    nativeInput.reverse = false;
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

            int[] auxiliaryInput = vehicleController.data.Get(Channel.Input);
            auxiliaryInput[InputData.Retarder] = 0;
            auxiliaryInput[InputData.Clutch] = 0;
            auxiliaryInput[InputData.GearShift] = 0;

            UpdateGripOnGrass();
            if (HasPlayerControl)
                GrassDriving.ApplyBumps(vehicleBody, gameFlow, ref grassBumpDistance, ref grassBumpWheel);

            FindEngine();
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

            if (driving)
            {
                RestartStalledEngine();
                return;
            }

            int[] input = vehicleController.data.Get(Channel.Input);
            bool stopped = gameFlow != null && (gameFlow.gameOver || gameFlow.finished);
            bool engineOn = engineRequested && !stopped;
            input[InputData.Key] = !engineOn ? -1 : Time.time < starterUntil ? 1 : 0;
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
            input[InputData.AutomaticGear] = driving || arrivalControl
                ? (reversing ? 2 : 4)
                : vehicleReady ? 1 : 4;
        }

        private void RestartStalledEngine()
        {
            if (!engineRequested || gameFlow == null || !gameFlow.carHasFuel ||
                gameFlow.gameOver || gameFlow.finished || damage >= 100f || nativeInput == null ||
                Time.time < starterUntil + 0.75f)
            {
                stalledTime = 0f;
                return;
            }
            int[] telemetry = vehicleController.data.Get(Channel.Vehicle);
            bool stalled = telemetry[VehicleData.EngineStalled] != 0 ||
                (telemetry[VehicleData.EngineWorking] == 0 && telemetry[VehicleData.EngineRpm] == 0);
            if (!stalled || telemetry[VehicleData.EngineStarting] != 0)
            {
                stalledTime = 0f;
                return;
            }
            stalledTime += Time.fixedDeltaTime;
            if (stalledTime < 0.35f) return;
            stalledTime = 0f;
            starterUntil = Time.time + 1.25f;
            nativeInput.externalIgnition = VPStandardInput.IgnitionKey.Start;
        }

        private void UpdateGripOnGrass()
        {
            bool onGrass = IsCarOverGrass();
            UpdateDust(onGrass);
            var friction = vehicleController.tireFriction;
            if (!originalTireGripSaved)
            {
                originalTireGrip = friction.frictionMultiplier;
                originalTireGripSaved = true;
            }
            float grip = originalTireGrip * (onGrass ? GrassGripMultiplier : 1f);
            if (Mathf.Approximately(friction.frictionMultiplier, grip)) return;
            friction.frictionMultiplier = grip;
            friction.SetupFrictionCurves();
            vehicleController.tireFriction = friction;
        }

        private void UpdateDust(bool onGrass)
        {
            if (dustParticles == null) return;
            var emission = dustParticles.emission;
            emission.enabled = HasPlayerControl && SpeedKmh > 10f && onGrass;
        }

        private bool IsCarOverGrass()
        {
            if (gameFlow == null) return false;

            foreach (Vector3 offset in grassProbeOffsets)
            {
                Vector3 origin = transform.TransformPoint(offset) + Vector3.up * 2.5f;
                int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, surfaceHits, 6f,
                    ~0, QueryTriggerInteraction.Ignore);
                float nearestDistance = float.MaxValue;
                Collider nearestCollider = null;

                for (int i = 0; i < hitCount; i++)
                {
                    Collider collider = surfaceHits[i].collider;
                    if (collider == null || collider.transform == transform || collider.transform.IsChildOf(transform))
                        continue;
                    if (surfaceHits[i].distance >= nearestDistance) continue;

                    nearestDistance = surfaceHits[i].distance;
                    nearestCollider = collider;
                }

                if (gameFlow.IsGrassSurface(nearestCollider)) return true;
            }

            return false;
        }

        public void StartEngineAudio(bool playStartSound)
        {
            if (!engineRequested)
            {
                starterUntil = Time.time + 1.25f;
                if (!playStartSound)
                {
                    SetStartupAudioClip(null);
                    starterUntil = -1f;
                    seedRunningEngine = true;
                }
            }
            engineRequested = true;
            SetExhaust(true);
        }

        public void StopEngineAudio()
        {
            engineRequested = false;
            SetExhaust(false);
            introFullThrottle = false;
            throttleInput = 0f;
            steerInput = 0f;
        }

        private void SetExhaust(bool running)
        {
            foreach (ParticleSystem smoke in exhaustParticles)
            {
                if (smoke == null) continue;
                var emission = smoke.emission;
                emission.enabled = running;
            }
        }

        private void SetStartupAudioClip(AudioClip clip)
        {
            foreach (StartupAudioClip entry in startupAudioClips)
            {
                if (entry.audioComponent != null)
                    entry.audioComponent.engineStartupClip = clip;
            }
        }

        private void RestoreStartupAudioClip()
        {
            foreach (StartupAudioClip entry in startupAudioClips)
            {
                if (entry.audioComponent != null)
                    entry.audioComponent.engineStartupClip = entry.originalClip;
            }
        }
    }

    internal static class GrassDriving
    {
        private static readonly RaycastHit[] hits = new RaycastHit[16];
        private static readonly Vector3[] wheelOffsets =
        {
            new Vector3(-0.75f, 0f, 1.65f), new Vector3(0.75f, 0f, 1.65f),
            new Vector3(-0.75f, 0f, -0.85f), new Vector3(0.75f, 0f, -0.85f)
        };

        public static void ApplyBumps(Rigidbody body, Juego flow, ref float distance, ref int nextWheel)
        {
            if (body == null || body.isKinematic || flow == null || flow.finished || flow.gameOver) return;
            float speed = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude;
            if (speed < 3f) { distance = 0f; return; }
            for (int probe = 0; probe < wheelOffsets.Length; probe++)
            {
                int wheel = (nextWheel + probe) % wheelOffsets.Length;
                Vector3 point = body.transform.TransformPoint(wheelOffsets[wheel]);
                int count = Physics.RaycastNonAlloc(point + Vector3.up * 0.6f, Vector3.down,
                    hits, 0.9f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                float nearest = float.PositiveInfinity;
                RaycastHit ground = default;
                for (int i = 0; i < count; i++)
                {
                    if (hits[i].collider.transform.IsChildOf(body.transform) || hits[i].distance >= nearest) continue;
                    nearest = hits[i].distance;
                    ground = hits[i];
                }
                if (ground.collider == null || ground.normal.y < 0.55f || !flow.IsGrassSurface(ground.collider)) continue;
                distance += speed * Time.fixedDeltaTime;
                float noise = Mathf.PerlinNoise(point.x * 0.3f + 50f, point.z * 0.3f + 50f);
                float spacing = Mathf.Lerp(1.8f, 3.2f, noise);
                if (distance < spacing) return;
                distance -= spacing;
                float kick = Mathf.Lerp(0.10f, 0.42f, Mathf.InverseLerp(3f, 30f, speed)) * Mathf.Lerp(0.6f, 1f, noise);
                body.AddForceAtPosition(Vector3.up * (body.mass * kick), point, ForceMode.Impulse);
                nextWheel = (wheel + 1) % wheelOffsets.Length;
                return;
            }
            distance = 0f;
        }
    }
}
