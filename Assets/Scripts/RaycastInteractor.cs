using UnityEngine;
using UnityEngine.InputSystem;

namespace EnElCamino
{
    public class RaycastInteractor : MonoBehaviour
    {
        [SerializeField] private GameUI gameUI;
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private GeneratorInteractable generatorButton;
        [SerializeField] private LadderInteractable exteriorLadder;
        [SerializeField] private LadderInteractable interiorLadder;
        [SerializeField] private GarageDoorInteractable garageDoor;
        [SerializeField] private FuelCan fuelCan;
        [SerializeField] private FuelPumpInteractable fuelPump;
        [SerializeField] private CarInteractable car;
        [SerializeField] private float interactionDistance = 2.4f;
        [SerializeField] private float ladderInteractionDistance = 4f;
        [SerializeField] private float distance = 6f;
        private Camera mainCamera;
        private IInteractable current;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (gameFlow.gameOver || gameFlow.finished)
            {
                gameUI.HidePrompt();
                return;
            }

            current = FindInteractable();
            if (current == null)
            {
                gameUI.HidePrompt();
                return;
            }

            gameUI.ShowPrompt(current.Prompt);
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                current.Interact();
        }

        private IInteractable FindInteractable()
        {
            if (IsNear(generatorButton)) return generatorButton;
            if (IsNear(exteriorLadder, ladderInteractionDistance)) return exteriorLadder;
            if (IsNear(interiorLadder, ladderInteractionDistance)) return interiorLadder;
            if (IsNear(garageDoor)) return garageDoor;
            if (!fuelCan.IsTaken() && IsNear(fuelCan)) return fuelCan;
            if (IsNear(fuelPump)) return fuelPump;
            if (IsNear(car)) return car;

            Ray ray = mainCamera != null
                ? mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : new Ray(transform.position + Vector3.up, transform.forward);

            if (!Physics.Raycast(ray, out RaycastHit hit, distance)) return null;
            MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
                if (behaviour is IInteractable interactable) return interactable;
            return null;
        }

        private bool IsNear(Component target)
        {
            return Vector3.Distance(transform.position, target.transform.position) < interactionDistance;
        }

        private bool IsNear(Component target, float maxDistance)
        {
            return Vector3.Distance(transform.position, target.transform.position) < maxDistance;
        }
    }
}
