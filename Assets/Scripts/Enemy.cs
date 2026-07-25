using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Enemy : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float gravity = 9.81f;

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

    [Header("Health & Combat")]
    public float maxHealth = 100f;
    private float currentHealth;
    public float hitCooldown = 0.5f;
    private float lastHitTime = -999f;

    [Header("Knockback Settings")]
    public float knockbackMagnitude = 5f;
    private Vector3 knockbackVelocity;
    public float knockbackDamping = 5f;

    [Header("Loot Settings")]
    public GameObject coinPrefab;
    public int minCoins = 1;
    public int maxCoins = 5;
    public float coinSpawnSpread = 1f;

    void Start()
    {
        currentHealth = maxHealth;
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        Vector3 moveDir = Vector3.zero;

        if (Player != null)
        {
            Vector3 toPlayer = Player.position - transform.position;
            toPlayer.y = 0f; // Keep movement flat

            if (toPlayer.magnitude > 0.1f)
            {
                moveDir = toPlayer.normalized * moveSpeed;
                transform.rotation = Quaternion.LookRotation(toPlayer);
            }
        }

        // Handle gravity / ground sticking
        if (controller.isGrounded)
        {
            verticalVelocity.y = -2f;
        }
        else
        {
            verticalVelocity.y -= gravity * Time.deltaTime;
        }

        // Decay knockback smoothly over time
        if (knockbackVelocity.magnitude > 0.1f)
        {
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDamping * Time.deltaTime);
        }
        else
        {
            knockbackVelocity = Vector3.zero;
        }

        // Execute movement via CharacterController exclusively
        Vector3 finalMovement = moveDir + verticalVelocity + knockbackVelocity;
        controller.Move(finalMovement * Time.deltaTime);
    }

    public void TakeDamage(float damageAmount, Vector3 hitDirection)
    {
        if (Time.time < lastHitTime + hitCooldown) return;

        lastHitTime = Time.time;
        currentHealth -= damageAmount;

        ApplyKnockback(hitDirection);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void ApplyKnockback(Vector3 hitDirection)
    {
        hitDirection.y = 0f;
        knockbackVelocity = hitDirection.normalized * knockbackMagnitude;
    }

    public void Jump(float jumpForce)
    {
        verticalVelocity.y = jumpForce;
    }

    void Die()
    {
        SpawnCoins();
        Destroy(gameObject);
    }

    void SpawnCoins()
    {
        if (coinPrefab == null) return;

        int coinCount = Random.Range(minCoins, maxCoins + 1);

        for (int i = 0; i < coinCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * coinSpawnSpread;
            Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            Instantiate(coinPrefab, spawnPosition, Quaternion.identity);
        }
    }
}