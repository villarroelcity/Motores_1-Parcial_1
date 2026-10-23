using UnityEngine;
using VehiclePhysics;

namespace EnElCamino
{
    // Follows the player's car along the road and steers around obstacles.
    [RequireComponent(typeof(Rigidbody))]
    public class PatrulleroIA : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private CarDriver targetCar;
        [SerializeField] private float chaseSpeed = 42f;
        [SerializeField] private float escapeDistance = 100f;
        [SerializeField] private AudioClip sirenClip;
        [SerializeField] private float sirenVolume = 0.9f;
        [SerializeField] private LayerMask obstacleLayers = ~0;
        [SerializeField] private Transform road;
        [SerializeField] private float roadHalfWidth = 3.2f;

        private Rigidbody body;
        private bool chasing;
        private bool intercepting;
        private Vector3 routeForward;
        private Vector3 movementForward;
        private float pursuitDirection = 1f;
        private float chosenLane;
        private float trackedTargetLane;
        private bool offRoadPursuit;
        private float grassBumpDistance;
        private int grassBumpWheel;
        private float nextPlanTime;
        private float plannedClearTime = 3f;
        private float steering;
        private Rigidbody targetBody;
        private readonly WheelCollider[] drivingWheels = new WheelCollider[4];
        private readonly Transform[] wheelModels = new Transform[4];
        private readonly Quaternion[] wheelRotations = new Quaternion[4];
        private readonly RaycastHit[] sensorHits = new RaycastHit[24];
        private float wheelbase = 2.7f;
        private int wheelCount;
        private float stuckTime;
        private float reverseUntil;
        private float chaseTime;
        private float nextHeadlightRefresh;
        [SerializeField] private Light leftPoliceLight;
        [SerializeField] private Light rightPoliceLight;
        [SerializeField] private Renderer leftBeaconRenderer;
        [SerializeField] private Renderer rightBeaconRenderer;
        private AudioSource sirenSource;
        private bool policeEffectsActive;
        private TrafficVehicle[] trafficVehicles;
        private float nextTrafficScan;
        private const float MinimumChaseSpeed = 48f;
        private const float RequiredEscapeDistance = 200f;
        private const float RoadEdgeMargin = 1.15f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            if (targetCar == null) targetCar = FindFirstObjectByType<CarDriver>();
            if (road == null)
            {
                GameObject roadObject = GameObject.Find("Ruta");
                if (roadObject != null) road = roadObject.transform;
            }
            DisableVppTelemetry();

            // Keep the patrol parked and silent until the trigger/speed limit
            // starts the chase. The player's VPP controls are unaffected.
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            foreach (VPStandardInput input in GetComponentsInChildren<VPStandardInput>(true))
                input.enabled = false;
            foreach (VPVehicleController controller in GetComponentsInChildren<VPVehicleController>(true))
                controller.enabled = false;
            // Disable VPP's wheels before configuring the patrol's Unity tires.
            foreach (WheelCollider wheel in GetComponentsInChildren<WheelCollider>(true))
                wheel.enabled = false;
            foreach (AudioSource source in GetComponentsInChildren<AudioSource>(true))
            {
                source.Stop();
                source.enabled = false;
            }
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
                if (component != null && component.GetType().Name == "VPAudioExtras")
                    component.enabled = false;

