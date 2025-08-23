using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, PlayerInputActions.IPlayerActions
{
    public float walkSpeed = 4f;
    public float sprintSpeed = 6.5f;
    public float crouchSpeed = 2.2f;
    public float crouchHeight = 1.1f;
    public float standingHeight = 1.8f;
    public float gravity = -9.81f;

    public Transform playerCamera;
    public float interactDistance = 3f;
    public LayerMask interactLayerMask = ~0;

    private PlayerInputActions input;
    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRot;
    private Vector3 velocity;
    private bool isSprinting;
    private bool isCrouching;
    private float currentSpeed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = new PlayerInputActions();
    }

    void OnEnable()
    {
        input.Player.Enable();
        input.Player.SetCallbacks(this);
    }

    void OnDisable()
    {
        input.Player.RemoveCallbacks(this);
        input.Player.Disable();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        controller.height = standingHeight;
    }

    void Update()
    {
        HandleMovement();
        HandleCamera();
    }

    private void HandleMovement()
    {
        if (isCrouching) currentSpeed = crouchSpeed;
        else if (isSprinting && moveInput.y > 0.1f) currentSpeed = sprintSpeed;
        else currentSpeed = walkSpeed;

        var move = transform.right * moveInput.x + transform.forward * moveInput.y;
        var horizontal = move.normalized * currentSpeed;

        if (controller.isGrounded) velocity.y = -2f;
        else velocity.y += gravity * Time.deltaTime;

        controller.Move((horizontal + velocity) * Time.deltaTime);
    }

    private void HandleCamera()
    {
        float mx = lookInput.x * 2f;
        float my = lookInput.y * 2f * -1f;
        xRot = Mathf.Clamp(xRot + my, -80f, 80f);
        playerCamera.localRotation = Quaternion.Euler(xRot, 0f, 0f);
        transform.Rotate(Vector3.up * mx);
    }

    private void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        controller.height = isCrouching ? crouchHeight : standingHeight;
    }

    private void TryInteract()
    {
        var ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out var hit, interactDistance, interactLayerMask, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
                interactable.Interact(this);
        }
    }

    // Input callbacks
    public void OnMove(InputAction.CallbackContext c) => moveInput = c.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext c) => lookInput = c.ReadValue<Vector2>();
    public void OnSprint(InputAction.CallbackContext c) => isSprinting = c.ReadValue<float>() > 0.5f;
    public void OnCrouch(InputAction.CallbackContext c) { if (c.performed) ToggleCrouch(); }
    public void OnInteract(InputAction.CallbackContext c) { if (c.performed) TryInteract(); }
}