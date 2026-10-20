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
            // Entering the opening is not itself a death: the player can start the
            // deliberate hang-and-drop here. Let PlayerController decide whether an
            // uncontrolled fall is fatal based on its height and landing distance.
            if (controller != null && !controller.isGrounded)
                player.RegisterRoofFall();
        }
    }
}
