using UnityEngine;

namespace EnElCamino
{
    public class Escalera : MonoBehaviour, IInteractable
    {
        [SerializeField] private Juego gameFlow;
        [SerializeField] private Transform player;
        [SerializeField] private Transform destination;
        [SerializeField] private bool entersGarage;
        [SerializeField] private string promptText;
        [SerializeField] private bool portableExterior;
        [SerializeField] private Transform lowerDestination;
        [SerializeField] private float hangPause = 0.3f;
        [SerializeField] private float edgeStandOff = 0.45f;
        [SerializeField] private float hangLowering = 2.1f;
        [SerializeField] private float dropGravity = 18f;
        [SerializeField] private bool holeInteraction;
        [SerializeField] private Escalera exteriorLadder;

        private enum ClimbState { Ground, AtRoof, Descending, Inside }
        private ClimbState climbState = ClimbState.Ground;
        private Coroutine movementRoutine;

        public bool CanClimb => portableExterior && climbState == ClimbState.Ground;
        public bool IsAtRoof => portableExterior && climbState == ClimbState.AtRoof;
        public bool CanInteract => holeInteraction && exteriorLadder != null && exteriorLadder.IsAtRoof
            && gameFlow != null && !gameFlow.insideGarage && !gameFlow.gameOver && !gameFlow.finished;

        public string Prompt
        {
            get
            {
                if (holeInteraction) return "[E] Colgarse del borde y bajar";
                if (!portableExterior) return promptText;
                if (climbState == ClimbState.Ground) return promptText;
                if (climbState == ClimbState.AtRoof) return "Acercate al hueco para bajar";
                if (climbState == ClimbState.Descending) return "Bajando al taller...";
                return "Ya estas dentro del taller";
            }
        }

        public void Interact()
        {
            if (gameFlow == null || player == null || gameFlow.gameOver || gameFlow.finished || gameFlow.movementLocked)
                return;

            if (holeInteraction)
            {
                if (exteriorLadder != null && exteriorLadder.IsAtRoof && !gameFlow.insideGarage)
                    exteriorLadder.DescendThroughHole(transform);
                return;
            }

            if (!portableExterior)
            {
                StartMovement(destination, entersGarage);
                return;
            }

            if (climbState == ClimbState.Ground)
            {
                ClimbExteriorInstant();
            }
        }

        private void ClimbExteriorInstant()
        {
            gameFlow.movementLocked = true;
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            Vector3 end = destination != null ? destination.position : player.position;
            Quaternion endRotation = destination != null ? destination.rotation : player.rotation;
            player.SetPositionAndRotation(end, endRotation);

            ResetPlayerFallState();
            if (controller != null) controller.enabled = true;
            ResetPlayerFallState();
            gameFlow.movementLocked = false;
            climbState = ClimbState.AtRoof;
            gameFlow.ShowMessage("Subiste al techo. Acercate al hueco y presiona [E] para colgarte y bajar.");
        }

        public void DescendThroughHole(Transform holePoint)
        {
            if (!IsAtRoof || holePoint == null || movementRoutine != null) return;
            movementRoutine = StartCoroutine(DescendThroughHoleRoutine(holePoint));
        }

        private void StartMovement(Transform target, bool notifyGarage)
        {
            if (target == null) return;
            gameFlow.movementLocked = true;
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.SetPositionAndRotation(target.position, target.rotation);
            ResetPlayerFallState();
            if (controller != null) controller.enabled = true;
            ResetPlayerFallState();
            gameFlow.movementLocked = false;
            if (notifyGarage || entersGarage) gameFlow.EnterGarage();
        }

        private System.Collections.IEnumerator DescendThroughHoleRoutine(Transform holePoint)
        {
            climbState = ClimbState.Descending;
            gameFlow.movementLocked = true;
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null) playerController.SetControlledRoofDescent(true);

            if (playerController != null) playerController.SetHanging(true);

