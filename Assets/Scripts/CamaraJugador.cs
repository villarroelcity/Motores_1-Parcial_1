using UnityEngine;
using VehiclePhysics;

namespace EnElCamino
{
    [DefaultExecutionOrder(5000)]
    public class CamaraJugador : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 4.2f;
        [SerializeField] private float height = 1.9f;
        [SerializeField] private float lookHeight = 1.1f;
        [SerializeField] private float lookAhead = 0.75f;
        [SerializeField] private float followSpeed = 12f;
        [SerializeField] private float collisionRadius = 0.35f;
        [SerializeField] private float collisionPadding = 0.18f;
        [SerializeField] private float minimumDistance = 0.45f;
        [SerializeField] private LayerMask collisionMask = ~0;
        private float yaw;
        private float pitch = 12f;
        private float smoothedDistance;
        private float distanceVelocity;
        private readonly RaycastHit[] cameraHits = new RaycastHit[24];
        private GameFlow gameFlow;
        private Transform vehicleTarget;

        private void Awake()
        {
            if (collisionMask.value == 0) collisionMask = Physics.DefaultRaycastLayers;
            smoothedDistance = distance;
            gameFlow = FindFirstObjectByType<GameFlow>();
        }

        private void Start()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            if (gameFlow != null && gameFlow.driving)
                RouteToVehicle();
        }

        public void SetTarget(Transform value)
        {
            target = value;
            vehicleTarget = value != null && value.GetComponent<VPVehicleController>() != null ? value : null;
            if (target != null)
            {
                yaw = target.eulerAngles.y;
                smoothedDistance = distance;
                distanceVelocity = 0f;
            }
        }

        public void SetVehicleTarget(Transform value)
        {
            if (value == null) return;
            vehicleTarget = value;
            SetTarget(value);
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            smoothedDistance = CalculateSafeDistance(target.position + Vector3.up * height,
                (Quaternion.Euler(pitch, yaw, 0f) * Vector3.back).normalized);
            distanceVelocity = 0f;
            Vector3 anchor = target.position + Vector3.up * height;
            Vector3 cameraDirection = (Quaternion.Euler(pitch, yaw, 0f) * Vector3.back).normalized;
            transform.position = anchor + cameraDirection * smoothedDistance;
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 focus = target.position + Vector3.up * lookHeight + forward * lookAhead;
            transform.rotation = Quaternion.LookRotation(focus - transform.position);
        }

        private void LateUpdate()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            if (gameFlow != null && gameFlow.driving) RouteToVehicle();
            if (target == null) return;
            yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, followSpeed * Time.deltaTime);
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 anchor = target.position + Vector3.up * height;
            Vector3 cameraDirection = (orbit * Vector3.back).normalized;
            float safeDistance = CalculateSafeDistance(anchor, cameraDirection);
            bool cameraHitObstacle = safeDistance < distance - 0.01f;
            if (cameraHitObstacle)
                smoothedDistance = safeDistance;
            smoothedDistance = Mathf.SmoothDamp(smoothedDistance, safeDistance, ref distanceVelocity, 0.08f);
            Vector3 desired = anchor + cameraDirection * smoothedDistance;
            transform.position = cameraHitObstacle
                ? desired
                : Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 focus = target.position + Vector3.up * lookHeight + forward * lookAhead;
            transform.rotation = Quaternion.LookRotation(focus - transform.position);
        }

        private void RouteToVehicle()
        {
            VPVehicleController vehicle = vehicleTarget != null
                ? vehicleTarget.GetComponent<VPVehicleController>()
                : null;
            if (vehicle == null || !vehicle.isActiveAndEnabled)
            {
                VPVehicleController[] vehicles = FindObjectsByType<VPVehicleController>(FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
                for (int i = 0; i < vehicles.Length; i++)
                {
                    if (vehicles[i] != null && vehicles[i].isActiveAndEnabled)
                    {
                        vehicle = vehicles[i];
                        break;
                    }
                }
            }
            if (vehicle == null || target == vehicle.transform) return;
            vehicleTarget = vehicle.transform;
            SetTarget(vehicle.transform);
            SnapToTarget();
        }

        private float CalculateSafeDistance(Vector3 anchor, Vector3 direction)
        {
            float safeDistance = distance;
            int hitCount = Physics.SphereCastNonAlloc(anchor, collisionRadius, direction,
                cameraHits, distance, collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Transform hitTransform = cameraHits[i].transform;
                if (hitTransform == null || hitTransform == target || hitTransform.IsChildOf(target)) continue;
                safeDistance = Mathf.Min(safeDistance,
                    Mathf.Max(minimumDistance, cameraHits[i].distance - collisionPadding));
            }
            return safeDistance;
        }

    }
}
