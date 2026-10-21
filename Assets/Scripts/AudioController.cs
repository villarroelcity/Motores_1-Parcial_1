using UnityEngine;
using UnityEngine.Audio;

namespace EnElCamino
{
    /// <summary>Plays the real Parcial 2 ambience, music and gameplay sounds.</summary>
    public class AudioController : MonoBehaviour
    {
        [SerializeField] private AudioClip ambientClip;
        [SerializeField] private AudioClip garageOpen;
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioSource explorationSource;
        [SerializeField] private AudioSource tensionSource;
        [SerializeField] private AudioClip explorationClip;
        [SerializeField] private AudioClip tensionClip;
        [SerializeField] private float transitionDuration = 1.5f;
        [SerializeField] private float explorationVolume = 0.22f;
        [SerializeField] private float tensionVolume = 0.32f;

        private GameFlow flow;
        private AudioSource ambientSource;
        private AudioSource effectsSource;
        private bool tensionState;
        private float transition;

        private void Awake()
        {
            flow = GetComponent<GameFlow>();
            if (flow == null) flow = FindFirstObjectByType<GameFlow>();
            ambientSource = CreateSource("AmbientAudio", true, 0.12f);
            effectsSource = CreateSource("EffectsAudio", false, 0.75f);

            ConfigureMusicSource(explorationSource, explorationClip, explorationVolume);
            ConfigureMusicSource(tensionSource, tensionClip, 0f);
            if (explorationSource != null) explorationSource.Play();
            if (tensionSource != null) tensionSource.Play();

            ambientSource.clip = ambientClip;
            if (ambientClip != null) ambientSource.Play();
        }

        private void OnEnable()
        {
            if (flow == null) flow = GetComponent<GameFlow>();
            if (flow == null) return;
            flow.GarageDoorMotionStarted += OnGarageDoorMotionStarted;
            flow.DrivingStarted += OnDrivingStarted;
        }

        private void OnDisable()
        {
            if (flow == null) return;
            flow.GarageDoorMotionStarted -= OnGarageDoorMotionStarted;
            flow.DrivingStarted -= OnDrivingStarted;
        }

        private void Update()
        {
            float direction = tensionState ? 1f : 0f;
            transition = Mathf.MoveTowards(transition, direction,
                Time.unscaledDeltaTime / Mathf.Max(0.01f, transitionDuration));
            if (explorationSource != null) explorationSource.volume = Mathf.Lerp(explorationVolume, 0f, transition);
            if (tensionSource != null) tensionSource.volume = Mathf.Lerp(0f, tensionVolume, transition);
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

        private void OnGarageDoorMotionStarted() => PlayEffect(garageOpen);

        private void OnDrivingStarted()
        {
            tensionState = true;
            if (tensionSource != null && !tensionSource.isPlaying) tensionSource.Play();
        }

        public void StopMusic()
        {
            if (ambientSource != null) ambientSource.Stop();
            if (effectsSource != null) effectsSource.Stop();
            if (explorationSource != null) explorationSource.Stop();
            if (tensionSource != null) tensionSource.Stop();
        }

        private static void ConfigureMusicSource(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null) return;
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = volume;
        }

        private AudioMixerGroup FindGroup(string groupName)
        {
            if (mixer == null) return null;
            AudioMixerGroup[] groups = mixer.FindMatchingGroups("Master/" + groupName);
            return groups.Length > 0 ? groups[0] : null;
        }
    }
}
