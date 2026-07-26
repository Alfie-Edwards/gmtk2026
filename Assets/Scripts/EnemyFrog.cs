using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyFrog : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 12f;

    [Header("Jump Settings")]
    public float minJumpInterval = 2f;
    public float maxJumpInterval = 5f;
    public float jumpHeight = 2f;       // How many units up it will go
    public float jumpDistance = 4f;     // How many units forward along a flat plane it will go
    public float rotationSpeed = 10f;
    public float gravity = 20f;

    private CharacterController controller;
    private Vector3 moveDirection;
    private bool isWaitingToJump = false;
    private Player cachedPlayer;

    private Player PlayerInstance
    {
        get
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = FindAnyObjectByType<Player>();
            }
            return cachedPlayer;
        }
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        Player player = PlayerInstance;
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            // Always turn to face the player on the Y axis while in range
            Vector3 directionToPlayer = player.transform.position - transform.position;
            directionToPlayer.y = 0f;
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Start the random interval jump loop if it isn't already running
            if (!isWaitingToJump)
            {
                StartCoroutine(JumpRoutine());
            }
        }

        // Handle Gravity and Stationary Landing
        if (controller.isGrounded && moveDirection.y <= 0)
        {
            moveDirection.x = 0f;
            moveDirection.z = 0f;
            moveDirection.y = -2f; // Small downward force to stick to ground
        }
        else
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Move the CharacterController every frame
        controller.Move(moveDirection * Time.deltaTime);
    }

    IEnumerator JumpRoutine()
    {
        isWaitingToJump = true;

        // Pick a random wait time between min and max intervals
        float randomInterval = Random.Range(minJumpInterval, maxJumpInterval);
        yield return new WaitForSeconds(randomInterval);

        // Calculate initial vertical velocity needed to reach target jumpHeight: v = sqrt(2 * gravity * height)
        float verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);

        // Calculate total time in the air for a standard projectile arc (time to peak * 2)
        float timeToAirborne = (2f * verticalVelocity) / gravity;

        // Calculate horizontal speed needed to cover jumpDistance over that total air time
        float horizontalSpeed = (timeToAirborne > 0f) ? (jumpDistance / timeToAirborne) : 0f;

        // Apply velocities
        Vector3 horizontalDir = transform.forward * horizontalSpeed;
        moveDirection.x = horizontalDir.x;
        moveDirection.z = horizontalDir.z;
        moveDirection.y = verticalVelocity;

        // Wait a brief moment before allowing the next jump interval to start
        yield return new WaitForSeconds(0.5f);

        isWaitingToJump = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}