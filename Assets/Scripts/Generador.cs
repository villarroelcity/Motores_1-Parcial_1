using UnityEngine;

namespace EnElCamino
{
    [RequireComponent(typeof(AudioSource))]
    public class Generador : MonoBehaviour, IInteractable
    {
        [SerializeField] private Juego gameFlow;
        [SerializeField] private Light indicatorLight;
        [SerializeField] private AudioClip switchSound;
        [SerializeField] private AudioClip generatorStartSound;
        [SerializeField] private AudioClip generatorMotorLoop;
        [SerializeField] private AudioSource motorAudioSource;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private ParticleSystem exhaust;
        private bool generatorOn;

        private void Awake()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<Juego>();
            if (motorAudioSource == null)
            {
                Transform motorTransform = transform.Find("GeneratorMotorAudio");
                if (motorTransform != null) motorAudioSource = motorTransform.GetComponent<AudioSource>();
            }
        }

        private void OnEnable()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<Juego>();
            if (gameFlow == null) return;
            gameFlow.GeneratorFuelDepleted += ShutDownGenerator;
            gameFlow.GameLost += ShutDownGenerator;
            gameFlow.GameFinished += ShutDownGenerator;
        }

        private void OnDisable()
        {
            if (gameFlow == null) return;
            gameFlow.GeneratorFuelDepleted -= ShutDownGenerator;
            gameFlow.GameLost -= ShutDownGenerator;
            gameFlow.GameFinished -= ShutDownGenerator;
        }

        private AudioSource MotorSource => motorAudioSource != null ? motorAudioSource : audioSource;

        private void Update()
        {
            if (generatorOn && gameFlow != null && (gameFlow.gameOver || gameFlow.finished))
                StopGeneratorAudio();
        }

        public string Prompt
        {
            get
            {
                if (generatorOn) return "Generador encendido";
                return "[E] Encender generador";
            }
        }

        public void Interact()
        {
            if (generatorOn || gameFlow == null || gameFlow.gameOver || gameFlow.finished) return;
            if (!gameFlow.insideGarage)
            {
                gameFlow.ShowMessage("Primero entra al taller por el hueco del techo.");
                return;
            }

            if (!gameFlow.TurnOnGenerator()) return;
            generatorOn = true;
            if (exhaust != null) exhaust.Play();
            if (switchSound != null) audioSource.PlayOneShot(switchSound);
            if (generatorStartSound != null)
            {
                MotorSource.PlayOneShot(generatorStartSound);
                Invoke(nameof(StartGeneratorMotor), generatorStartSound.length);
            }
            if (indicatorLight != null) indicatorLight.enabled = true;
        }

        private void StartGeneratorMotor()
        {
            if (!generatorOn || gameFlow == null || gameFlow.gameOver || gameFlow.finished || generatorMotorLoop == null) return;
            AudioSource motorSource = MotorSource;
            motorSource.clip = generatorMotorLoop;
            motorSource.loop = true;
            motorSource.Play();
        }

        private void StopGeneratorAudio()
        {
            if (exhaust != null) exhaust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            CancelInvoke(nameof(StartGeneratorMotor));
            if (audioSource != null)
            {
                audioSource.loop = false;
                audioSource.Stop();
            }
            if (motorAudioSource != null)
            {
                motorAudioSource.loop = false;
                motorAudioSource.Stop();
            }
        }

        private void ShutDownGenerator()
        {
            generatorOn = false;
            StopGeneratorAudio();
            if (indicatorLight != null) indicatorLight.enabled = false;
        }
    }
}
