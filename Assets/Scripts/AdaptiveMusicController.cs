using UnityEngine;

namespace EnElCamino
{
    public class AdaptiveMusicController : MonoBehaviour
    {
        public AudioSource explorationSource;
        public AudioSource tensionSource;
        public AudioClip explorationClip;
        public AudioClip tensionClip;
        public float transitionDuration = 1.5f;
        public float explorationVolume = 0.22f;
        public float tensionVolume = 0.32f;

        private bool tensionState;
        private float transition;

        private void Start()
        {
            ConfigureSource(explorationSource, explorationClip, explorationVolume);
            ConfigureSource(tensionSource, tensionClip, 0f);
            if (explorationSource != null) explorationSource.Play();
            if (tensionSource != null) tensionSource.Play();
        }

        private void Update()
        {
            float direction = tensionState ? 1f : 0f;
            transition = Mathf.MoveTowards(transition, direction, Time.unscaledDeltaTime / Mathf.Max(0.01f, transitionDuration));
            if (explorationSource != null) explorationSource.volume = Mathf.Lerp(explorationVolume, 0f, transition);
            if (tensionSource != null) tensionSource.volume = Mathf.Lerp(0f, tensionVolume, transition);
        }

        public void EnterTension()
        {
            tensionState = true;
            if (tensionSource != null && !tensionSource.isPlaying) tensionSource.Play();
        }

        public void StopMusic()
        {
            if (explorationSource != null) explorationSource.Stop();
            if (tensionSource != null) tensionSource.Stop();
        }

        private static void ConfigureSource(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null) return;
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = volume;
        }
    }
}
