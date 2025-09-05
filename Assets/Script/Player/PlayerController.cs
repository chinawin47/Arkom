using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ARKOM.Core;

namespace ARKOM.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour, PlayerInputActions.IPlayerActions
    {
        [Header("Camera Setup")]
        public Transform cameraRoot; // Empty ที่หัว
        public Camera mainCamera;    // MainCamera (child ของ cameraRoot)

        [Header("Movement Settings")]
        public float walkSpeed = 4f;
        public float sprintSpeed = 6.5f;
        public float crouchSpeed = 2.2f;
        public float crouchHeight = 1.1f;
        public float standingHeight = 1.8f;
        public float gravity = -9.81f;

        [Header("Mouse Settings")]
        [Range(0.1f, 10f)] public float mouseSensitivityX = 2f;
        [Range(0.1f, 10f)] public float mouseSensitivityY = 2f;
        public bool invertY = false;

        [Header("Camera / Interaction")]
        public float interactDistance = 3f;
        public LayerMask interactLayerMask = ~0;

        private PlayerInputActions inputActions;
        private CharacterController controller;
        private Vector2 moveInput;
        private Vector2 lookInput;
        private float xRotation;
        private Vector3 velocity;
        private bool isSprinting;
        private bool isCrouching;
        private float currentSpeed;

        private IInteractable focus;
        public IInteractable CurrentFocus => focus;
        public event Action<IInteractable> FocusChanged;
        private InteractableHighlighter lastHighlighter;

        private GameState currentState = GameState.DayExploration;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            inputActions = new PlayerInputActions();
        }

        void OnEnable()
        {
            inputActions.Player.Enable();
            inputActions.Player.SetCallbacks(this);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameState);
        }

        void OnDisable()
        {
            inputActions.Player.RemoveCallbacks(this);
            inputActions.Player.Disable();
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameState);
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            // ไม่ reset ตำแหน่ง/หมุนกล้อง
            xRotation = cameraRoot ? cameraRoot.localEulerAngles.x : 0f;
        }

        private void OnGameState(GameStateChangedEvent e)
        {
            currentState = e.State;

            bool block = (e.State == GameState.GameOver || e.State == GameState.Victory);
            if (block)
            {
                if (inputActions.Player.enabled)
                    inputActions.Player.Disable();
            }
            else
            {
                if (!inputActions.Player.enabled)
                    inputActions.Player.Enable();
            }
        }

        void Update()
        {
            if (currentState == GameState.GameOver || currentState == GameState.Victory)
                return;

            UpdateFocus();
            HandleMovement();
            HandleCamera();
        }

        private void UpdateFocus()
        {
            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            IInteractable newTarget = null;
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask, QueryTriggerInteraction.Collide))
            {
                hit.collider.TryGetComponent<IInteractable>(out newTarget);
            }

            if (!ReferenceEquals(newTarget, focus))
            {
                if (lastHighlighter)
                    lastHighlighter.SetHighlight(false);

                focus = newTarget;
                lastHighlighter = null;
                if (focus is Component comp)
                {
                    lastHighlighter = comp.GetComponent<InteractableHighlighter>()
                                      ?? comp.GetComponentInChildren<InteractableHighlighter>()
                                      ?? comp.GetComponentInParent<InteractableHighlighter>();
                    if (lastHighlighter)
                        lastHighlighter.SetHighlight(true);
                }
                FocusChanged?.Invoke(focus);
            }
        }

        private void HandleMovement()
        {
            if (isCrouching) currentSpeed = crouchSpeed;
            else if (isSprinting && moveInput.y > 0.1f) currentSpeed = sprintSpeed;
            else currentSpeed = walkSpeed;

            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            Vector3 horizontal = move.normalized * currentSpeed;

            if (controller.isGrounded) velocity.y = -2f;
            else velocity.y += gravity * Time.deltaTime;

            controller.Move((horizontal + velocity) * Time.deltaTime);
        }

        private void HandleCamera()
        {
            float mouseX = lookInput.x * mouseSensitivityX;
            float mouseY = lookInput.y * mouseSensitivityY * (invertY ? 1 : -1);

            // หมุนแนวตั้ง (pitch) ที่ cameraRoot
            xRotation = Mathf.Clamp(xRotation + mouseY, -80f, 80f);
            if (cameraRoot)
                cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // หมุนแนวนอน (yaw) ที่ Player
            transform.Rotate(Vector3.up * mouseX);
        }

        private void ToggleCrouch()
        {
            isCrouching = !isCrouching;
            controller.height = isCrouching ? crouchHeight : standingHeight;
        }

        private void TryInteract()
        {
            if (currentState == GameState.GameOver || currentState == GameState.Victory) return;

            if (focus != null)
            {
                focus.Interact(this);
                return;
            }

            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
                    interactable.Interact(this);
            }
        }

        // Input Callbacks
        public void OnMove(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();
        public void OnLook(InputAction.CallbackContext context) => lookInput = context.ReadValue<Vector2>();
        public void OnSprint(InputAction.CallbackContext context) => isSprinting = context.ReadValue<float>() > 0.5f;
        public void OnCrouch(InputAction.CallbackContext context) { if (context.performed) ToggleCrouch(); }
        public void OnInteract(InputAction.CallbackContext context) { if (context.performed) TryInteract(); }
    }
}