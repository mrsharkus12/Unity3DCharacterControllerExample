using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class CameraController : MonoBehaviour
    {
        public Transform playerCamera;

        public float mouseSensitivity = 0.1f;

        [Header("FOV Settings")]
        public bool changeFovWhenSprinting = true;
        public float sprintFOV = 75f;
        public float fovChangeSpeed = 5f;

        [Header("Zoom Settings")]
        public bool zoomAllowed = true;
        public float zoomLevel = 30f;
        public float zoomSpeed = 10f;
        public bool isZooming = false;

        [Header("Movement Side Tilt Settings")]
        public bool enableTilt = true;
        public float tiltAmount = 5f;
        public float tiltSpeed = 5f;

        private Camera mainCamera;
        private float xRotation = 0f;

        [Header("Debug")]
        public float defaultFOV;
        public float targetFOV;

        private float currentTilt = 0f;

        private PlayerInput playerInput;

        private InputAction moveAction;
        private InputAction lookAction;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            playerInput = GetComponent<PlayerInput>();

            moveAction = playerInput.actions["Move"];
            lookAction = playerInput.actions["Look"];

            mainCamera = playerCamera.GetComponentInChildren<Camera>();
            defaultFOV = mainCamera.fieldOfView;
            targetFOV = defaultFOV;
        }

        void Update()
        {
            Look();
            UpdateFOV();
        }

        void Look()
        {
            Vector2 lookInput = lookAction.ReadValue<Vector2>();

            float mouseX = lookInput.x * mouseSensitivity;
            float mouseY = lookInput.y * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            playerCamera.localRotation = Quaternion.Euler(xRotation + currentTilt, 0f, playerCamera.localEulerAngles.z);

            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            float targetTilt = -moveInput.x * tiltAmount;

            if (moveInput.magnitude < 0.1f)
            {
                targetTilt = 0f;
            }

            if (enableTilt)
            {
                Quaternion targetRotation = Quaternion.Euler(xRotation + currentTilt, 0f, targetTilt);
                playerCamera.localRotation = Quaternion.Lerp(
                    playerCamera.localRotation,
                    targetRotation,
                    tiltSpeed * Time.deltaTime
                );
            }
        }

        void UpdateFOV()
        {
            float speed = isZooming ? zoomSpeed : fovChangeSpeed;
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, speed * Time.deltaTime);
        }

        public void SetSprintFOV(bool isSprinting)
        {
            if (isSprinting && changeFovWhenSprinting)
            {
                targetFOV = sprintFOV;
            }
            else if (!isZooming)
            {
                targetFOV = defaultFOV;
            }
        }
    }
}