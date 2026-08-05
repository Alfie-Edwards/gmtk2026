using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(Item))]
public class Arrow : MonoBehaviour
{
    [Header("Flight Settings")]
    public float flySpeed = 20f;

    [Header("Embedding Settings")]
    [Tooltip("How far forward the arrow's tip should penetrate past the collision point.")]
    public float embedDepth = 1f;

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
        if (collision.collider.name != "Player") Embed(collision.collider, collision);
    }

    void OnTriggerEnter(Collider other)
    {
        Embed(other);
    }

    void Embed(Collider hitCollider, Collision collision = null)
    {
        if (isEmbedded) return;
        RuntimeManager.PlayOneShot("event:/SFX/Player/SFX_PlayerGetsHit");
        isEmbedded = true;
        Debug.Log("Embedding!");

        if (hitCollider.GetComponent<Enemy>() is Enemy enemy)
        {
            Debug.Log("Hit enemy with arrow!");
            enemy.TakeDamage(75f, moveDirection, 0.5f);
            if (enemy.currentHealth <= 0f)
            {
                // Special case where arrow hits enemy and kills it.
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
                GetComponent<Item>().enabled = true;
                return;
            }
        }
        else
        {
            GetComponent<Item>().enabled = true;
        }

        if (hitCollider.GetComponent<Target>() is Target target)
        {
            target.Trigger();
        }   

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Use the actual contact point if available to avoid floating, falling back to ClosestPoint
        Vector3 hitPoint = (collision != null && collision.contactCount > 0) 
            ? collision.GetContact(0).point 
            : hitCollider.ClosestPoint(transform.position);

        // Push the arrow inward by the embed depth along its travel direction
        transform.position = hitPoint + (moveDirection.normalized * embedDepth);

        if (col != null)
        {
            col.enabled = false;
        }

        transform.SetParent(hitCollider.transform);
    }
}