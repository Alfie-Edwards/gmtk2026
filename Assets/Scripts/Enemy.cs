using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Enemy : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float gravity = 9.81f;
    public float jumpForce = 3f;
    [SerializeField] private float detectionRadius = 10f;

    // Property with a backing field to find the player via component
    private Transform playerTransform;
    private Transform Player
    {
        get
        {
            if (playerTransform == null)
            {
                Player playerComponent = Object.FindAnyObjectByType<Player>();
                if (playerComponent != null)
                {
                    playerTransform = playerComponent.transform;
                }
            }
            return playerTransform;
        }
    }

    private CharacterController controller;
    private Vector3 verticalVelocity;
    private float jumpCooldownTimer = 0f; // Cooldown to prevent multi-jumping instantly

    [Header("Health & Combat")]
    public float maxHealth = 100f;
    private float currentHealth;
    public float hitCooldown = 0.5f;
    private float lastHitTime = -999f;

    [Header("Knockback Settings")]
    public float knockbackMagnitude = 5f;
    private Vector3 knockbackVelocity;
    public float knockbackDamping = 5f;
    public float contactForce = 10f;

    [Header("Loot Settings")]
    public GameObject coinPrefab;
    public GameObject xxxPrefab;
    public int minCoins = 1;
    public int maxCoins = 5;
    public bool canDropXXX = true;
    public float coinSpawnSpread = 0.5f;
    private bool aggro = false;

    void Start()
    {
        currentHealth = maxHealth;
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }

        HandleMovement();

        if (transform.position.z < 0)
        {
            Cull();
        }
    }

    void Cull()
    {
        minCoins = 0;
        maxCoins = 0;
        canDropXXX = false;
        Die();
    }

    void HandleMovement()
    {
        Vector3 moveDir = Vector3.zero;
        bool isTryingToMove = false;

        if (Player != null)
        {
            Vector3 toPlayer = Player.position - transform.position;
            toPlayer.y = 0f;
            if (!aggro)
            {
                aggro = toPlayer.sqrMagnitude <= detectionRadius * detectionRadius;
            }

            if (aggro)
            {
                if (toPlayer.magnitude > 0.1f)
                {
                    moveDir = toPlayer.normalized * moveSpeed;
                    transform.rotation = Quaternion.LookRotation(toPlayer);
                    isTryingToMove = true;
                }
            }
        }

        if (controller.isGrounded && verticalVelocity.y <= 0)
        {
            verticalVelocity.y = -2f;
        }
        else
        {
            verticalVelocity.y -= gravity * Time.deltaTime;
        }

        if (knockbackVelocity.magnitude > 0.1f)
        {
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDamping * Time.deltaTime);
        }
        else
        {
            knockbackVelocity = Vector3.zero;
        }

        Vector3 finalMovement = moveDir + verticalVelocity + knockbackVelocity;
        controller.Move(finalMovement * Time.deltaTime);

        if (controller.isGrounded && isTryingToMove && (controller.collisionFlags & CollisionFlags.Sides) != 0 && jumpCooldownTimer <= 0f)
        {
            Jump(jumpForce);
            jumpCooldownTimer = 1.5f;
        }
    }

    public void TakeDamage(float damageAmount, Vector3 hitDirection, float knockbackModifier = 1.0f)
    {
        if (Time.time < lastHitTime + hitCooldown) return;

        lastHitTime = Time.time;
        currentHealth -= damageAmount;

        ApplyKnockback(hitDirection, knockbackModifier);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void ApplyKnockback(Vector3 hitDirection, float knockbackModifier)
    {
        hitDirection.y = 0f;
        knockbackVelocity = hitDirection.normalized * knockbackMagnitude * knockbackModifier;
    }

    public void Jump(float jumpForce)
    {
        verticalVelocity.y = jumpForce;
    }

    void Die()
    {
        // Drop all embedded arrows before destroying or spawning loot
        foreach (Transform child in transform)
        {
            Arrow arrow = child.GetComponent<Arrow>();
            if (arrow != null)
            {
                child.SetParent(null);

                Rigidbody arrowRb = child.GetComponent<Rigidbody>();
                if (arrowRb != null)
                {
                    arrowRb.isKinematic = false;
                    arrowRb.useGravity = true;
#if UNITY_6000_0_OR_NEWER
                    arrowRb.linearVelocity = Vector3.zero;
#else
                    arrowRb.velocity = Vector3.zero;
#endif
                }

                Collider arrowCol = child.GetComponent<Collider>();
                if (arrowCol != null)
                {
                    arrowCol.enabled = true;
                }
            }
        }

        SpawnLoot();
        Destroy(gameObject);

        if (GetComponent<EnemyRock>() != null)
        {
            Player.GetComponent<Player>().win = true;
        }
    }

    void SpawnLoot()
    {
        if (coinPrefab != null)
        {
            int coinCount = Random.Range(minCoins, maxCoins + 1);

            for (int i = 0; i < coinCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * coinSpawnSpread;
                Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

                Instantiate(coinPrefab, spawnPosition, Random.rotation);
            }
        }
        if (xxxPrefab != null && canDropXXX)
        {
            if (Random.Range(0, 8) == 0) {
                Instantiate(xxxPrefab, transform.position, Quaternion.identity);
            }
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Player player = hit.collider.GetComponent<Player>();
        if (player != null)
        {
            Debug.Log("HIT PLAYER");
            Vector3 knockbackDir = -hit.normal;
            knockbackDir.y *= 0.01f;
            knockbackDir.Normalize();
            player.GetHit(knockbackDir * contactForce);
        }
    }
}