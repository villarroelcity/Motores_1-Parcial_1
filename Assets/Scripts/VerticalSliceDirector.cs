using System.Collections;
using UnityEngine;

namespace EnElCamino
{
    /// <summary>
    /// Integrates the Parcial 2 systems around the existing gameplay loop:
    /// audio feedback, final escape challenge, warning lights and particles.
    /// </summary>
    public class VerticalSliceDirector : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Transform vehicle;
        [SerializeField] private Transform exit;
        [SerializeField] private Light[] warningLights;
        [SerializeField] private float challengeDuration = 35f;

        private AudioSource ambientSource;
        private AudioClip ambientClip;
        private ParticleSystem feedbackParticles;
        private StationThreatAI threat;
        private float challengeTimeLeft;
        private bool challengeActive;
        private bool subscribed;

        private void Awake()
        {
            if (gameFlow == null) gameFlow = GetComponent<GameFlow>();
            if (vehicle == null)
            {
                GameObject car = GameObject.Find("VehiculoConRuedas");
                if (car != null) vehicle = car.transform;
            }
            if (exit == null)
            {
                GameObject exitObject = GameObject.Find("SalidaAlCamino");
                if (exitObject != null) exit = exitObject.transform;
            }

            if (warningLights == null || warningLights.Length == 0)
            {
                var foundLights = new System.Collections.Generic.List<Light>();
                foreach (Light candidate in FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (candidate.name.Contains("PosteLuz")) foundLights.Add(candidate);
                }
                warningLights = foundLights.ToArray();
            }

            CreateAmbientAudio();
            CreateFeedbackParticles();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (!subscribed || gameFlow == null) return;
            gameFlow.GeneratorStarted -= OnGeneratorStarted;
            gameFlow.DrivingStarted -= OnDrivingStarted;
            gameFlow.GameFinished -= OnGameFinished;
            gameFlow.GameLost -= OnGameLost;
            subscribed = false;
        }

        private void Subscribe()
        {
            if (subscribed || gameFlow == null) return;
            gameFlow.GeneratorStarted += OnGeneratorStarted;
            gameFlow.DrivingStarted += OnDrivingStarted;
            gameFlow.GameFinished += OnGameFinished;
            gameFlow.GameLost += OnGameLost;
            subscribed = true;
        }

        private void Update()
        {
            if (!challengeActive || gameFlow == null || gameFlow.finished || gameFlow.gameOver) return;

            challengeTimeLeft -= Time.deltaTime;
            if (challengeTimeLeft <= 0f)
            {
                challengeActive = false;
                gameFlow.ShowMessage("La amenaza quedó atrás. ¡Llegá a la ruta!");
                StopThreat();
                return;
            }

            if (gameFlow.UI != null && Mathf.FloorToInt(challengeTimeLeft) % 5 == 0)
                gameFlow.UI.SetObjective("DESAFÍO FINAL\nESCAPÁ A LA RUTA\nAmenaza activa: " + Mathf.CeilToInt(challengeTimeLeft) + " s");
        }

        private void OnGeneratorStarted()
        {
            PlayTone(520f, 0.12f, 0.16f);
            EmitFeedback(Color.yellow, 18);
        }

        private void OnDrivingStarted()
        {
            challengeActive = true;
            challengeTimeLeft = challengeDuration;
            PlayTone(120f, 0.6f, 0.22f);
            StartCoroutine(WarningPulse());
            EmitFeedback(Color.red, 70);
            SpawnThreat();
            gameFlow.ShowMessage("¡DESAFÍO FINAL! Una amenaza te sigue: escapá por la ruta.");
            if (gameFlow.UI != null)
                gameFlow.UI.SetObjective("DESAFÍO FINAL\nESCAPÁ A LA RUTA\nLa amenaza se acerca. Llegá a la salida.");
        }

        private void OnGameFinished()
        {
            challengeActive = false;
            StopThreat();
            PlayTone(880f, 0.25f, 0.2f);
            EmitFeedback(Color.green, 80);
        }

        private void OnGameLost()
        {
            challengeActive = false;
            StopThreat();
            PlayTone(80f, 0.7f, 0.25f);
        }

        private void SpawnThreat()
        {
            if (threat != null || vehicle == null) return;

            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "AmenazaIA";
            enemy.transform.position = vehicle.position - vehicle.forward * 12f + vehicle.right * 3f;
            enemy.transform.localScale = new Vector3(0.8f, 1.4f, 0.8f);
            Renderer renderer = enemy.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.55f, 0.02f, 0.02f);

            threat = enemy.AddComponent<StationThreatAI>();
            threat.Configure(vehicle, gameFlow, exit);
        }

        private void StopThreat()
        {
            if (threat == null) return;
            threat.StopThreat();
            Destroy(threat.gameObject, 1.5f);
            threat = null;
        }

        private IEnumerator WarningPulse()
        {
            while (challengeActive && !gameFlow.finished && !gameFlow.gameOver)
            {
                if (warningLights != null)
                {
                    foreach (Light warningLight in warningLights)
                        if (warningLight != null) warningLight.enabled = !warningLight.enabled;
                }
                yield return new WaitForSeconds(0.35f);
            }
        }

        private void CreateAmbientAudio()
        {
            if (GetComponent<GameplayAudioController>() != null) return;
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = true;
            ambientSource.spatialBlend = 0f;
            ambientSource.volume = 0.045f;
            ambientClip = CreateToneClip("StationAmbience", 110f, 4f, 0.05f);
            ambientSource.clip = ambientClip;
            ambientSource.Play();
        }

        private void CreateFeedbackParticles()
        {
            GameObject particleObject = new GameObject("VerticalSliceFeedbackParticles");
            particleObject.transform.SetParent(transform);
            particleObject.transform.localPosition = Vector3.up * 0.8f;
            feedbackParticles = particleObject.AddComponent<ParticleSystem>();
            var main = feedbackParticles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.9f;
            main.startSpeed = 3.5f;
            main.startSize = 0.12f;
            main.startColor = Color.yellow;
            main.maxParticles = 150;
            var emission = feedbackParticles.emission;
            emission.enabled = false;
            var shape = feedbackParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;
        }

        private void EmitFeedback(Color color, int count)
        {
            if (feedbackParticles == null) return;
            var main = feedbackParticles.main;
            main.startColor = color;
            feedbackParticles.Emit(count);
        }

        private void PlayTone(float frequency, float duration, float volume)
        {
            AudioClip clip = CreateToneClip("FeedbackTone", frequency, duration, volume);
            AudioSource.PlayClipAtPoint(clip, transform.position, volume);
            Destroy(clip, duration + 0.2f);
        }

        private AudioClip CreateToneClip(string name, float frequency, float duration, float amplitude)
        {
            const int sampleRate = 44100;
            int samples = Mathf.Max(1, Mathf.CeilToInt(sampleRate * duration));
            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float envelope = Mathf.Sin(Mathf.PI * i / samples);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * amplitude * envelope;
            }
            clip.SetData(data, 0);
            return clip;
        }
    }
}
