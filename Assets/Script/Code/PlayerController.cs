using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ARKOM.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float walkSpeed = 4f;
        public float sprintSpeed = 6.5f;
        public float crouchSpeed = 2.2f;
        public float crouchHeight = 1.1f;
        public float standingHeight = 1.8f;
        public float gravity = -9.81f;

        [Header("Mouse Settings")]
        [Tooltip("Horizontal look sensitivity")]
        [Range(0.1f, 10f)] public float mouseSensitivityX = 2f;
        [Tooltip("Vertical look sensitivity")]
        [Range(0.1f, 10f)] public float mouseSensitivityY = 2f;
        [Tooltip("Invert vertical axis")]
        public bool invertY = false;

        [Header("Camera Settings")]
        public Transform playerCamera;
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

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            inputActions = new PlayerInputActions();
        }

        void OnEnable()
        {
            inputActions.Player.Enable();

            inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;

            inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
            inputActions.Player.Look.canceled += _ => lookInput = Vector2.zero;
        }

        void OnDisable()
        {
            inputActions.Player.Disable();
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            currentSpeed = walkSpeed;
            controller.height = standingHeight;
        }

        void Update()
        {
            PollExtraInputs();   // Sprint / Crouch / Interact (keyboard polling to avoid needing extra InputActions)
            HandleMovement();
            HandleCamera();
        }

        private void PollExtraInputs()
        {
            if (Keyboard.current == null) return;

            // Sprint (hold LeftShift)
            isSprinting = Keyboard.current.leftShiftKey.isPressed && !isCrouching;

            // Crouch toggle (press LeftCtrl)
            if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
                ToggleCrouch();

            // Interact (press E)
            if (Keyboard.current.eKey.wasPressedThisFrame)
                TryInteract();
        }

        private void HandleMovement()
        {
            if (isCrouching)
                currentSpeed = crouchSpeed;
            else if (isSprinting && moveInput.y > 0.1f)
                currentSpeed = sprintSpeed;
            else
                currentSpeed = walkSpeed;

            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            Vector3 horizontal = move.normalized * currentSpeed;

            if (controller.isGrounded)
                velocity.y = -2f;
            else
                velocity.y += gravity * Time.deltaTime;

            controller.Move((horizontal + velocity) * Time.deltaTime);
        }

        private void HandleCamera()
        {
            float mouseX = lookInput.x * mouseSensitivityX;
            float mouseY = lookInput.y * mouseSensitivityY * (invertY ? 1 : -1);

            xRotation = Mathf.Clamp(xRotation + mouseY, -80f, 80f);
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        private void ToggleCrouch()
        {
            isCrouching = !isCrouching;
            controller.height = isCrouching ? crouchHeight : standingHeight;
        }

        private void TryInteract()
        {
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
                    interactable.Interact(this);
            }
        }
    }
}