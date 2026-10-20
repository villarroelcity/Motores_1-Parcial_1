using UnityEngine;

namespace EnElCamino
{
    [RequireComponent(typeof(AudioSource))]
    public class GeneratorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Renderer indicator;
        [SerializeField] private Light indicatorLight;
        [SerializeField] private AudioClip switchSound;
        [SerializeField] private AudioClip generatorStartSound;
        [SerializeField] private AudioClip generatorMotorLoop;
        [SerializeField] private AudioSource motorAudioSource;
        private AudioSource audioSource;
        private bool generatorOn;

        private void Awake()
        {
            EnsureAudioSource();
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            if (motorAudioSource == null)
            {
                Transform motorTransform = transform.Find("GeneratorMotorAudio");
                if (motorTransform != null) motorAudioSource = motorTransform.GetComponent<AudioSource>();
            }
        }

        private AudioSource MotorSource => motorAudioSource != null ? motorAudioSource : EnsureAudioSource();

        private AudioSource EnsureAudioSource()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            return audioSource;
        }

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
                return "[E] Presionar botón rojo";
            }
        }

        public void Interact()
        {
            if (generatorOn || gameFlow == null || gameFlow.gameOver || gameFlow.finished) return;
            if (!gameFlow.insideGarage)
            {
                gameFlow.ShowMessage("Primero entrá al taller por el hueco del techo y bajá por la escalera.");
                return;
            }

            if (!gameFlow.TurnOnGenerator()) return;
            generatorOn = true;
            if (switchSound != null) EnsureAudioSource().PlayOneShot(switchSound);
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
            CancelInvoke(nameof(StartGeneratorMotor));
            if (audioSource != null)
            {
                audioSource.loop = false;
                audioSource.Stop();
            }
            AudioSource motorSource = MotorSource;
            motorSource.loop = false;
            motorSource.Stop();
        }
    }
}
