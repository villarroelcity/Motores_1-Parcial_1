using UnityEngine;

namespace EnElCamino
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 4.2f;
        [SerializeField] private float height = 1.9f;
        [SerializeField] private float lookHeight = 1.1f;
        [SerializeField] private float lookAhead = 3.5f;
        [SerializeField] private float followSpeed = 12f;
        [SerializeField] private float collisionRadius = 0.22f;
        [SerializeField] private float collisionPadding = 0.12f;
        [SerializeField] private float minimumDistance = 0.75f;
        [SerializeField] private LayerMask collisionMask = ~0;
        private float yaw;
        private float pitch = 12f;
        private float smoothedDistance;
        private float distanceVelocity;
        private readonly RaycastHit[] cameraHits = new RaycastHit[24];

        public void SetTarget(Transform value)
        {
            target = value;
            if (target != null)
            {
                yaw = target.eulerAngles.y;
                smoothedDistance = distance;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, followSpeed * Time.deltaTime);
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 anchor = target.position + Vector3.up * height;
            Vector3 cameraDirection = (orbit * Vector3.back).normalized;
            float safeDistance = distance;
            int hitCount = Physics.SphereCastNonAlloc(anchor, collisionRadius, cameraDirection,
                cameraHits, distance, collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Transform hitTransform = cameraHits[i].transform;
                if (hitTransform == null || hitTransform == target || hitTransform.IsChildOf(target)) continue;
                safeDistance = Mathf.Min(safeDistance,
                    Mathf.Max(minimumDistance, cameraHits[i].distance - collisionPadding));
            }
            smoothedDistance = Mathf.SmoothDamp(smoothedDistance, safeDistance, ref distanceVelocity, 0.08f);
            Vector3 desired = anchor + cameraDirection * smoothedDistance;
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 focus = target.position + Vector3.up * lookHeight + forward * lookAhead;
            transform.rotation = Quaternion.LookRotation(focus - transform.position);
        }

    }
}
