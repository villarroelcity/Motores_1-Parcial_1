using UnityEngine;
using UnityEngine.InputSystem;

namespace EnElCamino
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float speed = 4.5f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float turnSpeed = 12f;

        private CharacterController controller;
        private Vector3 verticalVelocity;
        private Camera mainCamera;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            Vector2 input = ReadMovement();
            Vector3 direction = new Vector3(input.x, 0f, input.y);

            if (mainCamera != null && direction.sqrMagnitude > 0.01f)
            {
                Vector3 forward = mainCamera.transform.forward;
                Vector3 right = mainCamera.transform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
                Vector3 worldDirection = (forward * direction.z + right * direction.x).normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(worldDirection), turnSpeed * Time.deltaTime);
                controller.Move(worldDirection * speed * Time.deltaTime);
            }

            if (controller.isGrounded && verticalVelocity.y < 0f) verticalVelocity.y = -2f;
            verticalVelocity.y += gravity * Time.deltaTime;
            controller.Move(verticalVelocity * Time.deltaTime);
        }

        private Vector2 ReadMovement()
        {
            if (Keyboard.current == null) return Vector2.zero;
            float x = 0f;
            float y = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
            return new Vector2(x, y).normalized;
        }
    }
}
