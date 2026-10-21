using System.Collections;
using UnityEngine;

namespace EnElCamino
{
    public class Surtidor : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Renderer fuelCanRenderer;
        [SerializeField] private Transform player;
        [SerializeField] private float groundClearance = 0.025f;
        [SerializeField] private bool pumpMode;
        [SerializeField] private Surtidor fuelCan;
        [SerializeField] private AudioClip pumpSound;
        [SerializeField] private Vector2 pitchVariation = new Vector2(0.97f, 1.03f);
        [SerializeField] private Vector2 volumeVariation = new Vector2(0.95f, 1f);
        private bool taken;
        private bool pumpUsed;
        private Transform carryAnchor;
        private Vector3 carriedLocalPosition;
        private Vector3 carriedLocalScale;
        private AudioSource audioSource;

        public bool IsPump => pumpMode;

        private void Awake()
        {
            if (pumpMode)
            {
                if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
                if (fuelCan == null) fuelCan = FindFirstObjectByType<Surtidor>();
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                return;
            }

            Surtidor parentCan = transform.parent != null
                ? transform.parent.GetComponentInParent<Surtidor>()
                : null;
            if (parentCan != null)
            {
                // Imported visual prefabs sometimes carry a duplicated copy of
                // this script. It must never run its own ground/carry logic.
                enabled = false;
                return;
            }

            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            if (player == null)
            {
                GameObject playerObject = GameObject.Find("Jugador");
                if (playerObject != null) player = playerObject.transform;
            }

            // Some imported visual prefabs contain this component as well. The
            // root BidonCombustible is the only interactable and only it may
            // reposition the model.
            foreach (Surtidor nested in GetComponentsInChildren<Surtidor>(true))
                if (nested != null && nested != this) nested.enabled = false;
        }

        private void Start()
        {
            if (pumpMode) return;
            if (!taken) PlaceOnGround();
        }

        private void LateUpdate()
        {
            if (pumpMode) return;
            if (!taken) return;
            if (carryAnchor == null) AttachToHand();
            if (carryAnchor == null) return;

            // Keep the visual attached even while the procedural arm animation
            // is changing the shoulder and hand transforms.
            if (transform.parent != carryAnchor) transform.SetParent(carryAnchor, false);
            transform.localPosition = carriedLocalPosition;
            transform.localRotation = Quaternion.identity;
            transform.localScale = carriedLocalScale;
            AlignVisualToHand();
        }

        public string Prompt
        {
            get
            {
                if (pumpMode)
                {
                    if (gameFlow == null || !gameFlow.generatorOn || !gameFlow.stationPowerConnected) return "Surtidor sin energía";
                    if (!gameFlow.hasFuelCan) return "Necesitás un bidón";
                    if (pumpUsed) return "Bidón lleno";
                    return "[E] Llenar bidón";
                }
                if (taken) return "Bidón tomado";
                return "[E] Tomar bidón vacío";
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
                if (gameFlow != null) gameFlow.ShowMessage("Primero encendé y conectá el generador.");
                return;
            }

            if (!gameFlow.garageOpen)
            {
                gameFlow.ShowMessage("Primero abrí el portón desde adentro del garage.");
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
                if (gameFlow != null) gameFlow.ShowMessage("El surtidor necesita energía.");
                yield break;
            }
            if (!gameFlow.hasFuelCan)
            {
                gameFlow.ShowMessage("Necesitás traer el bidón vacío.");
                yield break;
            }

            pumpUsed = true;
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

        private void AttachToHand()
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.Find("Jugador");
                if (playerObject != null) player = playerObject.transform;
            }
            if (player == null) return;

            Transform hand = player.Find("PlayerModel/HombroDerecho/ManoDerecha");
            if (hand == null) hand = player.Find("PlayerModel/HombroDerecho");
            if (hand == null) return;

            Vector3 authoredScale = transform.localScale;
            carryAnchor = hand;
            transform.SetParent(carryAnchor, false);
            carriedLocalPosition = Vector3.zero;
            transform.localPosition = carriedLocalPosition;
            transform.localRotation = Quaternion.identity;
            Vector3 parentScale = carryAnchor.lossyScale;
            carriedLocalScale = new Vector3(
                authoredScale.x / Mathf.Max(0.001f, parentScale.x),
                authoredScale.y / Mathf.Max(0.001f, parentScale.y),
                authoredScale.z / Mathf.Max(0.001f, parentScale.z));
            transform.localScale = carriedLocalScale;
            AlignVisualToHand();
        }

        private void AlignVisualToHand()
        {
            if (carryAnchor == null) return;

            Bounds visualBounds = default;
            bool hasBounds = false;
            foreach (Renderer candidate in GetComponentsInChildren<Renderer>(true))
            {
                if (candidate == null || !candidate.enabled || !candidate.gameObject.activeInHierarchy) continue;
                if (!hasBounds)
                {
                    visualBounds = candidate.bounds;
                    hasBounds = true;
                }
                else visualBounds.Encapsulate(candidate.bounds);
            }

            if (!hasBounds) return;

            Vector3 desiredCenter = carryAnchor.position
                - carryAnchor.up * 0.13f
                + carryAnchor.forward * 0.06f;
            transform.position += desiredCenter - visualBounds.center;
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

        private void PlaceOnGround()
        {
            Bounds visualBounds = default;
            bool hasVisualBounds = false;
            foreach (Renderer candidate in GetComponentsInChildren<Renderer>(true))
            {
                if (candidate == null || !candidate.enabled || !candidate.gameObject.activeInHierarchy) continue;
                if (!hasVisualBounds)
                {
                    visualBounds = candidate.bounds;
                    hasVisualBounds = true;
                }
                else visualBounds.Encapsulate(candidate.bounds);
            }

            if (!hasVisualBounds) return;

            Vector3 rayOrigin = new Vector3(visualBounds.center.x, visualBounds.max.y + 2f, visualBounds.center.z);
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float groundY = float.NegativeInfinity;
            foreach (RaycastHit hit in hits)
            {
                Transform hitTransform = hit.collider != null ? hit.collider.transform : null;
                if (hitTransform == null || hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
                if (hit.point.y > groundY) groundY = hit.point.y;
            }

            if (float.IsNegativeInfinity(groundY)) return;
            Vector3 position = transform.position;
            position.y += groundY + groundClearance - visualBounds.min.y;
            transform.position = position;
        }
    }
}
