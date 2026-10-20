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

        private void Start()
        {
            if (!taken) PlaceOnGround();
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

            // Parent the can to the animated arm pivot so it stays in the hand while walking.
            Transform carryAnchor = player != null ? player.Find("PlayerModel/HombroDerecho") : null;
            Vector3 canScale = transform.localScale;
            transform.SetParent(carryAnchor != null ? carryAnchor : player, false);
            transform.localPosition = carryAnchor != null
                ? new Vector3(0f, -0.15f, 0.06f)
                : new Vector3(0.43f, 0.99f, 0.06f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = canScale;
            gameFlow.TakeFuelCan();
        }

        public void Fill()
        {
            if (fuelCanRenderer != null) fuelCanRenderer.material.color = Color.green;
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
