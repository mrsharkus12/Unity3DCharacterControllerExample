using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float walkSpeed = 5f;
        public float sprintSpeed = 8f;
        public float crouchSpeed = 2.5f;

        [Header("Jump and Gravity Settings")]
        public float jumpForce = 5f;
        public float gravity = -13f;
        public float crouchHeight = 1f;
        public float standHeight = 2f;

        [Header("Stamina Settings")]
        public float maxStamina = 100f;
        public float staminaDepletionRate = 10f;
        public float staminaRegenerationRate = 5f;
        public float staminaRegenerationDelay = 2f;
        public float currentStamina;

        [System.Serializable]
        public class Inertia
        {
            public float acceleration = 5f;
            public float airControl = 5f;
            public float sprintJumpBoost = 2f;
        }

        [Header("Inertia Settings")]
        [SerializeField] private Inertia inertia;

        [Header("Crouch Settings")]
        public float crouchSmoothSpeed = 10f;

        [Header("Cheats")]
        [Header("Stamina")]
        public bool infiniteStamina = false;
        [Header("Noclip")]
        public bool isNoclipActive = false;
        public float noclipSpeed = 10f;
        public float noclipFastSpeed = 20f;
        public float noclipVerticalSpeed = 5f;

        [Header("Debug")]
        public bool isGrounded;
        public bool isCrouching;
        public bool isSprinting;
        public bool wasSprintingOnJump;

        public float timeSinceLastSprint;

        public Vector3 currentMoveDirection;
        public Vector3 targetMoveDirection;
        public Vector3 airMoveDirection;
        public Vector3 verticalVelocity;

        public float velocity;

        [SerializeField] private float targetHeight;

        private CharacterController characterController;
        private PlayerInput playerInput;
        private CameraController cameraController;

        private Vector3 previousPosition;

        [HideInInspector] public InputAction moveAction;
        [HideInInspector] public InputAction jumpAction;
        [HideInInspector] public InputAction crouchAction;
        [HideInInspector] public InputAction sprintAction;

        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            cameraController = GetComponent<CameraController>();
            playerInput = GetComponent<PlayerInput>();

            targetHeight = standHeight;

            moveAction = playerInput.actions["Move"];
            jumpAction = playerInput.actions["Jump"];
            crouchAction = playerInput.actions["Crouch"];
            sprintAction = playerInput.actions["Sprint"];

            currentStamina = maxStamina;

            previousPosition = transform.position;
        }

        private void Update()
        {
            isGrounded = characterController.isGrounded;

            Move();
            Crouch();
            Sprint();
            Jump();
            CheckCeiling();
            UpdateStamina();
            CalculateVelocity();
        }

        void Move()
        {
            if (isNoclipActive)
            {
                Vector2 moveInput = moveAction.ReadValue<Vector2>();
                Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

                float verticalInput = 0f;
                if (Keyboard.current.spaceKey.isPressed) verticalInput = 1f;
                if (Keyboard.current.leftCtrlKey.isPressed) verticalInput = -1f;

                moveDirection += transform.up * verticalInput;

                float speed = isSprinting ? noclipFastSpeed : noclipSpeed;
                transform.position += moveDirection * speed * Time.deltaTime;
            }
            else
            {
                isGrounded = characterController.isGrounded;

                if (isGrounded && verticalVelocity.y < 0)
                {
                    verticalVelocity.y = -2f;
                }

                Vector2 moveInput = moveAction.ReadValue<Vector2>();
                targetMoveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

                if (isGrounded)
                {
                    currentMoveDirection = Vector3.Lerp(currentMoveDirection, targetMoveDirection, inertia.acceleration * Time.deltaTime);
                }
                else
                {
                    Vector3 airTargetDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
                    airMoveDirection = Vector3.Lerp(airMoveDirection, airTargetDirection, inertia.airControl * Time.deltaTime);
                    currentMoveDirection = Vector3.Lerp(currentMoveDirection, airMoveDirection, inertia.airControl * Time.deltaTime);
                }

                float moveSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
                characterController.Move(currentMoveDirection * moveSpeed * Time.deltaTime);

                verticalVelocity.y += gravity * Time.deltaTime;
                characterController.Move(verticalVelocity * Time.deltaTime);
            }
        }

        void ToggleNoclip()
        {
            isNoclipActive = !isNoclipActive;
            characterController.enabled = !isNoclipActive;

            if (isNoclipActive)
            {
                verticalVelocity = Vector3.zero;
            }
        }

        void Jump()
        {
            if (jumpAction.triggered && isGrounded)
            {
                verticalVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

                if (currentMoveDirection.magnitude > 0.1f)
                {
                    airMoveDirection = currentMoveDirection.normalized;

                    if (isSprinting)
                    {
                        airMoveDirection += transform.forward * inertia.sprintJumpBoost;
                        wasSprintingOnJump = true;
                    }
                    else
                    {
                        wasSprintingOnJump = false;
                    }
                }
                else
                {
                    airMoveDirection = Vector3.zero;
                    wasSprintingOnJump = false;
                }
            }
        }

        void Crouch()
        {
            bool isCrouchHeld = crouchAction.ReadValue<float>() > 0.5f;

            if (isCrouchHeld)
            {
                if (!isCrouching)
                {
                    targetHeight = crouchHeight;
                    isCrouching = true;
                }
            }
            else
            {
                if (isCrouching)
                {
                    if (!Physics.Raycast(transform.position, Vector3.up, standHeight))
                    {
                        targetHeight = standHeight;
                        isCrouching = false;
                    }
                }
            }

            if (characterController.height != targetHeight)
            {
                float newHeight = Mathf.Lerp(characterController.height, targetHeight, Time.deltaTime * crouchSmoothSpeed);
                float heightDifference = characterController.height - newHeight;

                Vector3 newPosition = transform.position;
                newPosition.y -= heightDifference * 0.5f;
                transform.position = newPosition;

                characterController.height = newHeight;
            }
        }

        void Sprint()
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            bool wasSprinting = isSprinting;

            if (isGrounded || wasSprintingOnJump)
            {
                isSprinting = sprintAction.ReadValue<float>() > 0.5f && !isCrouching && currentStamina > 0;
            }
            else
            {
                isSprinting = false;
            }

            if (cameraController != null)
            {
                cameraController.SetSprintFOV(isSprinting && currentMoveDirection.magnitude > 0.1f && moveInput.magnitude > 0.1f);
            }
        }

        private void CalculateVelocity()
        {
            Vector3 currentPosition = transform.position;
            float distanceMoved = Vector3.Distance(currentPosition, previousPosition);

            velocity = distanceMoved / Time.deltaTime;
            previousPosition = currentPosition;
        }

        private void CheckCeiling()
        {
            if ((characterController.collisionFlags & CollisionFlags.Above) != 0)
            {
                verticalVelocity.y = -2f;
            }
        }

        private void UpdateStamina()
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();

            bool isMoving = moveInput.magnitude > 0.1f;

            if (isSprinting && isMoving)
            {
                if (!infiniteStamina)
                {
                    currentStamina -= staminaDepletionRate * Time.deltaTime;
                    currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
                    timeSinceLastSprint = 0f;
                }
            }
            else
            {
                timeSinceLastSprint += Time.deltaTime;
                if (timeSinceLastSprint >= staminaRegenerationDelay)
                {
                    currentStamina += staminaRegenerationRate * Time.deltaTime;
                    currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
                }
            }

            if (currentStamina <= 0)
            {
                isSprinting = false;
            }
        }
    }
}