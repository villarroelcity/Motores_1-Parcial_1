using UnityEngine;
using UnityEngine.InputSystem;

namespace EnElCamino
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float speed = 4.5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float turnDegreesPerSecond = 90f;
        [SerializeField] private float fallDeathHeight = -3f;
        [SerializeField] private Animator animator;

        private CharacterController controller;
        private Vector3 verticalVelocity;
        private Juego gameFlow;
        private bool trackingFall;
        private bool controlledRoofDescent;
        private float fallStartY;
        [SerializeField] private AudioSource footstepSource;
        [SerializeField] private AudioClip footstepClip;
        private bool hanging;
        private bool falling;
        private bool interactionPoseLocked;
        private Vector3 interactionPosition;
        private Quaternion interactionRotation;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            gameFlow = FindFirstObjectByType<Juego>();
            if (animator == null) animator = GetComponent<Animator>();
        }

        private void Update()
        {
            bool frozen = interactionPoseLocked || gameFlow != null &&
                (gameFlow.fueling || gameFlow.gameOver || gameFlow.finished ||
                 gameFlow.movementLocked && !controlledRoofDescent);
            if (animator != null) animator.speed = frozen ? 0f : 1f;
            if (gameFlow != null && (gameFlow.gameOver || gameFlow.finished)) return;
            if (interactionPoseLocked)
            {
                transform.SetPositionAndRotation(interactionPosition, interactionRotation);
                verticalVelocity = Vector3.zero;
                return;
            }

            if (gameFlow == null) gameFlow = FindFirstObjectByType<Juego>();
            if (!controlledRoofDescent && gameFlow != null && !gameFlow.finished && !gameFlow.gameOver &&
                transform.position.y <= fallDeathHeight)
            {
                gameFlow.GameOver();
                return;
            }

            if (gameFlow != null && (gameFlow.fueling || gameFlow.movementLocked)) return;

            if (controller == null) return;

            if (hanging || falling) return;

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
            Vector3 previousPosition = transform.position;

            // Tank-style controls: horizontal input turns the character around its
            // own Y axis. It never translates the body in an arc around the camera.
            if (Mathf.Abs(input.x) > 0.01f)
                transform.Rotate(Vector3.up, input.x * turnDegreesPerSecond * Time.deltaTime, Space.Self);

            if (Mathf.Abs(input.y) > 0.01f)
            {
                bool sprinting = Keyboard.current != null &&
                                 (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
                float movementSpeed = sprinting ? Mathf.Max(speed, sprintSpeed) : speed;
                controller.Move(transform.forward * (input.y * movementSpeed * Time.deltaTime));
            }

            verticalVelocity.y += gravity * Time.deltaTime;
            controller.Move(verticalVelocity * Time.deltaTime);

            if (!controlledRoofDescent && trackingFall && controller.isGrounded)
            {
                float fallDistance = fallStartY - transform.position.y;
                trackingFall = false;
                if (fallDistance >= 2.5f && gameFlow != null && !gameFlow.gameOver && !gameFlow.finished)
                    gameFlow.GameOver();
            }

            float movementSpeedNow = Vector3.ProjectOnPlane(transform.position - previousPosition,
                Vector3.up).magnitude / Mathf.Max(Time.deltaTime, 0.001f);
            SetAnimation(movementSpeedNow > 0.2f ? 1 : 0, movementSpeedNow / speed);
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

        public void SetHanging(bool value)
        {
            hanging = value;
            SetAnimation(hanging ? 2 : falling ? 3 : 0);
        }

        public void SetFalling(bool value)
        {
            falling = value;
            SetAnimation(hanging ? 2 : falling ? 3 : 0);
        }

        public void SetInteractionPoseLocked(bool locked)
        {
            interactionPoseLocked = locked;
            if (!locked) return;

            interactionPosition = transform.position;
            interactionRotation = transform.rotation;
            verticalVelocity = Vector3.zero;
            if (animator != null) animator.speed = 0f;
        }

        private void SetAnimation(int state, float pace = 1f)
        {
            if (animator == null) return;
            animator.SetInteger("Estado", state);
            animator.SetFloat("Ritmo", Mathf.Clamp(pace, 0.65f, 1.8f));
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

        private void PlayFootstep()
        {
            if (footstepSource == null || footstepClip == null || Time.timeScale == 0f ||
                interactionPoseLocked || gameFlow != null && (gameFlow.fueling || gameFlow.movementLocked)) return;
            footstepSource.pitch = Random.Range(0.92f, 1.06f);
            footstepSource.PlayOneShot(footstepClip, Random.Range(0.82f, 1f));
        }

    }
}
