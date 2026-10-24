using System.Collections;
using UnityEngine;

namespace EnElCamino
{
    public class Surtidor : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Renderer fuelCanRenderer;
        [SerializeField] private Transform player;
        [SerializeField] private bool pumpMode;
        [SerializeField] private Surtidor fuelCan;
        [SerializeField] private AudioClip pumpSound;
        [SerializeField] private Vector2 pitchVariation = new Vector2(0.97f, 1.03f);
        [SerializeField] private Vector2 volumeVariation = new Vector2(0.95f, 1f);
        private bool taken;
        private bool pumpUsed;
        [SerializeField] private Transform handAnchor;
        [SerializeField] private Vector3 carriedLocalPosition;
        [SerializeField] private Vector3 carriedLocalScale;
        [SerializeField] private AudioSource audioSource;

        public bool IsPump => pumpMode;

        private void Awake()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
        }

        public string Prompt
        {
            get
            {
                if (pumpMode)
                {
                    if (gameFlow == null || !gameFlow.generatorOn || !gameFlow.stationPowerConnected) return "Surtidor sin energia";
                    if (!gameFlow.hasFuelCan) return "Necesitas un bidon";
                    if (pumpUsed) return "Bidon lleno";
                    return "[E] Llenar bidon";
                }
                if (taken) return "Bidon tomado";
                return "[E] Tomar bidon vacio";
            }
        }

        public void Interact()
        {
            if (pumpMode)
            {
                if (!pumpUsed) StartCoroutine(FillSequence());
                return;
            }
            if (taken) return;

            if (gameFlow == null || !gameFlow.generatorOn || !gameFlow.stationPowerConnected)
            {
                if (gameFlow != null) gameFlow.ShowMessage("Primero encende y conecta el generador.");
                return;
            }

            if (!gameFlow.garageOpen)
            {
                gameFlow.ShowMessage("Primero abri el porton desde adentro del taller.");
                return;
            }

            taken = true;
            Collider canCollider = GetComponent<Collider>();
            if (canCollider != null) canCollider.enabled = false;
            AttachToHand();
            gameFlow.TakeFuelCan();
        }

        private IEnumerator FillSequence()
        {
            if (gameFlow == null || !gameFlow.generatorOn || !gameFlow.stationPowerConnected)
            {
                if (gameFlow != null) gameFlow.ShowMessage("El surtidor necesita energia.");
                yield break;
            }
            if (!gameFlow.hasFuelCan)
            {
                gameFlow.ShowMessage("Necesitas traer el bidon vacio.");
                yield break;
            }

            pumpUsed = true;
            gameFlow.fueling = true;
            bool previousMovementLock = gameFlow.movementLocked;
            gameFlow.movementLocked = true;
            PlayerController playerController = player != null
                ? player.GetComponent<PlayerController>()
                : FindFirstObjectByType<PlayerController>();
            if (playerController != null) playerController.SetInteractionPoseLocked(true);

            try
            {
                if (pumpSound != null)
                {
                    audioSource.pitch = Random.Range(pitchVariation.x, pitchVariation.y);
                    audioSource.volume = Random.Range(volumeVariation.x, volumeVariation.y);
                    audioSource.PlayOneShot(pumpSound);
                    yield return new WaitForSeconds(pumpSound.length);
                }
                if (fuelCan != null) fuelCan.Fill();
                gameFlow.FillFuelCan();
            }
            finally
            {
                if (playerController != null) playerController.SetInteractionPoseLocked(false);
                gameFlow.movementLocked = previousMovementLock;
                gameFlow.fueling = false;
            }
        }

        private void AttachToHand()
        {
            transform.SetParent(handAnchor, false);
            transform.localPosition = carriedLocalPosition;
            transform.localRotation = Quaternion.identity;
            transform.localScale = carriedLocalScale;
        }

        public void Fill()
        {
            if (fuelCanRenderer != null) fuelCanRenderer.material.color = Color.green;
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer == fuelCanRenderer) continue;
                foreach (Material material in renderer.materials)
                    if (material != null && material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.green);
            }
        }

        public bool IsTaken()
        {
            return taken;
        }

        public void Use()
        {
            gameObject.SetActive(false);
        }

    }
}
