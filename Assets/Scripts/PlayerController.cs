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
        [SerializeField] private float walkCycleSpeed = 8f;
        [SerializeField] private float limbSwing = 28f;

        private CharacterController controller;
        private Vector3 verticalVelocity;
        private Juego gameFlow;
        private bool trackingFall;
        private bool controlledRoofDescent;
        private float fallStartY;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightFoot;
        [SerializeField] private AudioSource footstepSource;
        [SerializeField] private AudioClip footstepClip;
        private Vector3 headRestPosition;
        private Vector3 torsoRestPosition;
        private float walkPhase;
        private float idlePhase;
        private bool hanging;
        private bool falling;
        private bool interactionPoseLocked;
        private Vector3 interactionPosition;
        private Quaternion interactionRotation;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            gameFlow = FindFirstObjectByType<Juego>();
            headRestPosition = head.localPosition;
            torsoRestPosition = torso.localPosition;
        }

        private void Update()
        {
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

            if (hanging)
            {
                AnimateHang();
                return;
            }
            if (falling)
            {
                AnimateFalling();
                return;
            }

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

            AnimateBody();
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

        public void SetHanging(bool value) => hanging = value;

        public void SetFalling(bool value) => falling = value;

        public void SetInteractionPoseLocked(bool locked)
        {
            interactionPoseLocked = locked;
            if (!locked) return;

            interactionPosition = transform.position;
            interactionRotation = transform.rotation;
            verticalVelocity = Vector3.zero;
            walkPhase = 0f;
        }

        private void AnimateBody()
        {
            Vector3 horizontalVelocity = controller.velocity;
            horizontalVelocity.y = 0f;
            bool walking = horizontalVelocity.sqrMagnitude > 0.04f || HasMovementInput();
            if (walking)
            {
                float speedFactor = Mathf.Clamp(horizontalVelocity.magnitude / 4.5f, 0.65f, 1.35f);
                float previousPhase = walkPhase;
                walkPhase += Time.deltaTime * walkCycleSpeed * speedFactor;
                AnimateWalk();
                if (Mathf.Sign(Mathf.Sin(previousPhase)) != Mathf.Sign(Mathf.Sin(walkPhase)))
                    PlayFootstep();
            }
            else
            {
                walkPhase = Mathf.Lerp(walkPhase, 0f, Time.deltaTime * 8f);
                idlePhase += Time.deltaTime * 1.8f;
                AnimateIdle();
            }
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

        private bool HasMovementInput()
        {
            if (Keyboard.current == null) return false;
            return Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed
                || Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed
                || Keyboard.current.upArrowKey.isPressed || Keyboard.current.leftArrowKey.isPressed
                || Keyboard.current.downArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
        }

        private void AnimateWalk()
        {
            if (leftLeg == null || rightLeg == null) return;
            float swing = Mathf.Sin(walkPhase) * limbSwing;
            leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
            rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            leftArm.localRotation = Quaternion.Euler(-swing * 0.75f, 0f, -7f);
            rightArm.localRotation = Quaternion.Euler(swing * 0.75f, 0f, 7f);
            leftFoot.localRotation = Quaternion.Euler(-swing * 0.30f, 0f, 0f);
            rightFoot.localRotation = Quaternion.Euler(swing * 0.30f, 0f, 0f);
            torso.localPosition = torsoRestPosition + Vector3.up * (Mathf.Abs(Mathf.Sin(walkPhase * 2f)) * 0.025f);
            head.localPosition = headRestPosition + Vector3.up * (Mathf.Abs(Mathf.Sin(walkPhase * 2f)) * 0.04f);
        }

        private void AnimateIdle()
        {
            float breathing = Mathf.Sin(idlePhase) * 0.025f;
            float weightShift = Mathf.Sin(idlePhase * 0.5f) * 2.5f;
            leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(0f, 0f, weightShift), Time.deltaTime * 8f);
            rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(0f, 0f, weightShift), Time.deltaTime * 8f);
            leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(breathing * 18f, 0f, -7f), Time.deltaTime * 8f);
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-breathing * 18f, 0f, 7f), Time.deltaTime * 8f);
            leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            torso.localPosition = Vector3.Lerp(torso.localPosition, torsoRestPosition + Vector3.up * breathing, Time.deltaTime * 8f);
            head.localPosition = Vector3.Lerp(head.localPosition, headRestPosition + Vector3.up * breathing * 1.2f, Time.deltaTime * 8f);
        }

        private void AnimateHang()
        {
            leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(0f, 0f, 158f), Time.deltaTime * 10f);
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(0f, 0f, -158f), Time.deltaTime * 10f);
            leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(20f, 0f, -5f), Time.deltaTime * 6f);
            rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(-18f, 0f, 5f), Time.deltaTime * 6f);
            leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.Euler(-12f, 0f, 0f), Time.deltaTime * 6f);
            rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.Euler(10f, 0f, 0f), Time.deltaTime * 6f);
            torso.localPosition = Vector3.Lerp(torso.localPosition, torsoRestPosition + Vector3.down * 0.035f, Time.deltaTime * 6f);
            head.localPosition = Vector3.Lerp(head.localPosition, headRestPosition, Time.deltaTime * 6f);
        }

        private void AnimateFalling()
        {
            float kick = Mathf.Sin(Time.time * 8f) * 18f;
            leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(0f, 0f, 58f), Time.deltaTime * 8f);
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(0f, 0f, -58f), Time.deltaTime * 8f);
            leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(kick, 0f, -8f), Time.deltaTime * 7f);
            rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(-kick, 0f, 8f), Time.deltaTime * 7f);
            leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.Euler(-kick * 0.35f, 0f, 0f), Time.deltaTime * 7f);
            rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.Euler(kick * 0.35f, 0f, 0f), Time.deltaTime * 7f);
            torso.localPosition = Vector3.Lerp(torso.localPosition, torsoRestPosition, Time.deltaTime * 7f);
            head.localPosition = Vector3.Lerp(head.localPosition, headRestPosition, Time.deltaTime * 7f);
        }

        private void PlayFootstep()
        {
            if (footstepSource == null || footstepClip == null) return;
            footstepSource.pitch = Random.Range(0.92f, 1.06f);
            footstepSource.PlayOneShot(footstepClip, Random.Range(0.82f, 1f));
        }

    }
}
