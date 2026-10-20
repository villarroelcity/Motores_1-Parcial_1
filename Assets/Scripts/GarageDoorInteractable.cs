using System.Collections;
using UnityEngine;

namespace EnElCamino
{
    public class GarageDoorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private GameObject garageDoor;
        [SerializeField] private float travelDuration = 2.2f;
        private bool open;
        private bool openedOnce;
        private bool moving;
        private Vector3 closedPosition;
        private Coroutine motion;
        public bool IsOpen => open;
        public bool IsMoving => moving;
        public GameObject DoorObject => garageDoor != null ? garageDoor : gameObject;

        private void Awake()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            if (garageDoor == null) garageDoor = gameObject;
            closedPosition = garageDoor.transform.position;
        }

        public string Prompt
        {
            get
            {
                if (open) return "Portón abierto";
                if (moving) return "El portón está en movimiento";
                if (gameFlow == null || !gameFlow.insideGarage) return "El portón se abre desde adentro";
                return "[E] Abrir portón";
            }
        }

        public void Interact()
        {
            TrySetOpen(!open);
        }

        public bool TrySetOpen(bool value)
        {
            if (gameFlow == null || gameFlow.gameOver || gameFlow.finished || moving || value == open) return false;
            if (!gameFlow.stationPowerConnected)
            {
                gameFlow.ShowMessage("Primero conectá el generador en la caja eléctrica.");
                return false;
            }

            if (!gameFlow.insideGarage)
            {
                gameFlow.ShowMessage("Tenés que entrar al garage para abrir el portón.");
                return false;
            }

            if (garageDoor == null) garageDoor = gameObject;
            if (garageDoor == null) garageDoor = gameObject;
            motion = StartCoroutine(AnimateDoor(value));
            return true;
        }

        private IEnumerator AnimateDoor(bool opening)
        {
            moving = true;
            Renderer[] renderers = garageDoor.GetComponentsInChildren<Renderer>(true);
            Collider[] colliders = garageDoor.GetComponentsInChildren<Collider>(true);
            Bounds bounds = new Bounds(garageDoor.transform.position, Vector3.zero);
            bool foundBounds = false;
            foreach (Renderer item in renderers)
            {
                if (!item.enabled) continue;
                if (!foundBounds) { bounds = item.bounds; foundBounds = true; }
                else bounds.Encapsulate(item.bounds);
            }
            float height = foundBounds ? bounds.size.y : 4f;
            Vector3 start = opening ? closedPosition : garageDoor.transform.position;
            Vector3 end = opening ? closedPosition + Vector3.up * height : closedPosition;

            if (opening) gameFlow.NotifyGarageDoorMotionStarted();
            float elapsed = 0f;
            while (elapsed < travelDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, travelDuration));
                float eased = t * t * (3f - 2f * t);
                garageDoor.transform.position = Vector3.Lerp(start, end, eased);
                if (opening && t > 0.12f)
                    foreach (Collider item in colliders) item.enabled = false;
                else if (!opening && t > 0.82f)
                    foreach (Collider item in colliders) item.enabled = true;
                yield return null;
            }

            garageDoor.transform.position = end;
            open = opening;
            foreach (Collider item in colliders) item.enabled = !opening;
            if (opening && !openedOnce)
            {
                openedOnce = true;
                gameFlow.OpenGarage();
            }
            else
            {
                gameFlow.garageOpen = opening;
                gameFlow.ShowMessage(opening ? "Portón abierto." : "Portón cerrado. Accioná la tecla para abrirlo.");
            }
            moving = false;
            motion = null;
        }
    }
}
