using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] Camera view;
    [SerializeField] float reach = 3f;
    [SerializeField] TMP_Text prompt;
    [SerializeField] TerminalInteractable terminal;
    [SerializeField] FinishTrigger finish;

    public IInteractable FindTarget()
    {
        Ray ray = view.ViewportPointToRay(new Vector3(.5f, .5f));
        // El primer collider bloquea el rayo: no se interactúa a través de paredes.
        if (Physics.Raycast(ray, out RaycastHit hit, 12f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && Vector3.Distance(transform.position, hit.collider.ClosestPoint(transform.position)) <= reach)
            return hit.collider.GetComponentInParent<IInteractable>();
        return null;
    }
    void Update()
    {
        if (finish.Completed) { prompt.text = "RECORRIDO COMPLETADO\nR: volver al inicio"; return; }
        if (Cursor.lockState != CursorLockMode.Locked) { prompt.text = "Clic para jugar"; return; }
        var target = FindTarget();
        prompt.text = target != null ? target.Prompt : terminal.Activated
            ? "Energía restaurada. Cruzá la puerta y llegá a la zona cian."
            : "Buscá la terminal cian. Acercate y apuntá con la mira.";
        if (target != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            target.Interact();
    }
}
