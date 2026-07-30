using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Rooster : MonoBehaviour
{
    [Header("Movement Settings")]
    public float normalTargetDistance = 3f;  // The normal distance it tries to keep from the player
    public float runAwayDistance = 10f;      // The distance it tries to reach when hit
    public float runAwayDuration = 2f;       // How long it stays in the run-away state
    public float moveSpeed = 5f;             // Maximum movement speed
    public float stoppingDamping = 2f;       // Smoothing factor to slow down as it approaches the target
    public float turnSpeed = 10f;            // How fast the rooster rotates (degrees per second)
    public float gravity = -9.81f;           // Gravity force for falling

    [Header("Boid / Separation Settings")]
    public float separationRadius = 1.5f;    // Distance to maintain from other roosters
    public float separationWeight = 1.5f;    // How strongly they push away from each other

    private float currentTargetDistance;
    private float runAwayTimer = 0f;
    private bool isRunningAway = false;
    private CharacterController controller;
    private float verticalVelocity = 0f;
    private Vector3 smoothedVelocity = Vector3.zero; // Tracks velocity smoothly to prevent jittery changes
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

    void Start()
    {
        currentTargetDistance = normalTargetDistance;
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        Player player = PlayerInstance;
        if (player == null) return;

        // Handle gravity via CharacterController
        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f; // Small downward force to stay grounded
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 targetVelocity = Vector3.zero;

        // Check if player's Z position is less than 0
        if (player.transform.position.z < 0f)
        {
            // Handle run-away timer expiration
            if (isRunningAway)
            {
                runAwayTimer -= Time.deltaTime;
                if (runAwayTimer <= 0f)
                {
                    isRunningAway = false;
                    currentTargetDistance = normalTargetDistance;
                }
            }

            // Flatten Y positions to 0 so movement calculation stays horizontal
            Vector3 playerPosXZ = new Vector3(player.transform.position.x, 0f, player.transform.position.z);
            Vector3 currentPosXZ = new Vector3(transform.position.x, 0f, transform.position.z);

            Vector3 toPlayer = playerPosXZ - currentPosXZ;
            float distanceToPlayer = toPlayer.magnitude;
            
            if (distanceToPlayer > 0.001f)
            {
                toPlayer.Normalize();

                float distanceError = distanceToPlayer - currentTargetDistance;
                float desiredSpeed = distanceError * stoppingDamping;
                desiredSpeed = Mathf.Clamp(desiredSpeed, -moveSpeed, moveSpeed);

                Vector3 playerMovement = toPlayer * desiredSpeed;
                Vector3 separationForce = CalculateSmoothSeparation() * separationWeight;

                targetVelocity = playerMovement + separationForce;
                
                if (targetVelocity.magnitude > moveSpeed)
                {
                    targetVelocity = targetVelocity.normalized * moveSpeed;
                }
            }
        }

        // Smooth out velocity changes to eliminate the jittery/snappy sideways bumping
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, targetVelocity, 10f * Time.deltaTime);

        // Face the exact direction they are currently moving (ignoring Y)
        Vector3 moveDirXZ = new Vector3(smoothedVelocity.x, 0f, smoothedVelocity.z);
        if (moveDirXZ.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirXZ);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // Apply movement (XZ velocity + vertical gravity) via CharacterController
        Vector3 totalMove = new Vector3(smoothedVelocity.x, verticalVelocity, smoothedVelocity.z);
        controller.Move(totalMove * Time.deltaTime);
    }

    private Vector3 CalculateSmoothSeparation()
    {
        Vector3 steering = Vector3.zero;
        int count = 0;

        Rooster[] allRoosters = FindObjectsByType<Rooster>();

        foreach (Rooster other in allRoosters)
        {
            if (other == this) continue;

            Vector3 toOther = transform.position - other.transform.position;
            toOther.y = 0f; 
            float distance = toOther.magnitude;

            if (distance > 0f && distance < separationRadius)
            {
                float normalizedDist = distance / separationRadius;
                float strength = 1f - normalizedDist;
                steering += toOther.normalized * strength;
                count++;
            }
        }

        if (count > 0)
        {
            steering /= count;
            steering = steering.normalized * moveSpeed;
        }

        return steering;
    }

    public void Hit()
    {
        isRunningAway = true;
        currentTargetDistance = runAwayDistance;
        runAwayTimer = runAwayDuration;
    }
}