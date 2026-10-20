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
        private float yaw;
        private float pitch = 12f;

        public void SetTarget(Transform value)
        {
            target = value;
            if (target != null) yaw = target.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, followSpeed * Time.deltaTime);
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desired = target.position + Vector3.up * height + orbit * Vector3.back * distance;
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 focus = target.position + Vector3.up * lookHeight + forward * lookAhead;
            transform.rotation = Quaternion.LookRotation(focus - transform.position);
        }

    }
}
