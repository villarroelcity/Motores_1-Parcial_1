using UnityEngine;
using VehiclePhysics;
using System.Reflection;

namespace EnElCamino
{
    // Simple first-year AI: the patrol follows the player's car, waits if it
    // is still ahead of the player, and steers around collidable obstacles.
    [RequireComponent(typeof(Rigidbody))]
    public class PatrulleroIA : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private CarDriver targetCar;
        [SerializeField] private float chaseSpeed = 22f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float escapeDistance = 100f;
        [SerializeField] private float maxChaseSeconds = 60f;
        [SerializeField] private AudioClip sirenClip;
        [SerializeField] private float sirenVolume = 0.55f;
        [SerializeField] private LayerMask obstacleLayers = ~0;
        [SerializeField] private float obstacleDetectionDistance = 9f;
        [SerializeField] private float obstacleDetectionRadius = 1.2f;
        [SerializeField] private float obstacleSideCheckDistance = 4f;
        [SerializeField] private float obstacleAvoidanceStrength = 1.8f;
        [SerializeField] private Transform road;
        [SerializeField] private float roadHalfWidth = 3.2f;

        private Rigidbody body;
        private bool chasing;
        private bool waitingForPlayerToPass;
        private float speed;
        private float chaseTime;
        private Light leftPoliceLight;
        private Light rightPoliceLight;
        private Renderer leftBeaconRenderer;
        private Renderer rightBeaconRenderer;
        private AudioSource sirenSource;
        private bool policeEffectsActive;
        private const float MinimumChaseSpeed = 32f;
        private const float MinimumAcceleration = 18f;
        private const float RequiredEscapeDistance = 200f;

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
            foreach (AudioSource source in GetComponentsInChildren<AudioSource>(true))
            {
                source.Stop();
                source.enabled = false;
            }

