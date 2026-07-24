using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Keyboard")]
    public float moveSpeed = 5.0f;
    public float gravity = -9.81f * 2.0f;
    public float jumpHeight = 1.5f;

    [Header("Mouse")]
    public Transform camera;
    public float mouseSensitivity = 20f;
    public float cameraPitchMin = -45.0f;
    public float cameraPitchMax = 45.0f;

    private CharacterController controller;
    private Vector3 velocity = Vector3.zero;
    private float cameraPitch = 0;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (camera != null && Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

            cameraPitch += -mouseDelta.y;
            cameraPitch = Mathf.Clamp(cameraPitch, cameraPitchMin, cameraPitchMax);
            camera.localRotation = Quaternion.Euler(cameraPitch, 0, 0);

            transform.rotation *= Quaternion.Euler(0, mouseDelta.x, 0);
        }

        float moveForwardAmount = 0;
        float moveRightAmount = 0;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveForwardAmount += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveForwardAmount -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveRightAmount += 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveRightAmount -= 1;
        }

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        Vector3 move = ((forward * moveForwardAmount) + (right * moveRightAmount)).normalized * moveSpeed;


        // Jump
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        // Gravity
        if (!controller.isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // 4. Move the Controller
        controller.Move((velocity + move) * Time.deltaTime);
    }
}