using UnityEngine;

namespace EnElCamino
{
    public class FuelCan : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Renderer fuelCanRenderer;
        [SerializeField] private Transform player;
        [SerializeField] private float groundClearance = 0.025f;
        private bool taken;
        private Transform carryAnchor;
        private Vector3 carriedLocalPosition;
        private Vector3 carriedLocalScale;

        private void Awake()
        {
            FuelCan parentCan = transform.parent != null
                ? transform.parent.GetComponentInParent<FuelCan>()
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
            foreach (FuelCan nested in GetComponentsInChildren<FuelCan>(true))
                if (nested != null && nested != this) nested.enabled = false;
        }

        private void Start()
        {
            if (!taken) PlaceOnGround();
        }

        private void LateUpdate()
        {
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
                if (taken) return "Bidón tomado";
                return "[E] Tomar bidón vacío";
            }
        }

        public void Interact()
        {
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
