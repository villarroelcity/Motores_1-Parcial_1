using UnityEngine;
using System.Collections;

namespace EnElCamino
{
    [RequireComponent(typeof(AudioSource))]
    public class FuelPumpInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private FuelCan fuelCan;
        [SerializeField] private AudioClip pumpSound;
        [SerializeField] private Vector2 pitchVariation = new Vector2(0.97f, 1.03f);
        [SerializeField] private Vector2 volumeVariation = new Vector2(0.95f, 1f);
        private AudioSource audioSource;
        private bool used;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            if (fuelCan == null) fuelCan = FindFirstObjectByType<FuelCan>();
        }

        public string Prompt
        {
            get
            {
            if (gameFlow == null || !gameFlow.generatorOn || !gameFlow.stationPowerConnected) return "Surtidor sin energía";
                if (!gameFlow.hasFuelCan) return "Necesitás un bidón";
                if (used) return "Bidón lleno";
                return "[E] Llenar bidón";
            }
        }

        public void Interact()
        {
            if (used) return;
            if (gameFlow == null || !gameFlow.generatorOn || !gameFlow.stationPowerConnected)
            {
                gameFlow.ShowMessage("El surtidor necesita energía.");
                return;
            }

            if (!gameFlow.hasFuelCan)
            {
                gameFlow.ShowMessage("Necesitás traer el bidón vacío.");
                return;
            }

            StartCoroutine(FillSequence());
        }

        private IEnumerator FillSequence()
        {
            used = true;
            gameFlow.fueling = true;

            if (pumpSound != null)
            {
                audioSource.pitch = Random.Range(pitchVariation.x, pitchVariation.y);
                audioSource.volume = Random.Range(volumeVariation.x, volumeVariation.y);
                audioSource.PlayOneShot(pumpSound);
                yield return new WaitForSeconds(pumpSound.length);
            }

            if (fuelCan != null) fuelCan.Fill();
            gameFlow.FillFuelCan();
            gameFlow.fueling = false;
        }
    }
}
