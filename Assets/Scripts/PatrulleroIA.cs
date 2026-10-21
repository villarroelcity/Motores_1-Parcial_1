using UnityEngine;
using VehiclePhysics;
using System.Reflection;

namespace EnElCamino
{
    // Simple first-year AI: the patrol follows the player's car in a straight
    // chase. VPP remains on the player's car; the patrol only needs movement,
    // a collision and an adjustable catch distance.
    [RequireComponent(typeof(Rigidbody))]
    public class PatrulleroIA : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private CarDriver targetCar;
        [SerializeField] private float chaseSpeed = 22f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float catchDistance = 5f;
        [SerializeField] private float escapeDistance = 100f;
        [SerializeField] private float maxChaseSeconds = 60f;
        [SerializeField] private AudioClip sirenClip;
        [SerializeField] private float sirenVolume = 0.55f;

        private Rigidbody body;
        private bool chasing;
        private float speed;
        private float chaseTime;
        private Light leftPoliceLight;
        private Light rightPoliceLight;
        private Renderer leftBeaconRenderer;
        private Renderer rightBeaconRenderer;
        private AudioSource sirenSource;
        private bool policeEffectsActive;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            if (targetCar == null) targetCar = FindFirstObjectByType<CarDriver>();
            DisableVppTelemetry();

            // Keep the patrol parked and silent until the trigger/speed limit
            // starts the chase. The player's VPP controls are unaffected.
            body.isKinematic = true;
            body.useGravity = false;
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
            body.isKinematic = true;
            body.WakeUp();
            SetPoliceEffects(true);
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

            chaseTime += Time.fixedDeltaTime;
            Vector3 offset = targetCar.transform.position - body.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            float distanceBehindPlayer = Mathf.Max(0f,
                Vector3.Dot(targetCar.transform.forward, offset));
            if (gameFlow != null)
                gameFlow.UpdatePursuitDistance(distanceBehindPlayer, escapeDistance);

            if (distance <= catchDistance || chaseTime >= maxChaseSeconds)
            {
                gameFlow.GameOver();
                return;
            }
            // The patrol starts ahead of the player in the scene. That initial
            // separation is not an escape: only count it after the patrol has
            // actually fallen behind the player's forward direction.
            bool patrolBehindPlayer = Vector3.Dot(targetCar.transform.forward, offset) > 0f;
            if (distanceBehindPlayer >= escapeDistance && patrolBehindPlayer)
            {
                chasing = false;
                SetPoliceEffects(false);
                gameFlow.TryFinish();
                return;
            }

            Vector3 direction = offset.sqrMagnitude > 0.01f ? offset.normalized : transform.forward;
            speed = Mathf.MoveTowards(speed, chaseSpeed, acceleration * Time.fixedDeltaTime);
            body.MovePosition(body.position + direction * speed * Time.fixedDeltaTime);
            Quaternion wantedRotation = Quaternion.LookRotation(direction, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(body.rotation, wantedRotation, 120f * Time.fixedDeltaTime));
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!chasing || gameFlow == null) return;
            if (collision.collider.GetComponentInParent<CarDriver>() != null)
                gameFlow.GameOver();
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
