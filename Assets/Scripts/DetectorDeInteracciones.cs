using UnityEngine;
using UnityEngine.InputSystem;

namespace EnElCamino
{
    public class DetectorDeInteracciones : MonoBehaviour
    {
        [SerializeField] private UI gameUI;
        [SerializeField] private Juego gameFlow;
        [SerializeField] private Generador generatorButton;
        [SerializeField] private Escalera exteriorLadder;
        [SerializeField] private Escalera ladderPlacement;
        [SerializeField] private PortonTaller garageDoor;
        [SerializeField] private Surtidor fuelCan;
        [SerializeField] private Surtidor fuelPump;
        [SerializeField] private AutoJugador car;
        [SerializeField] private TableroElectrico powerBox;
        [SerializeField] private Interruptores gateSwitch;
        [SerializeField] private float interactionDistance = 2.4f;
        [SerializeField] private float ladderInteractionDistance = 4f;
        [SerializeField] private float distance = 6f;
        private Camera mainCamera;
        private IInteractable current;

        private void Awake()
        {
            mainCamera = Camera.main;
            if (gameFlow == null) gameFlow = FindFirstObjectByType<Juego>();
            if (gameUI == null) gameUI = FindFirstObjectByType<UI>();
            if (powerBox == null) powerBox = FindFirstObjectByType<TableroElectrico>();
            if (gateSwitch == null) gateSwitch = FindFirstObjectByType<Interruptores>();
        }

        private void Update()
        {
            if (gameFlow == null || gameFlow.gameOver || gameFlow.finished)
            {
                gameUI.HidePrompt();
                return;
            }

            if (gameFlow.movementLocked)
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
            // Aim takes precedence. Proximity fallback chooses the nearest useful
            // object rather than letting an already-running generator consume E.
            Ray ray = mainCamera != null
                ? mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : new Ray(transform.position + Vector3.up, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                foreach (MonoBehaviour behaviour in hit.collider.GetComponentsInParent<MonoBehaviour>())
                    if (behaviour is IInteractable interactable && IsUseful(behaviour) && IsNear(behaviour)) return interactable;
            }

            IInteractable nearest = null;
            float nearestDistance = float.PositiveInfinity;
            Consider(generatorButton, interactionDistance, ref nearestDistance, ref nearest);
            Consider(powerBox, interactionDistance, ref nearestDistance, ref nearest);
            Consider(gateSwitch, interactionDistance, ref nearestDistance, ref nearest);
            // Require the player to be close to the opening before starting the hang/drop animation.
            Consider(ladderPlacement, interactionDistance, ref nearestDistance, ref nearest);
            Consider(exteriorLadder, ladderInteractionDistance, ref nearestDistance, ref nearest);
            if (gateSwitch == null) Consider(garageDoor, interactionDistance, ref nearestDistance, ref nearest);
            Consider(fuelCan, interactionDistance, ref nearestDistance, ref nearest);
            Consider(fuelPump, interactionDistance, ref nearestDistance, ref nearest);
            Consider(car, interactionDistance, ref nearestDistance, ref nearest);
            return nearest;
        }

        private bool IsUseful(Component candidate)
        {
            if (candidate == null || !candidate.gameObject.activeInHierarchy) return false;
            if (candidate == generatorButton) return !gameFlow.generatorOn;
            if (candidate == powerBox) return gameFlow.generatorOn && !gameFlow.stationPowerConnected;
            if (candidate == gateSwitch) return gameFlow.stationPowerConnected && gameFlow.insideGarage;
            if (candidate == garageDoor) return gateSwitch == null;
            if (candidate == ladderPlacement) return !gameFlow.insideGarage && ladderPlacement.CanInteract;
            if (candidate == exteriorLadder) return !gameFlow.insideGarage && exteriorLadder.CanClimb;
            if (candidate == fuelCan) return !fuelCan.IsTaken() && gameFlow.garageOpen;
            if (candidate == fuelPump) return gameFlow.hasFuelCan && !gameFlow.fuelCanFull;
            if (candidate == car) return car.CanInteract;
            return true;
        }

        private void Consider(Component candidate, float range, ref float nearestDistance, ref IInteractable nearest)
        {
            if (!IsUseful(candidate) || !(candidate is IInteractable interactable)) return;
            float candidateDistance = Vector3.Distance(transform.position, candidate.transform.position);
            if (candidateDistance >= range || candidateDistance >= nearestDistance) return;
            nearestDistance = candidateDistance;
            nearest = interactable;
        }

        private bool IsNear(Component target)
        {
            return target != null && Vector3.Distance(transform.position, target.transform.position) < interactionDistance;
        }

        private bool IsNear(Component target, float maxDistance)
        {
            return target != null && Vector3.Distance(transform.position, target.transform.position) < maxDistance;
        }
    }

    // Interfaz comun de las acciones que se activan con E.
    // Vive junto al interactor porque no es un componente independiente de la escena.
    public interface IInteractable
    {
        string Prompt { get; }
        void Interact();
    }
}
