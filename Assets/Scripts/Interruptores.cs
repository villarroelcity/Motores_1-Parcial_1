using UnityEngine;

namespace EnElCamino
{
    public class Interruptores : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private PortonTaller garageDoor;
        [SerializeField] private Transform lever;
        [SerializeField] private AudioClip switchSound;
        private AudioSource switchAudio;
        private bool switchedOn;
        public bool IsOn => switchedOn;

        private void Awake()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            if (garageDoor == null) garageDoor = FindFirstObjectByType<PortonTaller>();
            switchAudio = GetComponent<AudioSource>();
            if (switchAudio == null) switchAudio = gameObject.AddComponent<AudioSource>();
            switchAudio.playOnAwake = false;
            switchAudio.spatialBlend = 1f;
            switchAudio.minDistance = 1f;
            switchAudio.maxDistance = 12f;
            switchAudio.volume = 0.45f;
        }

        public string Prompt
        {
            get
            {
                if (gameFlow == null || !gameFlow.insideGarage) return "Interruptor del porton (desde adentro)";
                if (!gameFlow.stationPowerConnected) return "Primero enchufa el generador a la caja electrica";
                return switchedOn ? "[E] Apagar tecla / cerrar porton" : "[E] Encender tecla / abrir porton";
            }
        }

        public void Interact()
        {
            if (gameFlow == null || gameFlow.gameOver || gameFlow.finished) return;
            if (!gameFlow.stationPowerConnected)
            {
                gameFlow.ShowMessage("Primero enchufa el generador a la caja electrica.");
                return;
            }
            if (!gameFlow.insideGarage)
            {
                gameFlow.ShowMessage("El interruptor esta del lado interior del taller.");
                return;
            }

            if (garageDoor == null)
            {
                gameFlow.ShowMessage("El interruptor no esta conectado al porton.");
                return;
            }
            bool next = !garageDoor.IsOpen;
            if (!garageDoor.TrySetOpen(next)) return;
            switchedOn = next;
            if (lever != null) lever.localRotation = Quaternion.Euler(switchedOn ? -12f : 12f, 0f, 0f);
            if (switchSound != null) switchAudio.PlayOneShot(switchSound);
        }
    }
}
