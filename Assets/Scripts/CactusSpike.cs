using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class CactusSpike : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 15f;
    public float lifetime = 5f;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
    }

    public void Initialize(Vector3 direction)
    {
        // 1. Force the direction to be level (strip out vertical Y movement)
        direction.y = 0f;
        moveDirection = direction.normalized;

        // 2. Rotate to face the direction of travel
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        // 3. Apply velocity immediately here so it doesn't wait for FixedUpdate
        rb.linearVelocity = moveDirection * moveSpeed;

        // Destroy the spike after a set time if it misses
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        if (hasHit) return;

        // Keep movement locked at steady speed
        rb.linearVelocity = moveDirection * moveSpeed;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit || collision.collider.name == "Cactus") return;
        hasHit = true;

        // Check if it hit the Player
        Player player = collision.collider.GetComponent<Player>();
        if (player != null)
        {
            player.GetHit();
        }

        BreakSpike();
    }

    void BreakSpike()
    {
        Destroy(gameObject);
    }
}