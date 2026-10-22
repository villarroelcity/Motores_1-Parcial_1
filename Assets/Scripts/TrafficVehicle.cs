using UnityEngine;

namespace EnElCamino
{
    /// <summary>
    /// Mueve un vehiculo visual de trafico a velocidad constante.
    /// Los vehiculos de trafico usan la carroceria de VPP, pero no su controlador fisico.
    /// </summary>
    public class TrafficVehicle : MonoBehaviour
    {
        private Rigidbody body;
        private Vector3 direction;
        private float speed;
        private float minZ;
        private float maxZ;
        private float roadHeight;

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

        public void Configure(Vector3 moveDirection, float speedMetersPerSecond, float lowerZ, float upperZ, float height)
        {
            direction = moveDirection.normalized;
            speed = speedMetersPerSecond;
            minZ = lowerZ;
            maxZ = upperZ;
            roadHeight = height;

            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void FixedUpdate()
        {
            if (direction.sqrMagnitude < 0.01f || speed <= 0f)
            {
                return;
            }

            Vector3 nextPosition = body != null ? body.position : transform.position;
            nextPosition += direction * speed * Time.fixedDeltaTime;
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
        }
    }
}
