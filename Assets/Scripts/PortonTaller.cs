using System.Collections;
using UnityEngine;

namespace EnElCamino
{
    public class PortonTaller : MonoBehaviour, IInteractable
    {
        [SerializeField] private Juego gameFlow;
        [SerializeField] private GameObject garageDoor;
        [SerializeField] private float travelDuration = 2.2f;
        private bool open;
        private bool openedOnce;
        private bool moving;
        private Vector3 closedPosition;
        private Vector3 closedScale;
        private float closedHeight;
        public bool IsOpen => open;
        public bool IsMoving => moving;
        public GameObject DoorObject => garageDoor != null ? garageDoor : gameObject;

        private void Awake()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<Juego>();
            if (garageDoor == null) garageDoor = gameObject;
            closedPosition = garageDoor.transform.position;
            closedScale = garageDoor.transform.localScale;
            closedHeight = GetDoorHeight();
        }

        public string Prompt
        {
            get
            {
                if (open) return "Porton abierto";
                if (moving) return "El porton esta en movimiento";
                if (gameFlow == null || !gameFlow.insideGarage) return "El porton se abre desde adentro";
                return "[E] Abrir porton";
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
                gameFlow.ShowMessage("Primero conecta el generador en la caja electrica.");
                return false;
            }

            if (!gameFlow.insideGarage)
            {
                gameFlow.ShowMessage("Tenes que entrar al taller para abrir el porton.");
                return false;
            }

            if (garageDoor == null) garageDoor = gameObject;
            StartCoroutine(AnimateDoor(value));
            return true;
        }

        private IEnumerator AnimateDoor(bool opening)
        {
            moving = true;
            Collider[] colliders = garageDoor.GetComponentsInChildren<Collider>(true);
            Vector3 start = opening ? closedPosition : garageDoor.transform.position;
            Vector3 startScale = opening ? closedScale : garageDoor.transform.localScale;
            float openScaleY = Mathf.Max(0.035f, closedScale.y * 0.045f);
            Vector3 openScale = new Vector3(closedScale.x, openScaleY, closedScale.z);
            // Roll upward into the lintel: the upper edge stays fixed instead of
            // translating a full door height through the roof.
            Vector3 openPosition = closedPosition + garageDoor.transform.up *
                ((closedHeight - closedHeight * (openScaleY / Mathf.Max(0.0001f, closedScale.y))) * 0.5f);
            Vector3 end = opening ? openPosition : closedPosition;
            Vector3 endScale = opening ? openScale : closedScale;

            if (opening) gameFlow.NotifyGarageDoorMotionStarted();
            float elapsed = 0f;
            while (elapsed < travelDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, travelDuration));
                float eased = t * t * (3f - 2f * t);
                garageDoor.transform.position = Vector3.Lerp(start, end, eased);
                garageDoor.transform.localScale = Vector3.Lerp(startScale, endScale, eased);
                if (opening && t > 0.12f)
                    foreach (Collider item in colliders) item.enabled = false;
                else if (!opening && t > 0.82f)
                    foreach (Collider item in colliders) item.enabled = true;
                yield return null;
            }

            garageDoor.transform.position = end;
            garageDoor.transform.localScale = endScale;
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
                gameFlow.ShowMessage(opening ? "Porton abierto." : "Porton cerrado. Acciona la tecla para abrirlo.");
            }
            moving = false;
        }

        private float GetDoorHeight()
        {
            Renderer[] renderers = garageDoor.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = new Bounds(garageDoor.transform.position, Vector3.zero);
            bool foundBounds = false;
            foreach (Renderer item in renderers)
            {
                if (!item.enabled) continue;
                if (!foundBounds) { bounds = item.bounds; foundBounds = true; }
                else bounds.Encapsulate(item.bounds);
            }
            return foundBounds ? Mathf.Max(0.1f, bounds.size.y) : 4f;
        }
    }
}
