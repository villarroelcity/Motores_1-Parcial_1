using UnityEngine;
using UnityEngine.InputSystem;

namespace EnElCamino
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float speed = 4.5f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float turnDegreesPerSecond = 90f;
        [SerializeField] private float fallDeathHeight = -3f;

        private CharacterController controller;
        private Vector3 verticalVelocity;
        private Camera mainCamera;
        private GameFlow gameFlow;
        private bool trackingFall;
        private bool controlledRoofDescent;
        private float fallStartY;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            mainCamera = Camera.main;
            gameFlow = FindFirstObjectByType<GameFlow>();
        }

        private void Update()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            if (gameFlow != null && !gameFlow.finished && !gameFlow.gameOver && transform.position.y <= fallDeathHeight)
            {
                gameFlow.GameOver();
                return;
            }

            if (gameFlow != null && (gameFlow.fueling || gameFlow.movementLocked)) return;

            bool groundedBeforeMove = controller.isGrounded;
            if (groundedBeforeMove && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = -2f;
                trackingFall = false;
            }
            else if (!groundedBeforeMove && !trackingFall)
            {
                trackingFall = true;
                fallStartY = transform.position.y;
            }

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
                Quaternion desiredRotation = Quaternion.LookRotation(worldDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, turnDegreesPerSecond * Time.deltaTime);
                controller.Move(worldDirection * speed * Time.deltaTime);
            }

            verticalVelocity.y += gravity * Time.deltaTime;
            controller.Move(verticalVelocity * Time.deltaTime);

            if (trackingFall && controller.isGrounded)
            {
                float fallDistance = fallStartY - transform.position.y;
                trackingFall = false;
                if (fallDistance >= 2.5f && gameFlow != null && !gameFlow.gameOver && !gameFlow.finished)
                    gameFlow.GameOver();
            }
        }

        public void ResetFallTracking()
        {
            trackingFall = false;
            verticalVelocity = new Vector3(0f, -2f, 0f);
        }

        public bool IsInControlledRoofDescent => controlledRoofDescent;

        public void SetControlledRoofDescent(bool value)
        {
            controlledRoofDescent = value;
            ResetFallTracking();
        }

        public void RegisterRoofFall()
        {
            if (trackingFall) return;
            trackingFall = true;
            fallStartY = transform.position.y;
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
