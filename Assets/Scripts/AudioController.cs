using UnityEngine;

namespace EnElCamino
{
    /// <summary>Plays the real Parcial 2 ambience, music and gameplay sounds.</summary>
    public class AudioController : MonoBehaviour
    {
        [SerializeField] private AudioSource explorationSource;
        [SerializeField] private AudioSource tensionSource;
        [SerializeField] private float transitionDuration = 1.5f;
        [SerializeField] private float explorationVolume = 0.22f;
        [SerializeField] private float tensionVolume = 0.32f;

        [SerializeField] private Juego flow;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource garageDoorSource;
        private bool tensionState;
        private float transition;

        private void Awake()
        {
            if (ambientSource.clip != null) ambientSource.Play();
            explorationSource.Play();
            tensionSource.Play();
        }

        private void OnEnable()
        {
            if (flow == null) flow = GetComponent<Juego>();
            if (flow == null) return;
            flow.GarageDoorMotionStarted += OnGarageDoorMotionStarted;
            flow.TensionStarted += OnTensionStarted;
        }

        private void OnDisable()
        {
            if (flow == null) return;
            flow.GarageDoorMotionStarted -= OnGarageDoorMotionStarted;
            flow.TensionStarted -= OnTensionStarted;
        }

        private void Update()
        {
            float direction = tensionState ? 1f : 0f;
            transition = Mathf.MoveTowards(transition, direction,
                Time.unscaledDeltaTime / Mathf.Max(0.01f, transitionDuration));
            if (explorationSource != null) explorationSource.volume = Mathf.Lerp(explorationVolume, 0f, transition);
            if (tensionSource != null) tensionSource.volume = Mathf.Lerp(0f, tensionVolume, transition);
        }

        private void OnGarageDoorMotionStarted()
        {
            if (garageDoorSource == null || garageDoorSource.clip == null) return;

            garageDoorSource.Stop();
            // The supplied porton recording contains 1.19 seconds of silence
            // before the mechanism starts. Skip that silence so the sound and
            // the first movement of the door begin together.
            garageDoorSource.time = Mathf.Min(1.19f, Mathf.Max(0f, garageDoorSource.clip.length - 0.05f));
            garageDoorSource.Play();
        }

        private void OnTensionStarted()
        {
            tensionState = true;
            if (tensionSource != null && !tensionSource.isPlaying) tensionSource.Play();
        }

        public void StopMusic()
        {
            if (ambientSource != null) ambientSource.Stop();
            if (garageDoorSource != null) garageDoorSource.Stop();
            if (explorationSource != null) explorationSource.Stop();
            if (tensionSource != null) tensionSource.Stop();
        }

    }
}
