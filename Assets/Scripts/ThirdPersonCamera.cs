using UnityEngine;
using UnityEngine.InputSystem;

namespace EnElCamino
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 5.5f, -8f);
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
            Quaternion orbit = Quaternion.Euler(28f, yaw, 0f);
            Vector3 desired = target.position + orbit * offset;
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * 1.1f);
        }
    }
}
