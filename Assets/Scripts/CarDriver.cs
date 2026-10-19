using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Reflection;

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
        private Component vppInput;
        private Rigidbody vehicleBody;
        private bool driving;
        private bool engineLoopPlaying;
        private bool vehicleReady = true;

        public bool IsReady => vehicleReady;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            vppInput = GetComponent("VPStandardInput");
            vehicleBody = GetComponent<Rigidbody>();

            if (vppInput != null)
            {
                if (vppInput is Behaviour behaviour) behaviour.enabled = false;
                vehicleReady = false;
            }
        }

        public void SetVehicleReady(bool ready)
        {
            vehicleReady = ready;
            if (!ready)
            {
                driving = false;
                SetVppInput(false);
                StopEngineLoop();
            }
        }

        public void EnterCar()
        {
            if (!vehicleReady || driving || !gameFlow.carHasFuel || gameFlow.gameOver) return;

            driving = true;
            if (engineLoopSound == null) engineLoopSound = accelerationSound;
            player.SetActive(false);
            cameraFollow.SetTarget(transform);
            SetVppInput(true);
            gameFlow.StartDriving();
        }

        private void Update()
        {
            if (!driving) return;
            if (gameFlow.gameOver || gameFlow.finished)
            {
                SetVppInputs(0f, 0f);
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

            if (vppInput != null)
            {
                SetVppInputs(forward, turn);
            }
            else
            {
                transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);
                transform.Translate(Vector3.forward * forward * speed * Time.deltaTime);
            }

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

        private void SetVppInput(bool enabled)
        {
            if (vppInput == null) return;
            if (vppInput is Behaviour behaviour) behaviour.enabled = enabled;
            if (!enabled) SetVppInputs(0f, 0f);
        }

        private void SetVppInputs(float forward, float turn)
        {
            SetVppField("externalThrottle", Mathf.Clamp01(Mathf.Max(0f, forward)));
            SetVppField("reverse", forward < -0.05f ? 1f : 0f);
            SetVppField("externalBrake", 0f);
            SetVppField("externalSteer", Mathf.Clamp(turn, -1f, 1f));
            SetVppField("externalIgnition", 1f);
        }

        private void SetVppField(string fieldName, float value)
        {
            if (vppInput == null) return;

            FieldInfo field = vppInput.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) return;

            try
            {
                Type type = field.FieldType;
                object converted = type == typeof(bool) ? value > 0.5f : Convert.ChangeType(value, type);
                field.SetValue(vppInput, converted);
            }
            catch (Exception)
            {
                // Vehicle Physics Pro versions expose these fields with different numeric types.
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
