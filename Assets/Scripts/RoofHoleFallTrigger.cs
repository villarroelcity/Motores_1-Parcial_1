using UnityEngine;

namespace EnElCamino
{
    public class RoofHoleFallTrigger : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;

        private void OnTriggerEnter(Collider other)
        {
            if (gameFlow == null || gameFlow.gameOver || gameFlow.finished) return;
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsInControlledRoofDescent) return;
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null && !controller.isGrounded)
                gameFlow.GameOver();
        }
    }
}