            EnableOptionalVisualDamage();
            CreatePoliceLights();
            CreateSirenSource();
            SetPoliceEffects(false);
        }

        private void DisableVppTelemetry()
        {
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().Name != "VPTelemetry") continue;
                FieldInfo field = component.GetType().GetField("showTelemetry", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(bool)) field.SetValue(component, false);
            }
        }

        private void Start()
        {
            if (gameFlow == null) return;
            gameFlow.PursuitStarted += BeginChase;
            if (gameFlow.pursuitActive) BeginChase();
        }

        private void OnDestroy()
        {
            if (gameFlow != null) gameFlow.PursuitStarted -= BeginChase;
            if (sirenSource != null) sirenSource.Stop();
        }

        private void BeginChase()
        {
            if (chasing) return;
            if (targetCar == null) targetCar = FindFirstObjectByType<CarDriver>();
            if (targetCar == null) return;

            chasing = true;
            chaseTime = 0f;
            speed = 0f;
            Vector3 relativePosition = transform.position - targetCar.transform.position;
            relativePosition.y = 0f;
            waitingForPlayerToPass = Vector3.Dot(targetCar.transform.forward, relativePosition) > 1f;
            body.isKinematic = true;
            body.WakeUp();
            SetPoliceEffects(!waitingForPlayerToPass);
        }

        private void Update()
        {
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
            if (!chasing) return;
            if (targetCar == null)
            {
                targetCar = FindFirstObjectByType<CarDriver>();
                if (targetCar == null) return;
            }

            if (waitingForPlayerToPass)
            {
                Vector3 relativePosition = body.position - targetCar.transform.position;
                relativePosition.y = 0f;
                if (Vector3.Dot(targetCar.transform.forward, relativePosition) > 1f) return;

                waitingForPlayerToPass = false;
                SetPoliceEffects(true);
            }

            chaseTime += Time.fixedDeltaTime;
            Vector3 offset = targetCar.transform.position - body.position;
            offset.y = 0f;
            float distanceBehindPlayer = Mathf.Max(0f,
                Vector3.Dot(targetCar.transform.forward, offset));
            float requiredEscapeDistance = Mathf.Max(escapeDistance, RequiredEscapeDistance);
            if (gameFlow != null)
                gameFlow.UpdatePursuitDistance(distanceBehindPlayer, requiredEscapeDistance);

            // Passing alongside the parked patrol is not a defeat. The patrol
            // only defeats the player if it actually collides with the car or
            // the chase timeout is reached.
            if (chaseTime >= maxChaseSeconds)
            {
                gameFlow.GameOver();
                return;
            }
            // The patrol starts ahead of the player in the scene. That initial
            // separation is not an escape: only count it after the patrol has
            // actually fallen behind the player's forward direction.
            bool patrolBehindPlayer = Vector3.Dot(targetCar.transform.forward, offset) > 0f;
            if (distanceBehindPlayer >= requiredEscapeDistance && patrolBehindPlayer)
            {
                chasing = false;
                SetPoliceEffects(false);
                gameFlow.TryFinish();
                return;
            }

            Vector3 direction = GetRoadChaseDirection();
            direction = AvoidObstacles(direction);
            direction = KeepInsideRoad(direction);
            float targetSpeed = Mathf.Max(chaseSpeed, MinimumChaseSpeed);
            float targetAcceleration = Mathf.Max(acceleration, MinimumAcceleration);
            speed = Mathf.MoveTowards(speed, targetSpeed, targetAcceleration * Time.fixedDeltaTime);
            body.MovePosition(body.position + direction * speed * Time.fixedDeltaTime);
            Quaternion wantedRotation = Quaternion.LookRotation(direction, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(body.rotation, wantedRotation, 120f * Time.fixedDeltaTime));
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!chasing || waitingForPlayerToPass || chaseTime < 1f || gameFlow == null) return;
            if (collision.collider.GetComponentInParent<CarDriver>() != null)
                gameFlow.GameOver();
        }

        private Vector3 AvoidObstacles(Vector3 desiredDirection)
        {
            desiredDirection = Vector3.ProjectOnPlane(desiredDirection, Vector3.up).normalized;
            if (desiredDirection.sqrMagnitude < 0.01f) return transform.forward;

            Vector3 origin = body.position + Vector3.up * 0.65f;
            if (!FindObstacle(origin, desiredDirection, obstacleDetectionDistance, out _))
                return desiredDirection;

            Vector3 side = Vector3.Cross(Vector3.up, desiredDirection).normalized;
            float leftClearance = GetClearance(origin, -side);
            float rightClearance = GetClearance(origin, side);
            Vector3 preferredSide = rightClearance >= leftClearance ? side : -side;
            return (desiredDirection + preferredSide * obstacleAvoidanceStrength).normalized;
        }

        private Vector3 GetRoadChaseDirection()
        {
            Vector3 targetPosition = targetCar.transform.position;
            if (road != null)
            {
                float centerX = road.position.x;
                float halfWidth = Mathf.Max(roadHalfWidth, road.lossyScale.x * 0.5f);
                targetPosition.x = Mathf.Clamp(targetPosition.x, centerX - halfWidth * 0.65f,
                    centerX + halfWidth * 0.65f);
            }

            Vector3 roadOffset = targetPosition - body.position;
            roadOffset.y = 0f;
            return roadOffset.sqrMagnitude > 0.01f ? roadOffset.normalized : transform.forward;
        }

        private Vector3 KeepInsideRoad(Vector3 direction)
        {
            if (road == null) return direction;

            float centerX = road.position.x;
            float halfWidth = Mathf.Max(roadHalfWidth, road.lossyScale.x * 0.5f);
            float leftEdge = centerX - halfWidth;
            float rightEdge = centerX + halfWidth;
            if (body.position.x <= leftEdge && direction.x < 0f) direction.x = 0f;
            if (body.position.x >= rightEdge && direction.x > 0f) direction.x = 0f;
            return direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward;
        }

        private float GetClearance(Vector3 origin, Vector3 direction)
        {
            return FindObstacle(origin, direction, obstacleSideCheckDistance, out RaycastHit hit)
                ? hit.distance
                : obstacleSideCheckDistance;
        }

        private bool FindObstacle(Vector3 origin, Vector3 direction, float distance, out RaycastHit closestHit)
        {
            closestHit = default;
            RaycastHit[] hits = Physics.SphereCastAll(
                origin,
                obstacleDetectionRadius,
                direction,
                distance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = float.MaxValue;
            bool found = false;
            foreach (RaycastHit hit in hits)
            {
                Collider collider = hit.collider;
                if (collider == null || collider.transform.IsChildOf(transform)) continue;
                if (collider.GetComponentInParent<CarDriver>() != null) continue;
                if (hit.normal.y > 0.7f) continue;
                if (hit.distance >= nearestDistance) continue;

                nearestDistance = hit.distance;
                closestHit = hit;
                found = true;
            }

            return found;
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

        private void CreatePoliceLights()
        {
            Bounds bounds = GetPatrolBounds();
            Vector3 roofCenter = bounds.center;
            roofCenter.y = bounds.max.y + 0.12f;
            float spacing = Mathf.Clamp(bounds.extents.x * 0.42f, 0.25f, 0.75f);

            CreateBeacon("LuzAzulIzquierda", roofCenter - transform.right * spacing,
                out leftPoliceLight, out leftBeaconRenderer);
            CreateBeacon("LuzAzulDerecha", roofCenter + transform.right * spacing,
                out rightPoliceLight, out rightBeaconRenderer);
        }

        private void CreateBeacon(string objectName, Vector3 worldPosition,
            out Light light, out Renderer beaconRenderer)
        {
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beacon.name = objectName;
            Destroy(beacon.GetComponent<Collider>());
            beacon.transform.SetParent(transform, false);
            beacon.transform.localPosition = transform.InverseTransformPoint(worldPosition);
            beacon.transform.localRotation = Quaternion.identity;
            beacon.transform.localScale = new Vector3(0.32f, 0.12f, 0.20f);

            beaconRenderer = beacon.GetComponent<Renderer>();
            Material material = new Material(FindUnlitShader());
            Color blue = new Color(0.02f, 0.18f, 1f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", blue);
            if (material.HasProperty("_Color")) material.SetColor("_Color", blue);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", blue * 5f);
            }
            beaconRenderer.material = material;

            light = beacon.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = blue;
            light.range = 11f;
            light.intensity = 7f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
            light.enabled = false;
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
            sirenSource.spatialBlend = 0.75f;
            sirenSource.minDistance = 8f;
            sirenSource.maxDistance = 80f;
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
            if (!material.HasProperty("_EmissionColor")) return;
            Color blue = new Color(0.02f, 0.18f, 1f, 1f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", on ? blue * 7f : Color.black);
        }

        private Bounds GetPatrolBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static Shader FindUnlitShader()
        {
            return Shader.Find("Universal Render Pipeline/Unlit") ??
                   Shader.Find("Unlit/Color") ??
                   Shader.Find("Standard");
        }
    }
}
