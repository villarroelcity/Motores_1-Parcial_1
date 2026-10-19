using UnityEngine;

namespace EnElCamino
{
    /// <summary>Plays the real Parcial 2 ambience, music and gameplay sounds.</summary>
    public class GameplayAudioController : MonoBehaviour
    {
        [SerializeField] private AudioClip ambientClip;
        [SerializeField] private AudioClip explorationMusic;
        [SerializeField] private AudioClip tensionMusic;
        [SerializeField] private AudioClip generatorStart;
        [SerializeField] private AudioClip garageOpen;
        [SerializeField] private AudioClip pumpSound;
        [SerializeField] private AudioClip motorStart;
        [SerializeField] private AudioClip victory;
        [SerializeField] private AudioClip gameOver;

        private GameFlow flow;
        private AudioSource ambientSource;
        private AudioSource musicSource;
        private AudioSource effectsSource;

        private void Awake()
        {
            flow = GetComponent<GameFlow>();
            ambientSource = CreateSource("AmbientAudio", true, 0.45f);
            musicSource = CreateSource("MusicAudio", true, 0.30f);
            effectsSource = CreateSource("EffectsAudio", false, 0.75f);

            ambientSource.clip = ambientClip;
            if (ambientClip != null) ambientSource.Play();
            PlayMusic(explorationMusic);
        }

        private void OnEnable()
        {
            if (flow == null) flow = GetComponent<GameFlow>();
            if (flow == null) return;
            flow.GeneratorStarted += OnGeneratorStarted;
            flow.GarageOpened += OnGarageOpened;
            flow.FuelCanFilled += OnFuelCanFilled;
            flow.DrivingStarted += OnDrivingStarted;
            flow.GameFinished += OnGameFinished;
            flow.GameLost += OnGameLost;
        }

        private void OnDisable()
        {
            if (flow == null) return;
            flow.GeneratorStarted -= OnGeneratorStarted;
            flow.GarageOpened -= OnGarageOpened;
            flow.FuelCanFilled -= OnFuelCanFilled;
            flow.DrivingStarted -= OnDrivingStarted;
            flow.GameFinished -= OnGameFinished;
            flow.GameLost -= OnGameLost;
        }

        private AudioSource CreateSource(string sourceName, bool loop, float volume)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = volume;
            return source;
        }

        private void PlayMusic(AudioClip clip)
        {
            if (musicSource == null || clip == null) return;
            musicSource.Stop();
            musicSource.clip = clip;
            musicSource.Play();
        }

        private void PlayEffect(AudioClip clip)
        {
            if (effectsSource != null && clip != null) effectsSource.PlayOneShot(clip);
        }

        private void OnGeneratorStarted() => PlayEffect(generatorStart);
        private void OnGarageOpened() => PlayEffect(garageOpen);
        private void OnFuelCanFilled() => PlayEffect(pumpSound);

        private void OnDrivingStarted()
        {
            PlayEffect(motorStart);
            PlayMusic(tensionMusic);
        }

        private void OnGameFinished() => PlayEffect(victory);
        private void OnGameLost() => PlayEffect(gameOver);
    }
}
