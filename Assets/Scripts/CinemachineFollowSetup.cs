using UnityEngine;
using Unity.Cinemachine;

namespace EnElCamino
{
    /// <summary>
    /// Creates the gameplay virtual camera at runtime while preserving the existing scene camera.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class CinemachineFollowSetup : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Transform vehicle;
        [SerializeField] private float distance = 4.2f;
        [SerializeField] private float height = 1.9f;
        [SerializeField] private float collisionRadius = 0.18f;
        [SerializeField] private float minimumCameraDistance = 0.12f;

        private CinemachineCamera gameplayCamera;

        private void Awake()
        {
            if (target == null)
            {
                GameObject player = GameObject.Find("Jugador");
                if (player != null) target = player.transform;
            }

            if (vehicle == null)
            {
                GameObject car = GameObject.Find("VehiculoConRuedas");
                if (car != null) vehicle = car.transform;
            }

            ThirdPersonCamera legacyCamera = GetComponent<ThirdPersonCamera>();
            if (legacyCamera != null) legacyCamera.enabled = false;

            CinemachineBrain brain = GetComponent<CinemachineBrain>();
            if (brain == null) brain = gameObject.AddComponent<CinemachineBrain>();
            brain.DefaultBlend.Time = 0.35f;

            GameObject rig = new GameObject("CinemachineGameplayCamera");
            gameplayCamera = rig.AddComponent<CinemachineCamera>();
            gameplayCamera.Priority = 10;
            gameplayCamera.Follow = target;
            gameplayCamera.LookAt = target;
            gameplayCamera.Lens.FieldOfView = 60f;
            gameplayCamera.transform.position = transform.position - transform.forward * distance + Vector3.up * height;

            CinemachineThirdPersonFollow follow = rig.AddComponent<CinemachineThirdPersonFollow>();
            follow.ShoulderOffset = new Vector3(0.35f, 1.4f, 0f);
            follow.VerticalArmLength = 0.35f;
            follow.CameraSide = 0.5f;
            follow.CameraDistance = distance;
            // The custom post-process below ignores the target hierarchy.  The
            // built-in Cinemachine collision resolver can mistake the player's
            // own capsule/body for an obstacle in this scene.
            follow.AvoidObstacles = new CinemachineThirdPersonFollow.ObstacleSettings
            {
                Enabled = false,
                CollisionFilter = 0,
                IgnoreTag = string.Empty,
                CameraRadius = 0.18f,
                DampingIntoCollision = 0f,
                DampingFromCollision = 0f
            };
            CinemachineRotationComposer aim = rig.AddComponent<CinemachineRotationComposer>();
            aim.TargetOffset = new Vector3(0f, 1.35f, 0f);
            aim.Damping = new Vector2(0.2f, 0.2f);
            aim.CenterOnActivate = true;
        }

        private void Update()
        {
            GameFlow flow = FindFirstObjectByType<GameFlow>();
            if (flow != null && flow.driving && vehicle != null && gameplayCamera != null)
                SetTarget(vehicle);
        }

        private void LateUpdate()
        {
            if (target == null || gameplayCamera == null)
                return;

            // CinemachineBrain has already placed the real camera at this
            // point.  Only shorten that segment when an external collider is
            // between the target and the camera; never test the player/vehicle
            // hierarchy itself.
            Vector3 anchor = target.position + Vector3.up * 1.15f;
            Vector3 cameraPosition = transform.position;
            Vector3 targetToCamera = cameraPosition - anchor;
            float targetDistance = targetToCamera.magnitude;
            if (targetDistance <= minimumCameraDistance)
                return;

            Vector3 direction = targetToCamera / targetDistance;
            RaycastHit[] hits = Physics.SphereCastAll(
                anchor,
                collisionRadius,
                direction,
                targetDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float safeDistance = targetDistance;
            for (int i = 0; i < hits.Length; i++)
            {
                if (IsInsideTargetHierarchy(hits[i].collider.transform))
                    continue;

                safeDistance = Mathf.Min(safeDistance, hits[i].distance - collisionRadius);
            }

            if (safeDistance >= targetDistance - 0.001f)
                return;

            safeDistance = Mathf.Max(minimumCameraDistance, safeDistance);
            transform.position = anchor + direction * safeDistance;
        }

        public void SetTarget(Transform newTarget)
        {
            if (newTarget == null || gameplayCamera == null) return;
            target = newTarget;
            gameplayCamera.Follow = newTarget;
            gameplayCamera.LookAt = newTarget;

            CinemachineThirdPersonFollow follow = gameplayCamera.GetComponent<CinemachineThirdPersonFollow>();
        }

        private bool IsInsideTargetHierarchy(Transform candidate)
        {
            return candidate == target || candidate.IsChildOf(target);
        }
    }
}
