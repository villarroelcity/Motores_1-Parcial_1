using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using VehiclePhysics;

namespace EnElCamino
{
    [DefaultExecutionOrder(-100)]
    public class CarDriver : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private GameObject player;
        [SerializeField] private ThirdPersonCamera cameraFollow;
        [SerializeField] private float speed = 7f;
        [SerializeField] private float turnSpeed = 75f;

        private VPVehicleController vehicleController;
        private bool driving;
        private bool vehicleReady = true;
        private bool engineRequested;
        private float starterUntil;
        private float throttleInput;
        private float steerInput;
        private bool automaticTransmissionConfigured;
        private readonly List<StartupAudioVolume> startupAudioVolumes = new List<StartupAudioVolume>();
        private readonly List<StartupAudioMute> startupAudioMutes = new List<StartupAudioMute>();
        private float restoreStartupAudioAt = -1f;

        private struct StartupAudioVolume
        {
            public Component audioComponent;
            public FieldInfo volumeField;
            public float originalVolume;
        }

        private struct StartupAudioMute
        {
            public AudioSource source;
            public bool originalMute;
        }

        public bool IsReady => vehicleReady;
        public bool EngineRequested => engineRequested;

        private void Awake()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            vehicleController = GetComponent<VPVehicleController>();
            if (vehicleController == null) return;
            vehicleController.gearbox.type = Gearbox.Type.Automatic;
            // The new Input System writes the VPP bus directly. The legacy input
            // reader would otherwise overwrite these controls every physics step.
            VPStandardInput legacyInput = GetComponent<VPStandardInput>();
            if (legacyInput != null) legacyInput.enabled = false;
            vehicleReady = false;
            foreach (VPAudio audio in GetComponentsInChildren<VPAudio>(true))
            {
                audio.enabled = true;
                FieldInfo startupVolume = audio.GetType().GetField("engineStartupVolume", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (startupVolume != null && startupVolume.FieldType == typeof(float))
                {
                    startupAudioVolumes.Add(new StartupAudioVolume
                    {
                        audioComponent = audio,
                        volumeField = startupVolume,
                        originalVolume = (float)startupVolume.GetValue(audio)
                    });
                }
                foreach (AudioSource source in audio.GetComponentsInChildren<AudioSource>(true))
                {
                    startupAudioMutes.Add(new StartupAudioMute
                    {
                        source = source,
                        originalMute = source.mute
                    });
                    source.enabled = true;
                    source.mute = false;
                }
            }
        }

        public void SetVehicleReady(bool ready)
        {
            vehicleReady = ready;
            if (ready) return;
            driving = false;
            throttleInput = 0f;
            steerInput = 0f;
        }

        public void EnterCar()
        {
            if (!vehicleReady || driving || gameFlow == null || !gameFlow.carHasFuel || gameFlow.gameOver || gameFlow.finished) return;
            driving = true;
            StartEngineAudio(true);
            if (player != null) player.SetActive(false);
            CinemachineFollowSetup cameraRig = FindFirstObjectByType<CinemachineFollowSetup>();
            if (cameraRig != null) cameraRig.SetTarget(transform);
            else if (cameraFollow != null) cameraFollow.SetTarget(transform);
            gameFlow.StartDriving();
        }

        private void Update()
        {
            if (restoreStartupAudioAt >= 0f && Time.time >= restoreStartupAudioAt)
                RestoreStartupAudioVolume();

            if (gameFlow != null && (gameFlow.gameOver || gameFlow.finished))
            {
                StopEngineAudio();
                driving = false;
                return;
            }
            throttleInput = 0f;
            steerInput = 0f;
            if (!driving || Keyboard.current == null) return;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) throttleInput = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) throttleInput = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) steerInput = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) steerInput = 1f;
            if (vehicleController == null)
            {
                transform.Rotate(0f, steerInput * turnSpeed * Time.deltaTime, 0f);
                transform.Translate(Vector3.forward * throttleInput * speed * Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (vehicleController == null) return;
            if (!vehicleController.initialized)
            {
                // Apply before VPP initialization as well: this project car prefab
                // is authored with a manual gearbox, but gameplay requires automatic.
                vehicleController.gearbox.type = Gearbox.Type.Automatic;
                return;
            }

            if (!automaticTransmissionConfigured)
            {
                vehicleController.gearbox.type = Gearbox.Type.Automatic;
                automaticTransmissionConfigured = true;
            }

            int[] input = vehicleController.data.Get(Channel.Input);
            bool stopped = gameFlow != null && (gameFlow.gameOver || gameFlow.finished);
            bool engineOn = engineRequested && !stopped;
            // VPP bus Key: -1 = off, 0 = ignition on, 1 = starter motor.
            input[InputData.Key] = !engineOn ? -1 : Time.time < starterUntil ? 1 : 0;
            input[InputData.Steer] = driving ? Mathf.RoundToInt(steerInput * 10000f) : 0;
            bool reversing = throttleInput < -0.05f;
            float throttle = driving ? Mathf.Abs(throttleInput) : !vehicleReady ? 0.18f : 0f;
            input[InputData.Throttle] = engineOn ? Mathf.RoundToInt(throttle * 10000f) : 0;
            input[InputData.Brake] = (!driving && vehicleReady) || stopped ? 10000 : 0;
            input[InputData.Handbrake] = !driving && vehicleReady ? 10000 : 0;
            input[InputData.Clutch] = 0;
            input[InputData.ManualGear] = 0;
            // VPP automatic modes: P=1 while parked, R=2 in reverse, N=3 for the
            // scripted arrival, and D=4 for forward driving (with auto-shifting).
            input[InputData.AutomaticGear] = driving ? (reversing ? 2 : 4) : vehicleReady ? 1 : 3;
        }

        public void StartEngineAudio(bool playStartSound)
        {
            if (!engineRequested)
            {
                // VPP needs a short starter-key pulse to transition from off to running.
                // On the intro the car is already moving, so silence only the starter clip,
                // then let VPP's original RPM-driven engine audio play normally.
                starterUntil = Time.time + 1.25f;
                if (!playStartSound)
                {
                    SetStartupAudioVolume(0f);
                    SetVehicleAudioMuted(true);
                    restoreStartupAudioAt = starterUntil + 0.1f;
                }
            }
            engineRequested = true;
        }

        public void StopEngineAudio()
        {
            engineRequested = false;
            throttleInput = 0f;
            steerInput = 0f;
        }

        private void SetStartupAudioVolume(float volume)
        {
            foreach (StartupAudioVolume entry in startupAudioVolumes)
            {
                if (entry.audioComponent != null && entry.volumeField != null)
                    entry.volumeField.SetValue(entry.audioComponent, volume);
            }
        }

        private void RestoreStartupAudioVolume()
        {
            foreach (StartupAudioVolume entry in startupAudioVolumes)
            {
                if (entry.audioComponent != null && entry.volumeField != null)
                    entry.volumeField.SetValue(entry.audioComponent, entry.originalVolume);
            }
            restoreStartupAudioAt = -1f;
            SetVehicleAudioMuted(false);
        }

        private void SetVehicleAudioMuted(bool muted)
        {
            foreach (StartupAudioMute entry in startupAudioMutes)
            {
                if (entry.source != null)
                    entry.source.mute = muted || entry.originalMute;
            }
        }
    }
}
