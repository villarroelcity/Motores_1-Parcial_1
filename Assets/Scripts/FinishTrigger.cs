using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class FinishTrigger : MonoBehaviour
{
    [SerializeField] TerminalInteractable terminal;
    public bool Completed { get; private set; }
    void OnTriggerEnter(Collider other)
    {
        if (Completed || !terminal.Activated || !other.GetComponent<PlayerMovement>()) return;
        Completed = true;
        Debug.Log("Núcleo Cero: recorrido completado.");
    }
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
