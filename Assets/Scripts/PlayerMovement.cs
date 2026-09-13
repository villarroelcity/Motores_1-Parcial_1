using UnityEngine;
using UnityEngine.InputSystem;

// CharacterController resuelve las colisiones; nosotros calculamos movimiento y gravedad.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] Transform cameraTransform;
    [SerializeField] float speed = 4f;
    [SerializeField] float rotationSpeed = 540f;
    [SerializeField] float gravity = -20f;
    CharacterController controller;
    InputActionAsset runtimeActions;
    InputAction move;
    Vector3 startPosition;
    float verticalSpeed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        startPosition = transform.position;
        runtimeActions = Instantiate(inputActions);
        move = runtimeActions.FindAction("Player/Move", true);
    }
    void OnEnable() { move?.Enable(); }
    void OnDisable() { move?.Disable(); }
    void OnDestroy() { if (runtimeActions) Destroy(runtimeActions); }

    void Update()
    {
        // Normalizar evita que caminar en diagonal sea más rápido.
        Vector2 input = Cursor.lockState == CursorLockMode.Locked ? move.ReadValue<Vector2>() : Vector2.zero;
        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 direction = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2f;
        verticalSpeed += gravity * Time.deltaTime;
        controller.Move((direction * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
        if (direction.sqrMagnitude > .001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
        if (transform.position.y < -10f) Respawn();
    }

    public void Respawn()
    {
        controller.enabled = false;
        transform.position = startPosition;
        verticalSpeed = 0;
        controller.enabled = true;
    }
}
