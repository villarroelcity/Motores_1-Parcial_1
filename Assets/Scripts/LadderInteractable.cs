using System.Collections;
using UnityEngine;

namespace EnElCamino
{
    public class LadderInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Transform player;
        [SerializeField] private Transform destination;
        [SerializeField] private bool entersGarage;
        [SerializeField] private string promptText;
        [SerializeField] private bool portableExterior;
        [SerializeField] private Transform roofLadderPoint;
        [SerializeField] private Transform lowerDestination;
        [SerializeField] private float climbDuration = 1.6f;
        [SerializeField] private float descendDuration = 1.2f;
        [SerializeField] private Vector3 carriedLocalPosition = new Vector3(0.55f, 0.85f, 0.45f);
        [SerializeField] private Vector3 carriedLocalEulerAngles = new Vector3(0f, 0f, 90f);

        private enum PortableState { Ground, AtRoof, Carried, Placed }
        private PortableState portableState = PortableState.Ground;
        private Coroutine movementRoutine;

        public bool IsCarried => portableExterior && portableState == PortableState.Carried;
        public bool IsPlaced => portableExterior && portableState == PortableState.Placed;
        public bool CanBePickedUp => portableExterior && portableState == PortableState.AtRoof;

        public string Prompt
        {
            get
            {
                if (!portableExterior) return promptText;
                if (portableState == PortableState.Ground) return promptText;
                if (portableState == PortableState.AtRoof) return "[E] Tomar la escalera";
                if (portableState == PortableState.Placed) return "[E] Bajar por la escalera";
                return "Escalera en mano";
            }
        }

        public void Interact()
        {
            if (gameFlow == null || player == null || gameFlow.gameOver || gameFlow.finished || gameFlow.movementLocked)
                return;

            if (!portableExterior)
            {
                StartMovement(destination, entersGarage);
                return;
            }

            if (portableState == PortableState.Ground)
            {
                if (!gameFlow.generatorOn)
                {
                    gameFlow.ShowMessage("Primero encendé el generador.");
                    return;
                }

                ClimbExteriorInstant();
            }
            else if (portableState == PortableState.AtRoof)
            {
                PickUp();
            }
            else if (portableState == PortableState.Placed)
            {
                StartMovement(lowerDestination, true);
            }
        }

        private void ClimbExteriorInstant()
        {
            portableState = PortableState.Carried;
            gameFlow.movementLocked = true;
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            Vector3 end = destination != null ? destination.position : player.position;
            Quaternion endRotation = destination != null ? destination.rotation : player.rotation;
            player.SetPositionAndRotation(end, endRotation);

            if (controller != null) controller.enabled = true;
            gameFlow.movementLocked = false;
            portableState = PortableState.AtRoof;
            gameFlow.exteriorLadderCarried = false;

            if (roofLadderPoint != null)
            {
                transform.SetParent(roofLadderPoint.parent, true);
                transform.position = roofLadderPoint.position;
                transform.rotation = roofLadderPoint.rotation;
                transform.localScale = roofLadderPoint.localScale;
            }

            gameFlow.ShowMessage("Subiste al techo. Tomá la escalera y colócala en el hueco para bajar.");
        }

        private void PickUp()
        {
            portableState = PortableState.Carried;
            gameFlow.exteriorLadderCarried = true;
            SetColliders(false);
            transform.SetParent(player, false);
            transform.localPosition = carriedLocalPosition;
            transform.localEulerAngles = carriedLocalEulerAngles;
            gameFlow.ShowMessage("Tenés la escalera. Acercate al hueco y presioná [E] para colocarla.");
        }

        public void PlaceAtHole(Transform placementPoint)
        {
            if (!IsCarried || placementPoint == null) return;

            portableState = PortableState.Placed;
            gameFlow.exteriorLadderCarried = false;
            gameFlow.exteriorLadderPlaced = true;
            transform.SetParent(placementPoint.parent, true);
            transform.position = placementPoint.position;
            transform.rotation = placementPoint.rotation;
            transform.localScale = placementPoint.localScale;
            Bounds visualBounds = new Bounds(transform.position, Vector3.zero);
            bool hasBounds = false;
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (!hasBounds) { visualBounds = renderer.bounds; hasBounds = true; }
                else visualBounds.Encapsulate(renderer.bounds);
            }
            if (hasBounds) transform.position += placementPoint.position - visualBounds.center;
            SetColliders(true);
            gameFlow.ShowMessage("Escalera colocada. Bajá por el hueco con [E].");
        }

        private void StartMovement(Transform target, bool notifyGarage)
        {
            if (target == null) return;
            gameFlow.movementLocked = true;
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.SetPositionAndRotation(target.position, target.rotation);
            if (controller != null) controller.enabled = true;
            gameFlow.movementLocked = false;
            if (notifyGarage || entersGarage) gameFlow.EnterGarage();
        }

        private IEnumerator AnimateToDestination(Transform target, bool notifyGarage)
        {
            gameFlow.movementLocked = true;
            SetClimbingAnimation(true);
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            float duration = portableExterior && portableState == PortableState.Placed ? descendDuration : climbDuration;
            yield return AnimatePlayer(player.position, player.rotation, target.position, target.rotation, duration);

            if (controller != null) controller.enabled = true;
            SetClimbingAnimation(false);
            gameFlow.movementLocked = false;
            movementRoutine = null;

            if (notifyGarage || entersGarage) gameFlow.EnterGarage();
        }

        private IEnumerator AnimatePlayer(Vector3 startPosition, Quaternion startRotation, Vector3 endPosition, Quaternion endRotation, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                float smooth = normalized * normalized * (3f - 2f * normalized);
                player.position = Vector3.Lerp(startPosition, endPosition, smooth);
                player.rotation = Quaternion.Slerp(startRotation, endRotation, smooth);
                yield return null;
            }

            player.position = endPosition;
            player.rotation = endRotation;
        }

        private void SetClimbingAnimation(bool value)
        {
            PlayerVisualAnimator animator = player != null ? player.GetComponent<PlayerVisualAnimator>() : null;
            if (animator != null) animator.SetClimbing(value);
        }

        private void SetColliders(bool value)
        {
            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
                collider.enabled = value;
        }
    }
}
