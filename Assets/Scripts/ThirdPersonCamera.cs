using UnityEngine;
using UnityEngine.InputSystem;

namespace EnElCamino
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 6.5f;
        [SerializeField] private float height = 2.65f;
        [SerializeField] private float lookHeight = 1.15f;
        [SerializeField] private float lookAhead = 2.4f;
        [SerializeField] private float sensitivity = 0.12f;
        [SerializeField] private float followSpeed = 8f;
        private float yaw;

        public void SetTarget(Transform value)
        {
            target = value;
            if (target != null) yaw = target.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            if (Mouse.current != null) yaw += Mouse.current.delta.ReadValue().x * sensitivity;
            Quaternion orbit = Quaternion.Euler(0f, yaw, 0f);
            Vector3 desired = target.position + Vector3.up * height + orbit * Vector3.back * distance;
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
            Vector3 focus = target.position + Vector3.up * lookHeight + orbit * Vector3.forward * lookAhead;
            transform.rotation = Quaternion.LookRotation(focus - transform.position);
        }
    }
}
