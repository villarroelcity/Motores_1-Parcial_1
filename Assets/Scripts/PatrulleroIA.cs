using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace EnElCamino
{
    [RequireComponent(typeof(Rigidbody))]
    public class PatrulleroIA : MonoBehaviour
    {
        [SerializeField] private Juego gameFlow;
        [SerializeField] private AutoJugador targetCar;
        [SerializeField] private float chaseSpeed = 72f;
        [SerializeField] private LayerMask obstacleLayers = ~0;
        [SerializeField] private Transform road;
        [SerializeField] private float roadHalfWidth = 3.2f;
        [SerializeField] private int navigationAgentType;
        [SerializeField] private NavMeshSurface navigationSurface;

        private Rigidbody body;
        private bool chasing;
        private bool intercepting;
        private Vector3 routeForward;
        private Vector3 mergePoint;
        private bool waitingForPass;
        private float driveDirection = 1f;
        private bool offRoadPursuit;
        private float nextPlanTime;
        private NavMeshPath navigationPath;
        private readonly Vector3[] pathCorners = new Vector3[64];
        private NavMeshQueryFilter navigationFilter;
        private int pathCornerCount;
        private int pathIndex;
        private float turnSide = 1f;
        private float steering;
        private Rigidbody targetBody;
        private Rigidbody passingCar;
        private float passingSide = -1f;
        [SerializeField] private WheelCollider[] drivingWheels = new WheelCollider[4];
        [SerializeField] private Transform[] wheelModels = new Transform[4];
        [SerializeField] private Quaternion[] wheelRotations = new Quaternion[4];
        private readonly RaycastHit[] sensorHits = new RaycastHit[24];
        [SerializeField] private float wheelbase = 2.7f;
        private float stuckTime;
        private float reverseUntil;
        private float chaseTime;
        private float nextHeadlightRefresh;
        [SerializeField] private Light leftPoliceLight;
        [SerializeField] private Light rightPoliceLight;
        [SerializeField] private Renderer leftBeaconRenderer;
        [SerializeField] private Renderer rightBeaconRenderer;
        [SerializeField] private AudioSource sirenSource;
        private bool policeEffectsActive;
        private const float BrakingAcceleration = 4f;
        private const float RoadEdgeMargin = 1.15f;

        private void Awake()
        {
            if (navigationSurface == null)
                navigationSurface = FindFirstObjectByType<NavMeshSurface>(FindObjectsInactive.Include);
            if (navigationSurface != null)
            {
                navigationSurface.gameObject.SetActive(true);
                navigationSurface.enabled = true;
            }
            body = GetComponent<Rigidbody>();
            body.maxAngularVelocity = 3f;
            body.maxDepenetrationVelocity = 2.5f;
            navigationPath = new NavMeshPath();
            navigationFilter = new NavMeshQueryFilter { agentTypeID = navigationAgentType, areaMask = NavMesh.AllAreas };
            navigationFilter.SetAreaCost(3, 2.5f);
            if (gameFlow == null) gameFlow = FindFirstObjectByType<Juego>();
            if (targetCar == null) targetCar = FindFirstObjectByType<AutoJugador>();
            if (road == null)
            {
                GameObject roadObject = GameObject.Find("Ruta");
                if (roadObject != null) road = roadObject.transform;
            }

            CacheDrivingWheels();
            SetPoliceEffects(false);
        }

        private void Start()
        {
            if (gameFlow == null) return;
            gameFlow.PursuitStarted += BeginChase;
            VehicleLightUtility.SetHeadlights(gameObject, true);
            if (gameFlow.pursuitActive) BeginChase();
        }

        private void OnDestroy()
        {
            if (gameFlow != null)
            {
                gameFlow.PursuitStarted -= BeginChase;
            }
            if (sirenSource != null) sirenSource.Stop();
        }

        private void BeginChase()
        {
            if (chasing) return;
            if (targetCar == null) targetCar = FindFirstObjectByType<AutoJugador>();
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
            waitingForPass = true;
            Vector3 right = Vector3.Cross(Vector3.up, routeForward);
            float lane = Mathf.Clamp(Vector3.Dot(targetBody.position - road.position, right),
                -roadHalfWidth + RoadEdgeMargin, roadHalfWidth - RoadEdgeMargin);
            mergePoint = body.position + routeForward * 35f +
                right * (lane - Vector3.Dot(body.position - road.position, right));
            driveDirection = 1f;
            pathCornerCount = 0;
            nextPlanTime = 0f;
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;

            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.WakeUp();
        }

        private void Update()
        {
            if (Time.time >= nextHeadlightRefresh)
            {
                VehicleLightUtility.SetHeadlights(gameObject, true);
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
            if (!chasing && gameFlow.pursuitActive) BeginChase();
            if (!chasing)
            {
                PrepareInterception();
                if (intercepting) DrivePursuit();
                return;
            }
            if (targetCar == null)
            {
                targetCar = FindFirstObjectByType<AutoJugador>();
                if (targetCar == null) return;
            }

            chaseTime += Time.fixedDeltaTime;
            Vector3 offset = targetCar.transform.position - body.position;
            offset.y = 0f;
            float distanceBehindPlayer = Mathf.Max(0f,
                Vector3.Dot(routeForward, offset));
            gameFlow.UpdatePursuitDistance(distanceBehindPlayer, 200f);

            if (distanceBehindPlayer >= 200f)
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
            if (approachingSpeed > 10f && distanceToPatrol > 0f &&
                distanceToPatrol < approachingSpeed * 4f + 35f)
                StartPatrolMotion();
        }

        private void LateUpdate()
        {
            for (int i = 0; i < drivingWheels.Length; i++)
            {
                if (wheelModels[i] == null) continue;
                drivingWheels[i].GetWorldPose(out Vector3 position, out Quaternion rotation);
                wheelModels[i].SetPositionAndRotation(position, rotation * wheelRotations[i]);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!chasing || chaseTime < 1f || gameFlow == null) return;
            if (collision.collider.GetComponentInParent<AutoJugador>() != null)
                gameFlow.GameOver();
        }

        private void CacheDrivingWheels()
        {
            if (drivingWheels.Length != 4 || wheelModels.Length != 4)
            {
                Debug.LogError("La patrulla necesita sus cuatro ruedas configuradas en la escena.", this);
                enabled = false;
                return;
            }
            foreach (WheelCollider wheel in drivingWheels)
                if (wheel != null) wheel.ConfigureVehicleSubsteps(10f, 5, 8);
        }

        private void DrivePursuit()
        {
            if (drivingWheels.Length != 4 || road == null || targetBody == null) return;
            Vector3 forward = Vector3.ProjectOnPlane(body.rotation * Vector3.forward, Vector3.up).normalized;
            float speed = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude;
            Vector3 offset = Vector3.ProjectOnPlane(targetBody.position - body.position, Vector3.up);
            float distance = offset.magnitude;
            Vector3 right = Vector3.Cross(Vector3.up, routeForward);
            offRoadPursuit = Mathf.Abs(Vector3.Dot(targetBody.position - road.position, right)) > roadHalfWidth;
            Vector3 destination = targetBody.position + targetBody.linearVelocity * Mathf.Clamp(distance / 80f, 0f, 0.6f);
            float alongRoad = Vector3.Dot(offset, routeForward);
            bool approaching = alongRoad > -Mathf.Max(30f, Vector3.Dot(targetBody.linearVelocity, routeForward) * 1.5f);
            float mergeDistance = Vector3.Dot(mergePoint - body.position, routeForward);
            bool aligned = Mathf.Abs(Vector3.Dot(mergePoint - body.position, right)) < 0.6f &&
                Vector3.Angle(forward, routeForward) < 3f;
            if (chasing && (alongRoad >= 3f || Vector3.Dot(targetBody.linearVelocity, routeForward) < 5f))
                waitingForPass = false;
            bool merging = !chasing || waitingForPass;
            if (merging)
            {
                destination = mergePoint + routeForward * Mathf.Max(0f, 10f - mergeDistance);
                if (!chasing && !approaching && mergeDistance < 3f && aligned)
                {
                    ApplyWheelControls(0f, 0f, 1f, speed);
                    return;
                }
            }

            if (!TryGetPathTarget(destination, speed, out Vector3 aim, out bool passing))
            {
                ApplyWheelControls(0f, 0f, 1f, speed);
                return;
            }

            Vector3 localAim = Quaternion.Inverse(body.rotation) * (aim - body.position);
            float requestedDirection = SelectDriveDirection(driveDirection, localAim.z, distance);
            bool changingDirection = requestedDirection != driveDirection && speed > 0.8f;
            if (!changingDirection) driveDirection = requestedDirection;
            float curvature = 2f * localAim.x / Mathf.Max(1f, localAim.x * localAim.x + localAim.z * localAim.z);
            float desiredSteer = Mathf.Atan(wheelbase * curvature) * Mathf.Rad2Deg;
            if (merging)
            {
                float laneError = Vector3.Dot(mergePoint - body.position - forward * wheelbase * 0.5f, right);
                desiredSteer = Vector3.SignedAngle(forward, routeForward, Vector3.up) +
                    Mathf.Atan2(laneError * 0.8f, speed + 3f) * Mathf.Rad2Deg;
            }
            if (driveDirection > 0f && localAim.z < 0f)
            {
                if (Mathf.Abs(localAim.x) > 0.5f) turnSide = Mathf.Sign(localAim.x);
                desiredSteer = turnSide * 32f;
            }

            float lateralAcceleration = offRoadPursuit ? 7.5f : 11f;
            float maxSteer = Mathf.Min(32f, Mathf.Atan(wheelbase * lateralAcceleration /
                Mathf.Max(4f, speed * speed)) * Mathf.Rad2Deg);
            steering = Mathf.MoveTowards(steering, Mathf.Clamp(desiredSteer, -maxSteer, maxSteer),
                65f * Time.fixedDeltaTime);

            float targetSpeed = Mathf.Max(0f, Vector3.Dot(targetBody.linearVelocity, forward));
            float desiredSpeed = PursuitSpeed(targetSpeed, distance, chaseSpeed);
            if (merging)
                desiredSpeed = chasing || approaching ? Mathf.Clamp(targetSpeed * 0.8f, 12f, 35f)
                    : Mathf.Clamp(Mathf.Sqrt(2f * BrakingAcceleration * Mathf.Max(0f, mergeDistance - 2f)), 1.5f, 12f);
            desiredSpeed = Mathf.Min(desiredSpeed,
                Mathf.Sqrt(lateralAcceleration / Mathf.Max(0.0001f, Mathf.Abs(curvature))));
            if (localAim.z < 0f && driveDirection > 0f) desiredSpeed = Mathf.Min(desiredSpeed, 6f);
            if (driveDirection < 0f) desiredSpeed = Mathf.Min(desiredSpeed, 6f);

            bool recovery = Time.fixedTime < reverseUntil;
            float driveSign = recovery ? -driveDirection : driveDirection;
            Vector3 movement = forward * driveSign;
            Vector3 sensorDirection = passing || recovery ? movement :
                Vector3.ProjectOnPlane(aim - body.position, Vector3.up).normalized;
            desiredSpeed = Mathf.Min(desiredSpeed,
                SafeSpeedAhead(sensorDirection, speed, passing ? Mathf.Max(10f, speed * 0.5f) : distance));
            if (changingDirection) desiredSpeed = 0f;

            stuckTime = !changingDirection && speed < 0.8f && distance > 5f
                ? stuckTime + Time.fixedDeltaTime : 0f;
            if (!recovery && stuckTime > 2f && HasRecoverySpace(-movement))
            {
                reverseUntil = Time.fixedTime + 1.3f;
                stuckTime = 0f;
            }
            if (recovery)
            {
                desiredSpeed = HasRecoverySpace(movement) ? 3f : 0f;
                desiredSpeed = Mathf.Min(desiredSpeed, SafeSpeedAhead(movement, speed));
            }

            float signedSpeed = Vector3.Dot(body.linearVelocity, movement);
            float pedal = Mathf.Clamp((desiredSpeed - signedSpeed) * 0.45f, -1f, 1f);
            if (signedSpeed < -0.5f || changingDirection) pedal = -1f;
            ApplyWheelControls(recovery ? -steering : steering, driveSign * Mathf.Max(0f, pedal),
                Mathf.Max(0f, -pedal), speed);
        }

        private bool TryGetPathTarget(Vector3 destination, float speed, out Vector3 aim, out bool passing)
        {
            Vector3 toward = Vector3.ProjectOnPlane(destination - body.position, Vector3.up);
            float lookAhead = Mathf.Clamp(6f + speed * 0.65f, 6f, 48f);
            passing = TryPassTraffic(toward, speed, lookAhead, out Vector3 passPoint);
            if (passing)
            {
                aim = passPoint;
                return true;
            }
            aim = body.position + Vector3.ClampMagnitude(toward, lookAhead);
            if (ClearPath(body.position, destination)) return true;
            if (Time.fixedTime >= nextPlanTime)
            {
                nextPlanTime = Time.fixedTime + 0.2f;
                navigationFilter.SetAreaCost(3, offRoadPursuit ? 1f : 2.5f);
                if (NavMesh.SamplePosition(body.position, out NavMeshHit start, 5f, navigationFilter) &&
                    NavMesh.SamplePosition(destination, out NavMeshHit goal, 6f, navigationFilter) &&
                    NavMesh.CalculatePath(start.position, goal.position, navigationFilter, navigationPath) &&
                    navigationPath.status == NavMeshPathStatus.PathComplete)
                {
                    pathCornerCount = navigationPath.GetCornersNonAlloc(pathCorners);
                    pathIndex = 1;
                }
                else pathCornerCount = 0;
            }
            if (pathCornerCount < 2) return false;
            while (pathIndex < pathCornerCount - 1 &&
                Vector3.ProjectOnPlane(pathCorners[pathIndex] - body.position, Vector3.up).magnitude < 1.5f)
                pathIndex++;

            Vector3 startPoint = pathCorners[pathIndex - 1];
            Vector3 segment = pathCorners[pathIndex] - startPoint;
            float length = segment.magnitude;
            if (length < 0.01f) return false;
            Vector3 along = segment / length;
            float projection = Mathf.Clamp(Vector3.Dot(body.position - startPoint, along), 0f, length);
            aim = startPoint + along * Mathf.Min(length, projection + lookAhead);
            return true;
        }

        private bool TryPassTraffic(Vector3 toward, float speed, float lookAhead, out Vector3 point)
        {
            point = body.position;
            if (!chasing || waitingForPass || Vector3.Dot(toward, routeForward) < 3f)
            {
                passingCar = null;
                return false;
            }
            if (passingCar != null && (!passingCar.gameObject.activeInHierarchy ||
                Vector3.Dot(passingCar.position - body.position, routeForward) < -8f ||
                Vector3.Dot(passingCar.position - body.position, routeForward) > 220f))
                passingCar = null;
            if (passingCar == null)
            {
                float nearest = Mathf.Clamp(speed * 4f, 35f, 180f);
                Vector3 direction = toward.normalized;
                int count = Physics.BoxCastNonAlloc(body.position + Vector3.up * 0.85f,
                    new Vector3(1f, 0.35f, 0.15f), direction, sensorHits,
                    Quaternion.LookRotation(direction), nearest, obstacleLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    TraficoAutos traffic = sensorHits[i].collider.GetComponentInParent<TraficoAutos>();
                    if (traffic == null || sensorHits[i].distance >= nearest) continue;
                    Rigidbody other = traffic.GetComponent<Rigidbody>();
                    if (Vector3.Dot(other.linearVelocity, routeForward) >= speed - 2f && sensorHits[i].distance > 12f) continue;
                    passingCar = other;
                    nearest = sensorHits[i].distance;
                }
            }
            if (passingCar == null) return false;
            Vector3 right = Vector3.Cross(Vector3.up, routeForward);
            float lane = Vector3.Dot(passingCar.position - body.position, right);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                float side = attempt == 0 ? passingSide : -passingSide;
                Vector3 lanePoint = body.position + right * (lane + side * 3.4f);
                point = lanePoint + routeForward * Mathf.Max(lookAhead, speed * 1.1f);
                if (!ClearPath(body.position, point) ||
                    !ClearPath(lanePoint + routeForward * 2f, lanePoint + routeForward * Mathf.Max(45f, speed * 2.2f))) continue;
                passingSide = side;
                return true;
            }
            return false;
        }

        private bool ClearPath(Vector3 from, Vector3 to)
        {
            Vector3 offset = Vector3.ProjectOnPlane(to - from, Vector3.up);
            if (offset.sqrMagnitude < 0.01f) return true;
            int count = Physics.BoxCastNonAlloc(from + Vector3.up * 0.85f,
                new Vector3(1f, 0.35f, 0.15f), offset.normalized, sensorHits,
                Quaternion.LookRotation(offset), offset.magnitude + 2f, obstacleLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider hit = sensorHits[i].collider;
                if (hit != null && !hit.transform.IsChildOf(transform) &&
                    hit.GetComponentInParent<AutoJugador>() == null && sensorHits[i].normal.y <= 0.6f)
                    return false;
            }
            return true;
        }

        private static float PursuitSpeed(float targetSpeed, float distance, float maximum)
        {
            float closingSpeed = Mathf.Clamp((distance - 2f) * 0.65f, 0f, 3f) +
                Mathf.Clamp((distance - 20f) * 0.2f, 0f, 11f);
            float stoppingLimit = targetSpeed + Mathf.Sqrt(
                2f * BrakingAcceleration * Mathf.Max(0f, distance - 1.5f));
            return Mathf.Min(maximum, targetSpeed + closingSpeed, stoppingLimit);
        }

        private static float SelectDriveDirection(float previous, float targetForward, float distance)
        {
            if (targetForward < -3f && distance < 18f) return -1f;
            if (targetForward > 3f || distance > 25f) return 1f;
            return previous;
        }

        private float SafeSpeedAhead(Vector3 direction, float speed, float rangeLimit = float.PositiveInfinity)
        {
            if (direction.sqrMagnitude < 0.01f) return 0f;
            float range = Mathf.Min(rangeLimit, Mathf.Max(10f, speed * speed / (2f * BrakingAcceleration) + 6f));
            int count = Physics.BoxCastNonAlloc(body.position + Vector3.up * 0.85f,
                new Vector3(0.85f, 0.3f, 0.15f), direction, sensorHits,
                Quaternion.LookRotation(direction), range, obstacleLayers, QueryTriggerInteraction.Ignore);
            float result = chaseSpeed;
            for (int i = 0; i < count; i++)
            {
                Collider hit = sensorHits[i].collider;
                if (hit == null || hit.transform.IsChildOf(transform) ||
                    hit.GetComponentInParent<AutoJugador>() != null || sensorHits[i].normal.y > 0.6f) continue;
                Rigidbody other = hit.attachedRigidbody;
                float otherSpeed = other != null ? Mathf.Max(0f, Vector3.Dot(other.linearVelocity, direction)) : 0f;
                float gap = Mathf.Max(0f, sensorHits[i].distance - 3.5f);
                result = Mathf.Min(result, Mathf.Sqrt(otherSpeed * otherSpeed + 2f * BrakingAcceleration * gap));
            }
            return result;
        }

        private bool HasRecoverySpace(Vector3 direction)
        {
            int count = Physics.BoxCastNonAlloc(body.position + Vector3.up * 0.85f,
                new Vector3(0.9f, 0.3f, 2f), direction, sensorHits, body.rotation,
                3f, obstacleLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider hit = sensorHits[i].collider;
                if (hit != null && !hit.transform.IsChildOf(transform) && sensorHits[i].normal.y <= 0.6f)
                    return false;
            }
            return true;
        }

        private void ApplyWheelControls(float steer, float motor, float brake, float speed)
        {
            float frontZ = float.NegativeInfinity;
            foreach (WheelCollider wheel in drivingWheels)
                frontZ = Mathf.Max(frontZ, transform.InverseTransformPoint(wheel.transform.position).z);
            foreach (WheelCollider wheel in drivingWheels)
            {
                bool front = transform.InverseTransformPoint(wheel.transform.position).z > frontZ - 0.5f;
                wheel.steerAngle = front ? steer : 0f;
                wheel.brakeTorque = brake * 3000f;
                bool grounded = wheel.GetGroundHit(out WheelHit hit);
                wheel.motorTorque = grounded && Mathf.Abs(hit.forwardSlip) < 0.4f
                    ? motor * (speed < 18f ? 2700f : 1900f) : 0f;
                bool grass = grounded && gameFlow.IsGrassSurface(hit.collider);
                WheelFrictionCurve friction = wheel.sidewaysFriction;
                friction.stiffness = Mathf.MoveTowards(friction.stiffness, grass ? 1.15f : 1.8f, Time.fixedDeltaTime);
                wheel.sidewaysFriction = friction;
            }
            float forwardSpeed = Vector3.Dot(body.linearVelocity, body.rotation * Vector3.forward);
            float yaw = Mathf.Tan(steer * Mathf.Deg2Rad) * forwardSpeed / wheelbase;
            body.AddTorque(Vector3.up * Mathf.Clamp((yaw - body.angularVelocity.y) * 6f, -3f, 3f), ForceMode.Acceleration);
            body.AddForce(-Vector3.up * speed * speed * 1.8f, ForceMode.Force);
        }

        private Vector3 GetRoadForward()
        {
            Vector3 forward = road != null ? road.forward : targetCar.transform.forward;
            forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            Rigidbody playerBody = targetCar.GetComponent<Rigidbody>();
            Vector3 travel = playerBody != null ? playerBody.linearVelocity : Vector3.zero;
            if (Mathf.Abs(Vector3.Dot(forward, travel)) < 2f) travel = targetCar.transform.forward;
            if (Vector3.Dot(forward, travel) < 0f) forward = -forward;
            return forward.sqrMagnitude > 0.01f ? forward : transform.forward;
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

            if (sirenSource == null || sirenSource.clip == null) return;
            if (!sirenSource.isPlaying) sirenSource.Play();
            if (gameFlow != null) gameFlow.NotifyPoliceSirenStarted();
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
            Color blue = new Color(0f, 0.12f, 1f, 1f);
            VehicleLightUtility.SetAppearance(renderer, on ? blue : blue * 0.16f,
                on ? blue * 3f : Color.black);
        }

    }
}
