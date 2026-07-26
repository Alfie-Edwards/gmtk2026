using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bomb : MonoBehaviour
{
    [Header("Throw Settings")]
    public float peakHeight = 1f;
    public float totalDistance = 5f;
    public float flightDuration = 0.6f;

    [Header("Explosion Settings")]
    public float explosionRadius = 4f;
    public float explosionDamage = 100f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float elapsedTime = 0f;
    private Rigidbody rb;
    private bool hasExploded = false;

    public void Initialize(Vector3 throwDirection)
    {
        rb = GetComponent<Rigidbody>();
        // Keep it kinematic during custom arc flight so physics doesn't interfere
        rb.isKinematic = true;

        startPosition = transform.position;

        throwDirection.y = 0f;
        throwDirection.Normalize();

        targetPosition = startPosition + (throwDirection * totalDistance);
    }

    void Update()
    {
        if (hasExploded) return;

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / flightDuration);

        Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, t);
        float heightOffset = 4 * peakHeight * t * (1f - t);
        currentPos.y = Mathf.Lerp(startPosition.y, targetPosition.y, t) + heightOffset;

        transform.position = currentPos;

        if (t >= 1f)
        {
            Explode();
        }
    }

    // Trigger explosion early if it hits a wall, floor, or enemy collider
    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        Explode();
    }

    // If using trigger colliders for hits
    void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;
        // Optional: Ignore the player if the bomb spawns right on top of them
        if (other.GetComponent<Player>() != null) return;

        Explode();
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        Vector3 center = transform.position;
        Collider[] hits = Physics.OverlapSphere(center, explosionRadius);

        foreach (Collider hit in hits)
        {
            Rock rock = hit.GetComponent<Rock>();
            if (rock != null)
            {
                rock.Break();
                continue;
            }

            CrackedWall wall = hit.GetComponent<CrackedWall>();
            if (wall != null)
            {
                wall.Break();
                continue;
            }

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                Vector3 hitVector = hit.transform.position - center;
                float effectAmount = hitVector.sqrMagnitude / (explosionRadius * explosionRadius);
                EnemyRock rockEnemy = hit.GetComponent<EnemyRock>();
                if (rockEnemy != null)
                {
                    enemy.TakeDamage(explosionDamage * 2.5f * effectAmount, hitVector, 5 * effectAmount);
                }
                else
                {
                    enemy.TakeDamage(explosionDamage * effectAmount, hitVector, 5 * effectAmount);
                }
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}