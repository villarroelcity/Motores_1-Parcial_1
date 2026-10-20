using UnityEngine;
using UnityEngine.Audio;

namespace EnElCamino
{
    /// <summary>Plays the real Parcial 2 ambience, music and gameplay sounds.</summary>
    public class GameplayAudioController : MonoBehaviour
    {
        [SerializeField] private AudioClip ambientClip;
        [SerializeField] private AudioClip garageOpen;
        [SerializeField] private AudioMixer mixer;

        private GameFlow flow;
        private AudioSource ambientSource;
        private AudioSource musicSource;
        private AudioSource effectsSource;
        private AdaptiveMusicController adaptiveMusic;

        private void Awake()
        {
            flow = GetComponent<GameFlow>();
            ambientSource = CreateSource("AmbientAudio", true, 0.12f);
            adaptiveMusic = FindFirstObjectByType<AdaptiveMusicController>();
            effectsSource = CreateSource("EffectsAudio", false, 0.75f);

            ambientSource.clip = ambientClip;
            if (ambientClip != null) ambientSource.Play();
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
            source.outputAudioMixerGroup = FindGroup(sourceName.Contains("Ambient") ? "Ambience" : "SFX");
            return source;
        }

        private void PlayEffect(AudioClip clip)
        {
            if (effectsSource != null && clip != null) effectsSource.PlayOneShot(clip);
        }

        private void OnGeneratorStarted() { }
        private void OnGarageOpened() => PlayEffect(garageOpen);
        private void OnFuelCanFilled() { }

        private void OnDrivingStarted()
        {
            adaptiveMusic?.EnterTension();
        }

        private void OnGameFinished() { }

        private void OnGameLost() { }

        private void StopAllGameplayAudio()
        {
            if (ambientSource != null) ambientSource.Stop();
            if (musicSource != null) musicSource.Stop();
            if (effectsSource != null) effectsSource.Stop();
        }

        private AudioMixerGroup FindGroup(string groupName)
        {
            if (mixer == null) return null;
            AudioMixerGroup[] groups = mixer.FindMatchingGroups("Master/" + groupName);
            return groups.Length > 0 ? groups[0] : null;
        }
    }
}
