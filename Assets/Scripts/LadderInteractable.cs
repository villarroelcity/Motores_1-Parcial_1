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
        [SerializeField] private Transform lowerDestination;
        [SerializeField] private float hangPause = 0.65f;
        [SerializeField] private float dropDuration = 1.45f;

        private enum ClimbState { Ground, AtRoof, Descending, Inside }
        private ClimbState climbState = ClimbState.Ground;
        private Coroutine movementRoutine;

        public bool CanClimb => portableExterior && climbState == ClimbState.Ground;
        public bool IsAtRoof => portableExterior && climbState == ClimbState.AtRoof;

        public string Prompt
        {
            get
            {
                if (!portableExterior) return promptText;
                if (climbState == ClimbState.Ground) return promptText;
                if (climbState == ClimbState.AtRoof) return "Acercate al hueco para bajar";
                if (climbState == ClimbState.Descending) return "Bajando al taller...";
                return "Ya estás dentro del taller";
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
            gameFlow.ShowMessage("Subiste al techo. Acercate al hueco y presioná [E] para colgarte y bajar.");
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

            PlayerVisualAnimator animator = player.GetComponent<PlayerVisualAnimator>();
            if (animator != null) animator.SetHanging(true);

            Vector3 startPosition = player.position;
            Quaternion startRotation = player.rotation;
            Vector3 atHolePosition = new Vector3(holePoint.position.x, startPosition.y, holePoint.position.z);
            Vector3 endPosition = lowerDestination != null
                ? lowerDestination.position
                : new Vector3(atHolePosition.x, startPosition.y - 4.5f, atHolePosition.z);
            Quaternion endRotation = lowerDestination != null ? lowerDestination.rotation : startRotation;

            // Step to the opening, hang briefly by both arms, release, then descend slowly.
            float approachElapsed = 0f;
            const float approachDuration = 0.45f;
            while (approachElapsed < approachDuration)
            {
                approachElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(approachElapsed / approachDuration);
                player.position = Vector3.Lerp(startPosition, atHolePosition, t * t * (3f - 2f * t));
                yield return null;
            }
            player.position = atHolePosition;
            yield return new WaitForSeconds(hangPause);

            Vector3 hangingPosition = atHolePosition + Vector3.down * 0.24f;
            const float hangTransition = 0.22f;
            float hangElapsed = 0f;
            while (hangElapsed < hangTransition)
            {
                hangElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(hangElapsed / hangTransition);
                player.position = Vector3.Lerp(atHolePosition, hangingPosition, t);
                yield return null;
            }
            if (animator != null)
            {
                animator.SetHanging(false);
                animator.SetFalling(true);
            }

            float elapsed = 0f;
            while (elapsed < dropDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, dropDuration));
                float eased = t * t * (3f - 2f * t);
                player.SetPositionAndRotation(
                    Vector3.Lerp(hangingPosition, endPosition, eased),
                    Quaternion.Slerp(startRotation, endRotation, eased));
                yield return null;
            }

            player.SetPositionAndRotation(endPosition, endRotation);

            if (controller != null) controller.enabled = true;
            if (playerController != null) playerController.SetControlledRoofDescent(false);
            if (animator != null) animator.SetHanging(false);
            if (animator != null) animator.SetFalling(false);
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
