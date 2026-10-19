using UnityEngine;
using UnityEngine.InputSystem;

namespace EnElCamino
{
    /// <summary>Small, predictable mouse orbit for the gameplay camera.</summary>
    public class CinemachineManualOrbit : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float sensitivity = 0.035f;
        [SerializeField] private float minPitch = -12f;
        [SerializeField] private float maxPitch = 32f;

        private float yaw;
        private float pitch = 12f;

        public void Configure(Transform orbitTarget, float mouseSensitivity)
        {
            target = orbitTarget;
            sensitivity = mouseSensitivity;
            if (target != null) yaw = target.eulerAngles.y;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.None;

            if (Mouse.current == null) return;
            if (Mouse.current.leftButton.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.Locked;
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 delta = Mouse.current.delta.ReadValue();
            yaw += delta.x * sensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
