using UnityEngine;
using UnityEngine.InputSystem;

namespace EnElCamino
{
    public class RaycastInteractor : MonoBehaviour
    {
        [SerializeField] private float distance = 3f;
        private Camera mainCamera;
        private IInteractable current;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            current = FindInteractable();
            if (current == null)
            {
                GameUI.Instance?.HidePrompt();
                return;
            }

            GameUI.Instance?.ShowPrompt(current.Prompt);
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                current.Interact();
        }

        private IInteractable FindInteractable()
        {
            Ray ray = mainCamera != null
                ? mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : new Ray(transform.position + Vector3.up, transform.forward);

            if (!Physics.Raycast(ray, out RaycastHit hit, distance)) return null;
            MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
                if (behaviour is IInteractable interactable) return interactable;
            return null;
        }
    }
}
