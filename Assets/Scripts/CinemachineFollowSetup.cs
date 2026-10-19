using UnityEngine;
using Unity.Cinemachine;

namespace EnElCamino
{
    /// <summary>
    /// Creates the gameplay virtual camera at runtime while preserving the existing scene camera.
    /// </summary>
    public class CinemachineFollowSetup : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Transform vehicle;
        [SerializeField] private float distance = 4.2f;
        [SerializeField] private float height = 1.9f;

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

            CinemachineManualOrbit orbit = rig.AddComponent<CinemachineManualOrbit>();
            orbit.Configure(target, 0.008f);

            CinemachineDeoccluder deoccluder = rig.AddComponent<CinemachineDeoccluder>();
            deoccluder.CollideAgainst = ~0;
            deoccluder.IgnoreTag = string.Empty;
            deoccluder.AvoidObstacles.Enabled = true;
            deoccluder.AvoidObstacles.CameraRadius = 0.25f;
            deoccluder.AvoidObstacles.Strategy = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
            deoccluder.AvoidObstacles.Damping = 0.25f;
            deoccluder.AvoidObstacles.DampingWhenOccluded = 0.1f;
        }

        private void Update()
        {
            GameFlow flow = FindFirstObjectByType<GameFlow>();
            if (flow != null && flow.driving && vehicle != null && gameplayCamera != null)
                SetTarget(vehicle);
        }

        public void SetTarget(Transform newTarget)
        {
            if (newTarget == null || gameplayCamera == null) return;
            gameplayCamera.Follow = newTarget;
            gameplayCamera.LookAt = newTarget;
        }
    }
}
