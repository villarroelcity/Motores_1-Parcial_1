using UnityEngine;
using UnityEngine.InputSystem;

// Se actualiza después del personaje para seguir su posición final de este frame.
public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float distance = 5f;
    [SerializeField] float height = 1.2f;
    [SerializeField] float sensitivity = .12f;
    [SerializeField] LayerMask obstacleMask = 1;
    float yaw;
    float pitch = 20f;

    void OnEnable() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }
    void LateUpdate()
    {
        if (!target) return;
        if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
        {
            // Mouse.delta ya es un desplazamiento por frame: no se multiplica por deltaTime.
            Vector2 look = Mouse.current.delta.ReadValue();
            yaw += look.x * sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, -20f, 65f);
        }
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 pivot = target.position + Vector3.up * height;
        Vector3 backward = rotation * Vector3.back;
        float safeDistance = distance;
        if (Physics.SphereCast(pivot, .2f, backward, out RaycastHit hit, distance,
            obstacleMask, QueryTriggerInteraction.Ignore)) safeDistance = Mathf.Max(.05f, hit.distance - .05f);
        transform.SetPositionAndRotation(pivot + backward * safeDistance, rotation);
    }
}