            Vector3 startPosition = player.position;
            Quaternion startRotation = player.rotation;
            Vector3 outward = startPosition - holePoint.position;
            outward.y = 0f;
            if (outward.sqrMagnitude < 0.001f) outward = player.forward;
            outward.y = 0f;
            outward.Normalize();

            // Find the nearest rim along the player's approach direction. Never move
            // the character to the center of the opening: the hands must reach its edge.
            Collider holeCollider = holePoint.GetComponent<Collider>();
            Vector3 extents = holeCollider != null ? holeCollider.bounds.extents : Vector3.one * 0.8f;
            float xToRim = Mathf.Abs(outward.x) > 0.001f ? extents.x / Mathf.Abs(outward.x) : float.PositiveInfinity;
            float zToRim = Mathf.Abs(outward.z) > 0.001f ? extents.z / Mathf.Abs(outward.z) : float.PositiveInfinity;
            float distanceToRim = Mathf.Min(xToRim, zToRim);
            if (float.IsInfinity(distanceToRim)) distanceToRim = 0f;

            Vector3 rimPosition = holePoint.position + outward * distanceToRim;
            Vector3 edgePosition = rimPosition + outward * edgeStandOff;
            edgePosition.y = startPosition.y;
            Vector3 hangPosition = rimPosition - outward * 0.12f;
            hangPosition.y = startPosition.y - hangLowering;
            Quaternion faceOpening = Quaternion.LookRotation(-outward, Vector3.up);

            // Walk only to the near edge, rotate toward the opening, then lower the
            // body until the raised hands are level with the roof rim.
            float approachElapsed = 0f;
            const float approachDuration = 0.38f;
            while (approachElapsed < approachDuration)
            {
                approachElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(approachElapsed / approachDuration);
                float eased = t * t * (3f - 2f * t);
                player.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, edgePosition, eased),
                    Quaternion.Slerp(startRotation, faceOpening, eased));
                yield return null;
            }
            player.SetPositionAndRotation(edgePosition, faceOpening);

            const float hangTransitionDuration = 0.42f;
            float hangElapsed = 0f;
            while (hangElapsed < hangTransitionDuration)
            {
                hangElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(hangElapsed / hangTransitionDuration);
                float eased = t * t * (3f - 2f * t);
                player.position = Vector3.Lerp(edgePosition, hangPosition, eased);
                yield return null;
            }
            player.position = hangPosition;
            yield return new WaitForSeconds(Mathf.Clamp(hangPause, 0f, 0.45f));

            if (playerController != null)
            {
                playerController.SetHanging(false);
                playerController.SetFalling(true);
            }

            float landingY = lowerDestination != null
                ? lowerDestination.position.y
                : holePoint.position.y - 4.5f;
            landingY = Mathf.Min(landingY, hangPosition.y - 0.1f);
            Vector3 landingPosition = new Vector3(hangPosition.x, landingY, hangPosition.z);
            float verticalSpeed = 0f;

            // A short, accelerating free fall replaces the old slow interpolation,
            // which made the player float. Keep X/Z at the edge; don't funnel to center.
            while (player.position.y > landingY + 0.001f)
            {
                verticalSpeed += Mathf.Max(1f, dropGravity) * Time.deltaTime;
                Vector3 position = player.position;
                position.y = Mathf.Max(landingY, position.y - verticalSpeed * Time.deltaTime);
                player.position = position;
                yield return null;
            }

            player.SetPositionAndRotation(landingPosition, faceOpening);

            if (controller != null) controller.enabled = true;
            if (playerController != null) playerController.SetControlledRoofDescent(false);
            if (playerController != null) playerController.SetHanging(false);
            if (playerController != null) playerController.SetFalling(false);
            ResetPlayerFallState();
            gameFlow.movementLocked = false;
            movementRoutine = null;
            climbState = ClimbState.Inside;
            gameFlow.EnterGarage();
        }

        private void ResetPlayerFallState()
        {
            PlayerController playerController = player != null ? player.GetComponent<PlayerController>() : null;
            if (playerController != null) playerController.ResetFallTracking();
        }
    }
}
