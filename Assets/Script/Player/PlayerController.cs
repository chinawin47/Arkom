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
        public Transform cameraRoot;
        public Camera mainCamera;

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

        [Header("Flashlight")]
        public Flashlight flashlight;              // จะถูกตั้งค่าเมื่อเก็บได้
        public Key flashlightKey = Key.L;          // ปุ่มไฟฉาย
        [Tooltip("เริ่มเกมมีไฟฉายเลยหรือไม่ (ถ้าไม่ ต้อฃเก็บก่อน)")]
        public bool startWithFlashlight = false;
        [Tooltip("ตำแหน่งยึดไฟฉายใต้กล้องหลังจากเก็บ (ถ้าว่าง ใช้ cameraRoot)")]
        public Transform flashlightAttachPoint;
        private bool hasFlashlight;                // เก็บแล้วหรือยัง

        [Header("Seating / Sit Mode")]
        public Key seatToggleKey = Key.F; // ใช้ลุกอย่างเดียว (นั่งต้อง Interact กับเก้าอี้)
        public bool lockPitchWhileSeated = true; // ล็อกมุมก้ม/เงยตอนนั่ง
        public float seatedPitch = 0f;          // มุมคงที่ถ้าล็อก

        private PlayerInputActions inputActions;
        private CharacterController controller;
        private Vector2 moveInput;
        private Vector2 lookInput;
        private float xRotation;
        private Vector3 velocity;
        private bool isSprinting;
        private bool isCrouching;
        private float currentSpeed;

        // Seating state
        public bool IsSeated => isSeated;
        private bool isSeated;
        private Transform currentSeat;          // จุดที่นั่ง (anchor player)
        private Transform currentSeatCamPoint;  // จุดกล้องเฉพาะของเก้าอี้ (optional)
        private float seatYaw;                  // ทิศผู้เล่นตอนนั่ง

        // เก็บค่าเดิมของ cameraRoot ตอนใช้ camera override
        private Vector3 cameraLocalPosDefault;
        private Quaternion cameraLocalRotDefault;
        private bool storedCameraDefault;
        private bool usingCameraOverride;

        private IInteractable focus;
        public IInteractable CurrentFocus => focus;
        public event Action<IInteractable> FocusChanged;
        private InteractableHighlighter lastHighlighter;

        private GameState currentState = GameState.DayExploration;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            inputActions = new PlayerInputActions();

            if (startWithFlashlight && flashlight)
            {
                hasFlashlight = true;
                AttachFlashlightParent();
            }
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
            else if (!inputActions.Player.enabled)
            {
                inputActions.Player.Enable();
            }
        }

        void Update()
        {
            if (currentState == GameState.GameOver || currentState == GameState.Victory)
                return;

            HandleSeatToggleInput(); // F = ลุก (ถ้านั่งอยู่)
            HandleFlashlightInput();
            UpdateFocus();
            HandleMovement();
            HandleCamera();
        }

        private void HandleSeatToggleInput()
        {
            if (!isSeated) return;
            var kb = Keyboard.current;
            if (kb != null && kb[seatToggleKey].wasPressedThisFrame)
            {
                ExitSeat();
            }
        }

        private void HandleFlashlightInput()
        {
            if (!hasFlashlight) return;          // ยังไม่เก็บ
            if (!flashlight) return;              // ไม่มีอ้างอิง
            if (isSeated) return;                 // ไม่ให้เปิดขณะนั่ง (เอาออกหากต้องการ)
            var kb = Keyboard.current;
            if (kb != null && kb[flashlightKey].wasPressedThisFrame)
            {
                flashlight.Toggle();
            }
        }

        // เรียกจาก SeatInteractable เมื่อนั่ง
        public void EnterSeat(Transform seatAnchor, Transform cameraPoint)
        {
            if (seatAnchor == null) return;

            currentSeat = seatAnchor;
            currentSeatCamPoint = cameraPoint;
            isSeated = true;
            isCrouching = false; // ปิด crouch
            controller.height = crouchHeight;   // ใช้ความสูงนั่ง
            velocity = Vector3.zero;
            moveInput = Vector2.zero;

            transform.position = seatAnchor.position;
            transform.rotation = Quaternion.Euler(0f, seatAnchor.eulerAngles.y, 0f);
            seatYaw = transform.eulerAngles.y;

            if (cameraRoot)
            {
                if (!storedCameraDefault)
                {
                    cameraLocalPosDefault = cameraRoot.localPosition;
                    cameraLocalRotDefault = cameraRoot.localRotation;
                    storedCameraDefault = true;
                }
                if (cameraPoint)
                {
                    cameraRoot.position = cameraPoint.position;
                    cameraRoot.rotation = cameraPoint.rotation;
                    usingCameraOverride = true;
                    if (lockPitchWhileSeated)
                    {
                        Vector3 e = cameraRoot.localEulerAngles;
                        xRotation = e.x;
                        seatedPitch = xRotation;
                    }
                }
                else
                {
                    usingCameraOverride = false;
                }
            }
        }

        public void ExitSeat()
        {
            if (!isSeated) return;
            isSeated = false;
            currentSeat = null;
            currentSeatCamPoint = null;
            controller.height = standingHeight;

            if (usingCameraOverride && cameraRoot)
            {
                cameraRoot.localPosition = cameraLocalPosDefault;
                cameraRoot.localRotation = cameraLocalRotDefault;
            }
            usingCameraOverride = false;
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
            if (isSeated)
            {
                velocity = Vector3.zero;
                return;
            }

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

            if (isSeated && lockPitchWhileSeated)
            {
                xRotation = seatedPitch; // ล็อก pitch
                mouseY = 0f;
            }
            else
            {
                xRotation = Mathf.Clamp(xRotation + mouseY, -80f, 80f);
            }

            if (cameraRoot)
                cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            transform.Rotate(Vector3.up * mouseX);
        }

        private void ToggleCrouch()
        {
            if (isSeated) return;
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

        // เรียกโดย FlashlightPickupInteractable เมื่อผู้เล่นเก็บไฟฉาย
        public void AcquireFlashlight(Flashlight picked, bool setOn = false)
        {
            if (picked == null) return;
            flashlight = picked;
            hasFlashlight = true;
            AttachFlashlightParent();
            if (setOn)
                flashlight.SetOn(true);
            else
                flashlight.SetOn(false); // ปิดไว้ก่อนให้ผู้เล่นกดเอง
        }

        private void AttachFlashlightParent()
        {
            if (!flashlight) return;
            Transform parent = flashlightAttachPoint ? flashlightAttachPoint : cameraRoot;
            if (parent)
            {
                flashlight.transform.SetParent(parent, worldPositionStays: false);
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