using UnityEngine;
using UnityEngine.InputSystem;

namespace EnElCamino
{
    public class CarDriver : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private GameObject player;
        [SerializeField] private ThirdPersonCamera cameraFollow;
        [SerializeField] private float speed = 7f;
        [SerializeField] private float turnSpeed = 75f;
        private bool driving;

        public void EnterCar()
        {
            if (driving || !gameFlow.carHasFuel || gameFlow.gameOver) return;

            driving = true;
            player.SetActive(false);
            cameraFollow.SetTarget(transform);
            gameFlow.StartDriving();
        }

        private void Update()
        {
            if (!driving || gameFlow.gameOver || gameFlow.finished) return;
            if (Keyboard.current == null) return;

            float forward = 0f;
            float turn = 0f;

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) forward = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) forward = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) turn = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) turn = 1f;

            transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);
            transform.Translate(Vector3.forward * forward * speed * Time.deltaTime);
        }
    }
}
