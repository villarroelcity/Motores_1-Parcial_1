using UnityEngine;

namespace EnElCamino
{
    /// <summary>
    /// Mueve un vehiculo visual de trafico a velocidad constante.
    /// </summary>
    public class TrafficVehicle : MonoBehaviour
    {
        private Rigidbody body;
        private Vector3 direction;
        private float speed;
        private float minZ;
        private float maxZ;
        private float roadHeight;
        private Transform[] wheels;
        private bool stoppedByCollision;
        private const float WheelRadius = 0.32f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.mass = 1400f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }

            foreach (AudioSource audioSource in GetComponentsInChildren<AudioSource>(true))
            {
                audioSource.Stop();
                audioSource.enabled = false;
            }
        }

        public void Configure(Vector3 moveDirection, float speedMetersPerSecond, float lowerZ, float upperZ,
            float height, Transform[] wheelTransforms)
        {
            direction = moveDirection.normalized;
            speed = speedMetersPerSecond;
            minZ = lowerZ;
            maxZ = upperZ;
            roadHeight = height;
            wheels = wheelTransforms;

            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            if (body != null) body.linearVelocity = direction * speed;
        }

        private void FixedUpdate()
        {
            if (stoppedByCollision)
            {
                return;
            }

            if (direction.sqrMagnitude < 0.01f || speed <= 0f)
            {
                return;
            }

            if (body != null)
            {
                Vector3 desiredVelocity = direction * speed;
                Vector3 velocity = body.linearVelocity;
                desiredVelocity.y = velocity.y;
                body.AddForce(Vector3.ClampMagnitude(desiredVelocity - velocity, 4f * Time.fixedDeltaTime),
                    ForceMode.VelocityChange);
            }

            float distance = (body != null ? Vector3.Dot(body.linearVelocity, direction) : speed) * Time.fixedDeltaTime;
            float wheelDegrees = distance / (2f * Mathf.PI * WheelRadius) * 360f;
            if (wheels != null)
            {
                foreach (Transform wheel in wheels)
                {
                    if (wheel != null) wheel.Rotate(Vector3.right, wheelDegrees, Space.Self);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            StopAfterImpact(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            StopAfterImpact(collision);
        }

        private void StopAfterImpact(Collision collision)
        {
            if (stoppedByCollision) return;
            Collider other = collision.collider;
            if (other == null || other.attachedRigidbody == body) return;
            // El suelo sostiene el auto; cualquier impacto lateral corta su motor.
            foreach (ContactPoint contact in collision.contacts)
            {
                if (Mathf.Abs(contact.normal.y) > 0.7f) continue;
                stoppedByCollision = true;
                if (body != null)
                {
                    body.constraints = RigidbodyConstraints.None;
                    body.linearDamping = 0.7f;
                    body.angularDamping = 1f;
                }
                return;
            }
        }

        public bool CanRecycle => !stoppedByCollision;

        public void Recycle(float z)
        {
            Vector3 position = body.position;
            position.z = Mathf.Clamp(z, minZ, maxZ);
            position.y = roadHeight;
            body.position = position;
            body.rotation = Quaternion.LookRotation(direction, Vector3.up);
            body.linearVelocity = direction * speed;
            body.angularVelocity = Vector3.zero;
        }
    }
}
