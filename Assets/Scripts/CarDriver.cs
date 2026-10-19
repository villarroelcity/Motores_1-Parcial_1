using UnityEngine;
using UnityEngine.InputSystem;

namespace EnElCamino
{
    [RequireComponent(typeof(AudioSource))]
    public class CarDriver : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private GameObject player;
        [SerializeField] private ThirdPersonCamera cameraFollow;
        [SerializeField] private float speed = 7f;
        [SerializeField] private float turnSpeed = 75f;
        [SerializeField] private AudioClip accelerationSound;
        [SerializeField] private AudioClip engineLoopSound;
        private AudioSource audioSource;
        private bool driving;
        private bool engineLoopPlaying;

        private void Awake() => audioSource = GetComponent<AudioSource>();

        public void EnterCar()
        {
            if (driving || !gameFlow.carHasFuel || gameFlow.gameOver) return;

            driving = true;
            if (engineLoopSound == null) engineLoopSound = accelerationSound;
            player.SetActive(false);
            cameraFollow.SetTarget(transform);
            gameFlow.StartDriving();
        }

        private void Update()
        {
            if (!driving) return;
            if (gameFlow.gameOver || gameFlow.finished)
            {
                StopEngineLoop();
                return;
            }
            if (Keyboard.current == null) return;

            float forward = 0f;
            float turn = 0f;

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) forward = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) forward = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) turn = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) turn = 1f;

            transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);
            transform.Translate(Vector3.forward * forward * speed * Time.deltaTime);

            if (forward != 0f && !engineLoopPlaying && engineLoopSound != null)
            {
                engineLoopPlaying = true;
                audioSource.clip = engineLoopSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            else if (forward == 0f)
            {
                StopEngineLoop();
            }
        }

        private void StopEngineLoop()
        {
            if (!engineLoopPlaying) return;
            engineLoopPlaying = false;
            audioSource.loop = false;
            audioSource.Stop();
        }
    }
}
