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
    public float explosionDamage = 250f;
    public GameObject explosionPrefab;

    private Rigidbody rb;
    private bool hasExploded = false;

    public void Initialize(Vector3 throwDirection)
    {
        rb = GetComponent<Rigidbody>();
        // Ensure it uses true physics instead of being kinematic
        rb.isKinematic = false;
        rb.useGravity = true;

        throwDirection.y = 0f;
        throwDirection.Normalize();

        // Calculate required initial velocities to match the desired arc and distance
        // Horizontal velocity: distance / time
        float horizontalSpeed = totalDistance / flightDuration;
        Vector3 horizontalVelocity = throwDirection * horizontalSpeed;

        // Vertical velocity: using kinematic equation s = ut + 0.5at^2 solved for u
        // s = peakHeight, t = flightDuration / 2 (time to peak), a = gravity (-Physics.gravity.y)
        float gravity = Mathf.Abs(Physics.gravity.y);
        float timeToPeak = flightDuration * 0.5f;
        float verticalVelocityY = (peakHeight + 0.5f * gravity * timeToPeak * timeToPeak) / timeToPeak;

        // Combine into the final throw velocity vector
        rb.linearVelocity = new Vector3(horizontalVelocity.x, verticalVelocityY, horizontalVelocity.z);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        if (collision.collider.GetComponent<Player>() != null) return;
        Explode();
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;
        if (other.GetComponent<Player>() != null) return;
        Explode();
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        Vector3 center = transform.position;
        Collider[] hits = Physics.OverlapSphere(center, explosionRadius);

        foreach (Collider hit in hits)
        {
            if (hit.GetComponent<Rock>() is Rock rock)
            {
                rock.Break();
                continue;
            }

            if (hit.GetComponent<CrackedWall>() is CrackedWall wall)
            {
                wall.Break();
                continue;
            }

            if ((hit.GetComponent<Enemy>() ?? hit.GetComponentInParent<Enemy>()) is Enemy enemy)
            {
                Vector3 hitVector = enemy.transform.position - center;
                float effectAmount = hitVector.sqrMagnitude / (explosionRadius * explosionRadius);
                
                if (enemy.GetComponent<EnemyRock>() is EnemyRock rockEnemy)
                {
                    enemy.TakeDamage(explosionDamage * 2.5f * effectAmount, hitVector, 5 * effectAmount);
                }
                else if (enemy.GetComponent<EnemyGhost>() is EnemyGhost ghost)
                {
                    enemy.TakeDamage(1e10f, Vector3.zero);
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