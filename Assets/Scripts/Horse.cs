using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Horse : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Riding & Proximity Detection")]
    public Transform mountPoint; // Define this in the Inspector where the player should sit
    public float detectionRadius = 3.0f; // Radius around mount point to check for proximity
    public LayerMask playerLayer; // Layer mask to filter the player

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 knockbackVelocity = Vector3.zero;
    private Quaternion lookTarget;
    
    private bool isBeingRidden = false;
    private GameObject rider;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }
        lookTarget = transform.rotation;
    }

    void Update()
    {
        // If not being ridden, check if a player is nearby, above the mount point, and falling
        if (!isBeingRidden)
        {
            CheckForRiderProximity();
            return;
        }

        if (transform.position.z < 0 && transform.position.x > 25)
        {
            SceneManager.LoadScene("WIN");
        }

        float moveForwardAmount = 0;
        float moveRightAmount = 0;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.isPressed) moveForwardAmount += 1;
            if (Keyboard.current.downArrowKey.isPressed) moveForwardAmount -= 1;
            if (Keyboard.current.rightArrowKey.isPressed) moveRightAmount += 1;
            if (Keyboard.current.leftArrowKey.isPressed) moveRightAmount -= 1;
        }
        Vector3 move = ((Vector3.forward * moveForwardAmount) + (Vector3.right * moveRightAmount)).normalized * moveSpeed;

        // Jump
        if (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        // Gravity
        if (!controller.isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // Move the Controller
        controller.Move((velocity + move + knockbackVelocity) * Time.deltaTime);

        if (move != Vector3.zero)
        {
            lookTarget = Quaternion.LookRotation(move);
        }
        transform.rotation = Quaternion.Slerp(transform.rotation, lookTarget, Mathf.Min(4f * Time.deltaTime, 1f));
    }

    private void CheckForRiderProximity()
    {
        if (mountPoint == null) return;

        Collider[] hits = Physics.OverlapSphere(mountPoint.position, detectionRadius, playerLayer);

        foreach (Collider hit in hits)
        {
            if (hit.GetComponent<Player>() is Player player)
            {
                if (hit.transform.position.y >= mountPoint.position.y && player.velocity.y < 0)
                {
                    Ride(hit.gameObject);
                    break;
                }
            }
        }
    }

    public void Ride(GameObject candidate)
    {
        if (isBeingRidden || candidate == null) return;

        isBeingRidden = true;
        rider = candidate;

        if (rider.GetComponent<Player>() is Player player)
        {
            player.disableControls = true;
        }

        if (rider.GetComponent<CharacterController>() is CharacterController cc)
        {
            cc.Move(Vector3.zero);
            cc.enabled = false;
        }

        if (mountPoint != null)
        {
            rider.transform.position = mountPoint.position;
            rider.transform.rotation = mountPoint.rotation;
            rider.transform.SetParent(mountPoint.transform);
        }
        else
        {
            rider.transform.SetParent(transform);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (mountPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(mountPoint.position, detectionRadius);
        }
    }
}