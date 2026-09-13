using UnityEngine;

public class TerminalInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] GameObject corePrefab;
    [SerializeField] Transform spawnPoint;
    [SerializeField] GameObject door;
    [SerializeField] Material activeMaterial;
    public bool Activated { get; private set; }
    public string Prompt => Activated ? "Terminal activada. La salida está abierta." : "E: restaurar energía";

    public void Interact()
    {
        if (Activated) return; // Una activación: no genera núcleos infinitos.
        Instantiate(corePrefab, spawnPoint.position, spawnPoint.rotation);
        Activated = true;
        GetComponent<Renderer>().sharedMaterial = activeMaterial;
        door.SetActive(false);
        Debug.Log("Terminal activada: núcleo instanciado y puerta abierta.");
    }
}
