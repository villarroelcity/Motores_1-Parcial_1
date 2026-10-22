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
        private const float WheelRadius = 0.32f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
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
        }

        private void FixedUpdate()
        {
            if (direction.sqrMagnitude < 0.01f || speed <= 0f)
            {
                return;
            }

            float distance = speed * Time.fixedDeltaTime;
            Vector3 nextPosition = body != null ? body.position : transform.position;
            nextPosition += direction * distance;
            nextPosition.y = roadHeight;

            if (direction.z > 0f && nextPosition.z > maxZ)
            {
                nextPosition.z = minZ;
            }
            else if (direction.z < 0f && nextPosition.z < minZ)
            {
                nextPosition.z = maxZ;
            }

            Quaternion nextRotation = Quaternion.LookRotation(direction, Vector3.up);
            if (body != null)
            {
                body.MovePosition(nextPosition);
                body.MoveRotation(nextRotation);
            }
            else
            {
                transform.SetPositionAndRotation(nextPosition, nextRotation);
            }

            float wheelDegrees = distance / (2f * Mathf.PI * WheelRadius) * 360f;
            if (wheels != null)
            {
                foreach (Transform wheel in wheels)
                {
                    if (wheel != null) wheel.Rotate(Vector3.right, wheelDegrees, Space.Self);
                }
            }
        }
    }
}