            ConfigureDrivingWheels();
            EnableOptionalVisualDamage();
            BindPoliceLights();
            CreateSirenSource();
            SetPoliceEffects(false);
        }

        private void DisableVppTelemetry()
        {
            VehicleLightUtility.DisableTelemetry(gameObject);
        }

        private void Start()
        {
            if (gameFlow == null) return;
            gameFlow.PursuitStarted += BeginChase;
            gameFlow.NightLightingChanged += SetHeadlights;
            SetHeadlights(gameFlow.NightLightsOn);
            if (gameFlow.pursuitActive) BeginChase();
        }

        private void OnDestroy()
        {
            if (gameFlow != null)
            {
                gameFlow.PursuitStarted -= BeginChase;
                gameFlow.NightLightingChanged -= SetHeadlights;
            }
            if (sirenSource != null) sirenSource.Stop();
        }

        private void SetHeadlights(bool enabled)
        {
            VehicleLightUtility.SetHeadlights(gameObject, true);
        }

        private void BeginChase()
        {
            if (chasing) return;
            if (targetCar == null) targetCar = FindFirstObjectByType<CarDriver>();
            if (targetCar == null) return;

            chasing = true;
            chaseTime = 0f;
            StartPatrolMotion();
            SetPoliceEffects(true);
        }

        private void StartPatrolMotion()
        {
            if (intercepting) return;
            intercepting = true;
            targetBody = targetCar.GetComponent<Rigidbody>();
            routeForward = GetRoadForward();
            movementForward = routeForward;
            chosenLane = road != null ? Vector3.Dot(body.position - road.position,
                Vector3.Cross(Vector3.up, routeForward)) : 0f;
            trackedTargetLane = chosenLane;
            body.isKinematic = false;
            body.useGravity = true;
            body.mass = Mathf.Max(body.mass, 1200f);
            body.constraints = RigidbodyConstraints.None;
            body.centerOfMass = new Vector3(0f, 0.15f, 0.4f);
            body.maxAngularVelocity = 3f;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;

            body.maxDepenetrationVelocity = 2.5f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
                if (collider != null && !(collider is WheelCollider) && !collider.isTrigger)
                    collider.sharedMaterial = TrafficVehicle.GetNoBounceMaterial();
            body.WakeUp();
        }

        private void Update()
        {
            if (Time.time >= nextHeadlightRefresh)
            {
                SetHeadlights(true);
                nextHeadlightRefresh = Time.time + 1f;
            }

            if (!chasing) return;
            if (gameFlow != null && (gameFlow.gameOver || gameFlow.finished))
            {
                chasing = false;
                SetPoliceEffects(false);
                return;
            }

            UpdatePoliceLights();
        }

        private void FixedUpdate()
        {
            if (gameFlow == null || gameFlow.gameOver || gameFlow.finished) return;
            // Fallback for scene load/order issues: the patrol can still start
            // when GameFlow already raised the pursuit state before this
            // component subscribed to the event.
            if (!chasing && gameFlow.pursuitActive) BeginChase();
            if (!chasing)
            {
                PrepareInterception();
                if (intercepting) DrivePursuit();
                return;
            }
            if (targetCar == null)
            {
                targetCar = FindFirstObjectByType<CarDriver>();
                if (targetCar == null) return;
            }

            chaseTime += Time.fixedDeltaTime;
            Vector3 offset = targetCar.transform.position - body.position;
            offset.y = 0f;
            float distanceBehindPlayer = Mathf.Max(0f,
                Vector3.Dot(routeForward, offset));
            float requiredEscapeDistance = Mathf.Max(escapeDistance, RequiredEscapeDistance);
            if (gameFlow != null)
                gameFlow.UpdatePursuitDistance(distanceBehindPlayer, requiredEscapeDistance);

            // The patrol only defeats the player if it actually collides with
            // the car; GameFlow owns the one-minute escape deadline.
            bool patrolBehindPlayer = Vector3.Dot(routeForward, offset) > 0f;
            if (gameFlow.PursuitDefeatEnabled && distanceBehindPlayer >= requiredEscapeDistance && patrolBehindPlayer)
            {
                chasing = false;
                SetPoliceEffects(false);
                gameFlow.TryFinish();
                return;
            }

            DrivePursuit();
        }

        private void PrepareInterception()
        {
            if (intercepting || targetCar == null || !gameFlow.driving || !gameFlow.carHasFuel) return;
            Vector3 forward = GetRoadForward();
            Rigidbody playerBody = targetCar.GetComponent<Rigidbody>();
            float approachingSpeed = playerBody != null ? Vector3.Dot(playerBody.linearVelocity, forward) : 0f;
            float distanceToPatrol = Vector3.Dot(body.position - targetCar.transform.position, forward);
            // Start pulling onto the road while the approaching car is still
            // four seconds away. The trigger still starts the chase HUD/timer.
            if (approachingSpeed > 10f && distanceToPatrol > 0f &&
                distanceToPatrol < approachingSpeed * 4f + 35f)
                StartPatrolMotion();
        }

        private void LateUpdate()
        {
            for (int i = 0; i < wheelCount; i++)
            {
                if (wheelModels[i] == null) continue;
                drivingWheels[i].GetWorldPose(out Vector3 position, out Quaternion rotation);
                wheelModels[i].SetPositionAndRotation(position, rotation * wheelRotations[i]);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!chasing || chaseTime < 1f || gameFlow == null || !gameFlow.PursuitDefeatEnabled) return;
            if (collision.collider.GetComponentInParent<CarDriver>() != null)
                gameFlow.GameOver();
        }

        // The Community Edition permits only one VPP vehicle. The patrol uses
        // Unity WheelColliders, so steering and impacts still come from tires.
        private void ConfigureDrivingWheels()
        {
            foreach (VPWheelCollider source in GetComponentsInChildren<VPWheelCollider>(true))
            {
                if (wheelCount >= 4) break;
                Transform model = source.wheelTransform;
                source.hideWheelOnDisable = false;
                source.enabled = false;
                if (model != null) model.gameObject.SetActive(true);

                GameObject holder = new GameObject("RuedaFisicaPatrulla");
                holder.transform.SetParent(transform, false);
                holder.transform.position = source.transform.TransformPoint(source.center) + transform.up * 0.09f;
                holder.transform.rotation = transform.rotation;
                WheelCollider wheel = holder.AddComponent<WheelCollider>();
                wheel.radius = source.radius;
                wheel.mass = 25f;
                wheel.suspensionDistance = 0.18f;
                JointSpring spring = wheel.suspensionSpring;
                spring.spring = 42000f;
                spring.damper = 5000f;
                spring.targetPosition = 0.5f;
                wheel.suspensionSpring = spring;
                wheel.forceAppPointDistance = 0.15f;
                WheelFrictionCurve friction = wheel.forwardFriction;
                friction.stiffness = 1.6f;
                wheel.forwardFriction = friction;
                friction = wheel.sidewaysFriction;
                friction.stiffness = 1.8f;
                wheel.sidewaysFriction = friction;
                wheel.ConfigureVehicleSubsteps(10f, 5, 8);
                drivingWheels[wheelCount] = wheel;
                wheelModels[wheelCount] = model;
                wheelRotations[wheelCount] = model != null
                    ? Quaternion.Inverse(transform.rotation) * model.rotation : Quaternion.identity;
                wheelCount++;
            }
            if (wheelCount != 4)
            {
                Debug.LogError("La patrulla necesita las cuatro ruedas del prefab VPP.", this);
                return;
            }
            float front = float.NegativeInfinity;
            float rear = float.PositiveInfinity;
            foreach (WheelCollider wheel in drivingWheels)
            {
                front = Mathf.Max(front, wheel.transform.localPosition.z);
                rear = Mathf.Min(rear, wheel.transform.localPosition.z);
            }
            wheelbase = Mathf.Max(2f, front - rear);
        }

        private void DrivePursuit()
        {
            if (wheelCount != 4 || road == null) return;
            Vector3 right = Vector3.Cross(Vector3.up, routeForward);
            float signedVelocity = Vector3.Dot(body.linearVelocity, transform.forward);
            float velocity = Mathf.Abs(signedVelocity);
            float gap = Vector3.Dot(targetCar.transform.position - body.position, routeForward);
            float safeWidth = Mathf.Max(0.5f, GetRoadHalfWidth() - RoadEdgeMargin);
            float playerLane = Vector3.Dot(targetCar.transform.position - road.position, right);
            float patrolLane = Vector3.Dot(body.position - road.position, right);
            if (chasing && Mathf.Abs(playerLane) > safeWidth + 0.8f) offRoadPursuit = true;
            else if (Mathf.Abs(playerLane) < safeWidth - 0.3f && Mathf.Abs(patrolLane) < safeWidth + 0.5f)
                offRoadPursuit = false;
            // If the patrol overshoots, brake and back toward the player on the
            // road rather than continuing to chase an imaginary point ahead.
            float playerAlongRoad = targetBody != null ? Vector3.Dot(targetBody.linearVelocity, routeForward) : 0f;
            // Slow down when ahead of a moving player. Only back up after
            // stopping, if the player has actually stopped or gone backward.
            if (chasing && gap < -12f && playerAlongRoad < 3f && velocity < 2f) pursuitDirection = -1f;
            else if ((gap > 5f || playerAlongRoad > 8f) && velocity < 2f) pursuitDirection = 1f;
            movementForward = routeForward * pursuitDirection;
            if (Time.fixedTime >= nextPlanTime)
            {
                PlanPassingLane(right, velocity);
                nextPlanTime = Time.fixedTime + 0.15f;
            }

            float lookAhead = Mathf.Clamp(8f + velocity * 0.65f, 10f, 40f);
            if (chasing) lookAhead = Mathf.Min(lookAhead, Mathf.Max(5f, Mathf.Abs(gap)));
            float lane = Vector3.Dot(body.position - road.position, right);
            Vector3 aim = body.position + movementForward * lookAhead + right * (chosenLane - lane);
            if (offRoadPursuit)
            {
                Vector3 prediction = targetCar.transform.position + (targetBody != null ? targetBody.linearVelocity * 0.35f : Vector3.zero);
                float targetForward = Mathf.Clamp(Vector3.Dot(prediction - body.position, routeForward), -lookAhead, lookAhead);
                aim = body.position + routeForward * targetForward + right * (chosenLane - lane);
            }
            Vector3 localAim = transform.InverseTransformPoint(aim);
            float curvature = 2f * localAim.x / Mathf.Max(1f, localAim.sqrMagnitude);
            float desiredSteer = Mathf.Atan(wheelbase * curvature) * Mathf.Rad2Deg;
            float lateralAcceleration = offRoadPursuit ? 6.5f : 12f;
            float maxSteer = Mathf.Min(32f,
                Mathf.Atan(wheelbase * lateralAcceleration / Mathf.Max(4f, velocity * velocity)) * Mathf.Rad2Deg);
            steering = Mathf.MoveTowards(steering, Mathf.Clamp(desiredSteer, -maxSteer, maxSteer),
                55f * Time.fixedDeltaTime);

            float playerSpeed = targetBody != null ? Vector3.Dot(targetBody.linearVelocity, movementForward) : 0f;
            float desiredSpeed = chasing
                ? Mathf.Clamp(playerSpeed + gap * pursuitDirection * 0.65f, 0f, 60f)
                : Mathf.Max(chaseSpeed, MinimumChaseSpeed);
            if (pursuitDirection < 0f) desiredSpeed = Mathf.Min(desiredSpeed, 16f);
            if (pursuitDirection < 0f && playerAlongRoad > 8f) desiredSpeed = 0f;
            if (offRoadPursuit)
            {
                float distance = Vector3.ProjectOnPlane(targetCar.transform.position - body.position, Vector3.up).magnitude;
                desiredSpeed = Mathf.Clamp((targetBody != null ? targetBody.linearVelocity.magnitude : 0f) + distance * 0.5f, 0f, 35f);
            }
            // A parked car first turns onto the road; high-speed corners are
            // limited by available lateral tire grip rather than forced yaw.
            desiredSpeed = Mathf.Min(desiredSpeed,
                Mathf.Sqrt(lateralAcceleration / Mathf.Max(0.004f, Mathf.Abs(curvature))));
            if (plannedClearTime < 1.2f) desiredSpeed = Mathf.Min(desiredSpeed, velocity * 0.6f);
            float driveSign = Vector3.Dot(movementForward, transform.forward) >= 0f ? 1f : -1f;
            if (offRoadPursuit)
            {
                driveSign = localAim.z < -2f ? -1f : 1f;
                if (driveSign < 0f) desiredSpeed = Mathf.Min(desiredSpeed, 12f);
            }
            float pedal = Mathf.Clamp((desiredSpeed - signedVelocity * driveSign) * 0.35f, -1f, 1f);
            bool changingDirection = signedVelocity * driveSign < -0.5f;
            if (changingDirection) pedal = -1f;
            stuckTime = velocity < 1f && Mathf.Abs(gap) > 6f ? stuckTime + Time.fixedDeltaTime : 0f;
            if (stuckTime > 2f && Time.fixedTime >= reverseUntil)
            {
                reverseUntil = Time.fixedTime + 1.2f;
                stuckTime = 0f;
            }
            bool reversing = Time.fixedTime < reverseUntil;
            float frontZ = float.NegativeInfinity;
            foreach (WheelCollider wheel in drivingWheels)
                frontZ = Mathf.Max(frontZ, wheel.transform.localPosition.z);
            foreach (WheelCollider wheel in drivingWheels)
            {
                bool frontWheel = wheel.transform.localPosition.z > frontZ - 0.5f;
                wheel.steerAngle = frontWheel ? (reversing ? -steering : steering) : 0f;
                wheel.motorTorque = reversing ? -driveSign * 650f : driveSign * Mathf.Max(0f, pedal) * (velocity < 20f ? 1400f : 950f);
                wheel.brakeTorque = reversing ? 0f : Mathf.Max(0f, -pedal) * 2200f;
                bool grass = wheel.GetGroundHit(out WheelHit hit) && gameFlow.IsGrassSurface(hit.collider);
                WheelFrictionCurve friction = wheel.sidewaysFriction;
                friction.stiffness = grass ? 1.15f : 1.8f;
                wheel.sidewaysFriction = friction;
            }
            // Downforce keeps suspension contact at pursuit speeds.
            body.AddForce(-transform.up * velocity * velocity * 1.8f, ForceMode.Force);
            GrassDriving.ApplyBumps(body, gameFlow, ref grassBumpDistance, ref grassBumpWheel);
        }

        private void PlanPassingLane(Vector3 right, float velocity)
        {
            if (Time.time >= nextTrafficScan || trafficVehicles == null)
            {
                trafficVehicles = FindObjectsByType<TrafficVehicle>(FindObjectsSortMode.None);
                nextTrafficScan = Time.time + 0.5f;
            }
            float halfWidth = Mathf.Max(0.5f, GetRoadHalfWidth() - RoadEdgeMargin);
            float lane = Vector3.Dot(body.position - road.position, right);
            float rawTargetLane = Vector3.Dot(targetCar.transform.position - road.position, right);
            float minLane = offRoadPursuit ? Mathf.Min(-halfWidth, lane - 1.5f, rawTargetLane - 1.5f) : -halfWidth;
            float maxLane = offRoadPursuit ? Mathf.Max(halfWidth, lane + 1.5f, rawTargetLane + 1.5f) : halfWidth;
            float targetLane = Mathf.Clamp(rawTargetLane, minLane, maxLane);
            trackedTargetLane = Mathf.MoveTowards(trackedTargetLane, targetLane, offRoadPursuit ? 1.2f : 0.35f);
            targetLane = Mathf.Clamp(trackedTargetLane, minLane, maxLane);
            float bestScore = float.PositiveInfinity;
            float bestLane = chosenLane;
            float bestClearTime = 0f;
            // Five corridors plus the current plan. Keeping a plan has a cost
            // advantage, preventing left/right oscillation at every sensor tick.
            for (int candidate = 0; candidate < 8; candidate++)
            {
                float destination = candidate == 5 ? Mathf.Clamp(chosenLane, minLane, maxLane)
                    : candidate == 6 ? targetLane : candidate == 7 ? lane
                    : Mathf.Lerp(minLane, maxLane, candidate / 4f);
                float clearTime = EvaluateCorridor(destination, lane, right, velocity);
                float score = Mathf.Abs(destination - targetLane) * 0.8f +
                    (Mathf.Abs(destination - chosenLane) > 0.4f ? 0.6f : 0f);
                if (clearTime < 3f) score += 1000f * (3f - clearTime);
                if (score >= bestScore) continue;
                bestScore = score;
                bestLane = destination;
                bestClearTime = clearTime;
            }
            chosenLane = bestLane;
            plannedClearTime = bestClearTime;
        }

        private float EvaluateCorridor(float destination, float currentLane, Vector3 right, float velocity)
        {
            Vector3 previous = body.position + Vector3.up * 0.9f;
            float horizonSpeed = Mathf.Max(12f, velocity);
            for (int step = 1; step <= 12; step++)
            {
                float time = step * 0.25f;
                float blend = Mathf.SmoothStep(0f, 1f, time / 1.5f);
                Vector3 point = body.position + movementForward * (horizonSpeed * time) +
                    right * ((destination - currentLane) * blend) + Vector3.up * 0.9f;
                foreach (TrafficVehicle traffic in trafficVehicles)
                {
                    if (traffic == null || !traffic.isActiveAndEnabled) continue;
                    Rigidbody trafficBody = traffic.GetComponent<Rigidbody>();
                    Vector3 predicted = traffic.transform.position +
                        (trafficBody != null ? trafficBody.linearVelocity * time : Vector3.zero);
                    Vector3 offset = predicted - point;
                    if (Mathf.Abs(Vector3.Dot(offset, right)) < 2.1f &&
                        Mathf.Abs(Vector3.Dot(offset, routeForward)) < 5.8f)
                        return time;
                }
                Vector3 segment = point - previous;
                // A wide, low box detects body-height obstructions without
                // sweeping into the asphalt/grass under the bumper.
                int hits = Physics.BoxCastNonAlloc(previous, new Vector3(0.85f, 0.3f, 0.15f),
                    segment.normalized, sensorHits, Quaternion.LookRotation(movementForward),
                    segment.magnitude, obstacleLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hits; i++)
                {
                    Collider hit = sensorHits[i].collider;
                    if (hit.transform.IsChildOf(transform) || hit.GetComponentInParent<CarDriver>() != null ||
                        hit.GetComponentInParent<TrafficVehicle>() != null || sensorHits[i].normal.y > 0.6f)
                        continue;
                    return time;
                }
                previous = point;
            }
            return 3f;
        }

        private Vector3 GetRoadForward()
        {
            Vector3 forward = road != null ? road.forward : targetCar.transform.forward;
            forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            if (Vector3.Dot(forward, targetCar.transform.forward) < 0f) forward = -forward;
            return forward.sqrMagnitude > 0.01f ? forward : transform.forward;
        }

        private float GetRoadHalfWidth()
        {
            Renderer surface = road.GetComponentInChildren<Renderer>();
            if (surface == null) return roadHalfWidth;
            // Local mesh width remains correct even if the road is rotated.
            Vector3 width = surface.transform.TransformVector(
                Vector3.right * surface.localBounds.extents.x);
            return Mathf.Max(1.5f, width.magnitude);
        }

        private void EnableOptionalVisualDamage()
        {
            // If the VPP prefab includes a damage/deformation component, keep
            // it enabled. If it does not, this loop intentionally does nothing.
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) continue;
                string componentName = component.GetType().Name;
                bool isDamageComponent = componentName.IndexOf("Damage", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    componentName.IndexOf("Deform", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (isDamageComponent) component.enabled = true;
            }
        }

        private void BindPoliceLights()
        {
            // Geometry and lights belong to the saved scene, editable in Stop.
            if (leftPoliceLight == null)
                leftPoliceLight = transform.Find("BalizaIzquierda")?.GetComponent<Light>();
            if (rightPoliceLight == null)
                rightPoliceLight = transform.Find("BalizaDerecha")?.GetComponent<Light>();
            if (leftBeaconRenderer == null && leftPoliceLight != null)
                leftBeaconRenderer = leftPoliceLight.GetComponent<Renderer>();
            if (rightBeaconRenderer == null && rightPoliceLight != null)
                rightBeaconRenderer = rightPoliceLight.GetComponent<Renderer>();
        }

        private void CreateSirenSource()
        {
            if (sirenClip == null) sirenClip = Resources.Load<AudioClip>("SFX/sirena_patrulla");

            GameObject sourceObject = new GameObject("SirenaPatrulla");
            sourceObject.transform.SetParent(transform, false);
            sirenSource = sourceObject.AddComponent<AudioSource>();
            sirenSource.clip = sirenClip;
            sirenSource.loop = true;
            sirenSource.playOnAwake = false;
            sirenSource.volume = sirenVolume;
            sirenSource.spatialBlend = 1f;
            sirenSource.rolloffMode = AudioRolloffMode.Linear;
            sirenSource.dopplerLevel = 0f;
            sirenSource.minDistance = 8f;
            sirenSource.maxDistance = 220f;
        }

        private void SetPoliceEffects(bool active)
        {
            policeEffectsActive = active;
            if (!active)
            {
                SetLightState(leftPoliceLight, leftBeaconRenderer, false);
                SetLightState(rightPoliceLight, rightBeaconRenderer, false);
                if (sirenSource != null) sirenSource.Stop();
                return;
            }

            if (sirenSource != null && sirenSource.clip != null && !sirenSource.isPlaying)
                sirenSource.Play();
        }

        private void UpdatePoliceLights()
        {
            if (!policeEffectsActive) return;
            bool leftOn = Mathf.Repeat(Time.time * 3.2f, 1f) < 0.5f;
            SetLightState(leftPoliceLight, leftBeaconRenderer, leftOn);
            SetLightState(rightPoliceLight, rightBeaconRenderer, !leftOn);
        }

        private static void SetLightState(Light light, Renderer renderer, bool on)
        {
            if (light != null) light.enabled = on;
            if (renderer == null) return;

            Material material = renderer.material;
            Color blue = new Color(0f, 0.12f, 1f, 1f);
            Color lensColor = on ? blue : blue * 0.16f;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", lensColor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", lensColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", on ? blue * 3f : Color.black);
            }
        }

    }
}
