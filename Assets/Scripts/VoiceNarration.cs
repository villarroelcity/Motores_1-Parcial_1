using UnityEngine;

namespace EnElCamino
{
    public class VoiceNarration : MonoBehaviour
    {
        [SerializeField] private AudioSource firstSpeaker;
        [SerializeField] private AudioClip[] firstSpeakerLines = new AudioClip[6];

        public void PlayFirst(int line)
        {
            Play(firstSpeaker, firstSpeakerLines, line);
        }

        private static void Play(AudioSource source, AudioClip[] lines, int line)
        {
            if (source == null || lines == null || line < 0 || line >= lines.Length) return;
            if (lines[line] != null) source.PlayOneShot(lines[line]);
        }
    }
}
