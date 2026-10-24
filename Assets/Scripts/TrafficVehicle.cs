using UnityEngine;
using System.Collections;
using System.Reflection;

namespace EnElCamino
{
    internal static class VehicleLightUtility
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static void SetHeadlights(GameObject vehicle, bool enabled)
        {
            if (vehicle == null) return;

            foreach (MonoBehaviour component in vehicle.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().Name != "VPVisualEffects") continue;
                SetReferencedObjects(component, "headLightsOn", enabled);
                SetReferencedObjects(component, "headLightsOff", !enabled);
                FieldInfo state = component.GetType().GetField("headLightsEnabled", InstanceFlags);
                if (state != null && state.FieldType == typeof(bool)) state.SetValue(component, enabled);
            }

            foreach (Transform item in vehicle.GetComponentsInChildren<Transform>(true))
            {
                string name = item.name.Replace(" ", "").Replace("_", "").ToLowerInvariant();
                if (!name.Contains("headlight") && !name.Contains("headlights")) continue;
                if (name.Contains("off")) item.gameObject.SetActive(!enabled);
                else if (name.Contains("on") || name.Contains("glow")) item.gameObject.SetActive(enabled);
            }
            SetRoadBeams(vehicle, enabled);
        }

        private static void SetRoadBeams(GameObject vehicle, bool enabled)
        {
            Transform holder = vehicle.transform.Find("FarosSobreAsfalto");
            if (holder != null) holder.gameObject.SetActive(enabled);
        }

        public static void DisableTelemetry(GameObject vehicle)
        {
            if (vehicle == null) return;

            foreach (MonoBehaviour component in vehicle.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().Name != "VPTelemetry") continue;

                DisableHotKey(component, "enableHotKey");
                DisableHotKey(component, "showTelemetry");
                component.enabled = false;
            }
        }

        private static void DisableHotKey(MonoBehaviour component, string fieldName)
        {
            FieldInfo field = component.GetType().GetField(fieldName, InstanceFlags);
            if (field != null && field.FieldType == typeof(bool)) field.SetValue(component, false);
        }

        private static void SetReferencedObjects(MonoBehaviour component, string fieldName, bool active)
        {
            FieldInfo field = component.GetType().GetField(fieldName, InstanceFlags);
            if (field == null) return;
            object value = field.GetValue(component);
            SetObjectActive(value, active);

            IEnumerable objects = value as IEnumerable;
            if (objects == null) return;
            foreach (object item in objects) SetObjectActive(item, active);
        }

        private static void SetObjectActive(object value, bool active)
        {
            GameObject target = value as GameObject;
            if (target == null && value is Component child) target = child.gameObject;
            if (target != null) target.SetActive(active);
        }
    }

    /// <summary>
    /// Mantiene un auto de trafico en su carril y frena si encuentra algo adelante.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TrafficVehicle : MonoBehaviour
    {
        [SerializeField] private float brakingDistance = 22f;
        [SerializeField] private float safeDistance = 5f;
        [SerializeField] private float acceleration = 3f;
        [SerializeField] private float braking = 10f;

        private Rigidbody body;
        private Vector3 direction;
        private float maxSpeed;
        private float currentSpeed;
        private float minZ;
        private float maxZ;
        [SerializeField] private float groundRootY;
        [SerializeField] private float groundOffset;
        private float laneX;
        [SerializeField] private Transform[] wheels;
        private bool damagedInCollision;
        private float trafficContactUntil;
        private const float WheelRadius = 0.34f;

        private void Awake()
        {
            VehicleLightUtility.DisableTelemetry(gameObject);
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
                if (component != null && component.GetType().Namespace == "VehiclePhysics")
                {
                    if (component is VehiclePhysics.VPWheelCollider wheel) wheel.hideWheelOnDisable = false;
                    component.enabled = false;
                }
            body = GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = true;
            body.linearDamping = 0f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxDepenetrationVelocity = 2.5f;
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            foreach (AudioSource audioSource in GetComponentsInChildren<AudioSource>(true))
            {
                audioSource.Stop();
                audioSource.enabled = false;
            }
        }

        public float GroundOffset => groundOffset;

        public void Configure(Vector3 moveDirection, float speedMetersPerSecond,
            float lowerZ, float upperZ, float rootY, float lanePositionX)
        {
            if (body == null) body = GetComponent<Rigidbody>();
            damagedInCollision = false;
            trafficContactUntil = 0f;
            direction = moveDirection.normalized;
            maxSpeed = speedMetersPerSecond;
            currentSpeed = maxSpeed;
            minZ = lowerZ;
            maxZ = upperZ;
            groundRootY = rootY;
            // Traffic travels on this flat road. Keep its wheels at road height
            // while retaining dynamic horizontal collision response.
            body.constraints = RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            laneX = lanePositionX;

            body.linearVelocity = direction * currentSpeed;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
        }

        public void SetHeadlights(bool enabled)
        {
            VehicleLightUtility.SetHeadlights(gameObject, enabled);
        }

        private void FixedUpdate()
        {
            if (direction.sqrMagnitude < 0.01f || maxSpeed <= 0f) return;

            if (damagedInCollision)
            {
                Vector3 planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
                body.AddForce(-planarVelocity * 1.5f, ForceMode.Acceleration);
                return;
            }

            currentSpeed = Mathf.Max(0f, Vector3.Dot(body.linearVelocity, direction));
            float targetSpeed = Time.time < trafficContactUntil ? 0f : GetSafeSpeedAhead();
            float motor = Mathf.Clamp((targetSpeed - currentSpeed) / Time.fixedDeltaTime, -braking, acceleration);
            body.AddForce(direction * motor, ForceMode.Acceleration);
            float lateralForce = Mathf.Clamp((laneX - body.position.x) * 3f - body.linearVelocity.x * 2f, -4f, 4f);
            body.AddForce(Vector3.right * lateralForce, ForceMode.Acceleration);

            Quaternion straightAhead = Quaternion.LookRotation(direction, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(body.rotation, straightAhead,
                35f * Time.fixedDeltaTime));

            float wheelDegrees = currentSpeed * Time.fixedDeltaTime / (2f * Mathf.PI * WheelRadius) * 360f;
            if (wheels != null)
            {
                foreach (Transform wheel in wheels)
                    if (wheel != null) wheel.Rotate(Vector3.right, wheelDegrees, Space.Self);
            }

            Vector3 position = body.position;
            if (direction.z > 0f && position.z > maxZ)
                body.position = new Vector3(laneX, groundRootY, minZ);
            else if (direction.z < 0f && position.z < minZ)
                body.position = new Vector3(laneX, groundRootY, maxZ);
        }

        private float GetSafeSpeedAhead()
        {
            Vector3 origin = body.position + Vector3.up * 0.9f;
            float sensorDistance = Mathf.Max(brakingDistance, currentSpeed * currentSpeed / (2f * braking) + safeDistance + 4f);
            RaycastHit[] hits = Physics.BoxCastAll(origin, new Vector3(0.85f, 0.3f, 0.15f), direction,
                Quaternion.LookRotation(direction), sensorDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            float allowedSpeed = maxSpeed;
            foreach (RaycastHit hit in hits)
            {
                Collider otherCollider = hit.collider;
                if (otherCollider == null || otherCollider.transform.IsChildOf(transform)) continue;
                if (hit.normal.y > 0.7f) continue;

                TrafficVehicle otherTraffic = otherCollider.GetComponentInParent<TrafficVehicle>();
                // Only moving road users participate in adaptive cruise. The
                // huge road/terrain colliders must never be treated as cars at rest.
                if (otherTraffic == null && otherCollider.GetComponentInParent<CarDriver>() == null &&
                    otherCollider.GetComponentInParent<PatrulleroIA>() == null) continue;
                Rigidbody otherBody = otherTraffic != null
                    ? otherTraffic.body
                    : otherCollider.GetComponentInParent<Rigidbody>();
                float otherSpeed = otherBody != null ? Mathf.Max(0f, Vector3.Dot(otherBody.linearVelocity, direction)) : 0f;
                float availableGap = Mathf.Max(0f, hit.distance - safeDistance);
                float safeSpeed = Mathf.Sqrt(otherSpeed * otherSpeed + 2f * braking * availableGap);
                allowedSpeed = Mathf.Min(allowedSpeed, safeSpeed);
            }

            return allowedSpeed;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null || collision.collider == null) return;
            if (collision.collider.transform.IsChildOf(transform)) return;
            Bounds contactBounds = collision.collider.bounds;
            if (contactBounds.size.x > 20f && contactBounds.size.z > 20f && contactBounds.size.y < 1f) return;
            if (collision.collider.GetComponentInParent<TrafficVehicle>() != null)
            {
                // A traffic-to-traffic scrape yields briefly, then rejoins the
                // flow; it must not permanently disable both cruise controllers.
                trafficContactUntil = Time.time + 0.75f;
                return;
            }

            bool isRoadContact = collision.collider.GetComponentInParent<TrafficVehicle>() == null &&
                collision.collider.GetComponentInParent<CarDriver>() == null &&
                collision.collider.GetComponentInParent<PatrulleroIA>() == null;
            if (isRoadContact)
            {
                foreach (ContactPoint contact in collision.contacts)
                {
                    if (contact.normal.y > 0.7f) return;
                }
            }

            // Stop the cruise controller, preserving the momentum exchanged by
            // physics. Subsequent contacts must not restart its motor.
            damagedInCollision = true;
        }

    }
}
