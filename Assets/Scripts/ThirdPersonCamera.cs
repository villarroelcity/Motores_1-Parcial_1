using UnityEngine;
using UnityEngine.InputSystem;

namespace EnElCamino
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 4.2f;
        [SerializeField] private float height = 1.9f;
        [SerializeField] private float lookHeight = 1.1f;
        [SerializeField] private float lookAhead = 3.5f;
        [SerializeField] private float sensitivity = 0.08f;
        [SerializeField] private float followSpeed = 12f;
        private float yaw;
        private float pitch = 12f;

        private void Start()
        {
            LockCursor();
        }

        public void SetTarget(Transform value)
        {
            target = value;
            if (target != null) yaw = target.eulerAngles.y;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                LockCursor();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (Mouse.current != null)
            {
                Vector2 mouse = Mouse.current.delta.ReadValue();
                yaw += mouse.x * sensitivity;
                pitch -= mouse.y * sensitivity;
                pitch = Mathf.Clamp(pitch, -10f, 35f);
            }

            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desired = target.position + Vector3.up * height + orbit * Vector3.back * distance;
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 focus = target.position + Vector3.up * lookHeight + forward * lookAhead;
            transform.rotation = Quaternion.LookRotation(focus - transform.position);
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
