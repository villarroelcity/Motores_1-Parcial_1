using UnityEngine;

namespace EnElCamino
{
    public class GateSwitchInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private GarageDoorInteractable garageDoor;
        [SerializeField] private Transform lever;
        [SerializeField] private AudioClip switchSound;
        private AudioSource switchAudio;
        private bool switchedOn;
        public bool IsOn => switchedOn;

        private void Awake()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            if (garageDoor == null) garageDoor = FindFirstObjectByType<GarageDoorInteractable>();
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
                if (gameFlow == null || !gameFlow.insideGarage) return "Interruptor del portón (desde adentro)";
                if (!gameFlow.stationPowerConnected) return "Primero enchufá el generador a la caja eléctrica";
                return switchedOn ? "[E] Apagar tecla / cerrar portón" : "[E] Encender tecla / abrir portón";
            }
        }

        public void Interact()
        {
            if (gameFlow == null || gameFlow.gameOver || gameFlow.finished) return;
            if (!gameFlow.stationPowerConnected)
            {
                gameFlow.ShowMessage("Primero enchufá el generador a la caja eléctrica.");
                return;
            }
            if (!gameFlow.insideGarage)
            {
                gameFlow.ShowMessage("El interruptor está del lado interior del taller.");
                return;
            }

            if (garageDoor == null)
            {
                gameFlow.ShowMessage("El interruptor no está conectado al portón.");
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
