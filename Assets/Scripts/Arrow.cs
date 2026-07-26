using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(Item))]
public class Arrow : MonoBehaviour
{
    [Header("Flight Settings")]
    public float flySpeed = 20f;

    [Header("Embedding Settings")]
    public float embedDepth = 0.2f;

    private bool isEmbedded = false;
    private Rigidbody rb;
    private Collider col;
    private Vector3 moveDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // Disable gravity so it flies straight
        rb.useGravity = false;
    }

    public void Initialize(Vector3 direction, Collider playerCollider = null)
    {
        moveDirection = direction.normalized;

        // Rotate arrow to face the direction it's traveling
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        // Ignore collision with the player so it doesn't instantly hit them on spawn
        if (playerCollider != null && col != null)
        {
            Debug.Log("Ignore collision");
            Physics.IgnoreCollision(col, playerCollider);
        }

        // Apply velocity to the Rigidbody so physics handles movement and collisions
        rb.linearVelocity = moveDirection * flySpeed;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.name != "Player") Embed(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        Embed(other);
    }

    void Embed(Collider hitCollider)
    {
        if (isEmbedded) return;
        isEmbedded = true;

        // 1. Stop movement and make rigidBody kinematic so it stays put
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        // 2. Since your collider is at the tip, use ClosestPoint to find where the tip touched
        Vector3 tipHitPoint = hitCollider.ClosestPoint(col.bounds.center);

        // 3. Move the arrow so that the tip stays right at the surface,
        // plus your custom embedDepth to push the shaft in nicely.
        // (Note: This assumes your arrow's transform/pivot is at its center or tail.
        // If your pivot is at the tip, just use tipHitPoint + (moveDirection.normalized * embedDepth).)

        transform.position = tipHitPoint + (moveDirection.normalized * embedDepth);

        // 4. Disable hit detection (turn off collider)
        Debug.Log(col);
        if (col != null)
        {
            col.enabled = false;
        }

        // 5. Deal damage if it hit an enemy
        Enemy enemy = hitCollider.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(25f, moveDirection, 0.5f);
        }
        else
        {
            GetComponent<Item>().enabled = true;
        }

        // 6. Parent itself to the hit object so it moves with it
        transform.SetParent(hitCollider.transform);
    }
}