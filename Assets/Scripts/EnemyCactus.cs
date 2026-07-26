using UnityEngine;

public class EnemyCactus : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 10f;

    [Header("Combat Settings")]
    public GameObject spikePrefab;
    public Transform firePoint;
    public float fireCooldown = 2f;
    public float spawnOffset = 1.5f;

    private float fireTimer;
    private Player cachedPlayer;

    // Player property with lazy initialization and caching
    private Player PlayerInstance
    {
        get
        {
            if (cachedPlayer == null)
            {
                Player foundPlayer = FindAnyObjectByType<Player>();
                if (foundPlayer != null)
                {
                    cachedPlayer = foundPlayer;
                }
            }
            return cachedPlayer;
        }
    }

    void Update()
    {
        Player player = PlayerInstance;
        if (player == null) return;

        // Check distance to the player
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            // Handle shooting cooldown
            fireTimer += Time.deltaTime;
            if (fireTimer >= fireCooldown)
            {
                ShootSpikeRing();
                fireTimer = 0f;
            }
        }
        else
        {
            // Reset timer so it shoots immediately when player re-enters range
            fireTimer = fireCooldown;
        }
    }

    void ShootSpikeRing()
    {
        if (spikePrefab == null || firePoint == null) return;

        int spikeCount = 16;
        float angleStep = 360f / spikeCount;

        for (int i = 0; i < spikeCount; i++)
        {
            float currentAngle = i * angleStep;
            Vector3 shootDirection = Quaternion.Euler(0f, currentAngle, 0f) * Vector3.forward;
            Vector3 spawnPosition = firePoint.position + (shootDirection * spawnOffset);

            GameObject spikeObj = Instantiate(spikePrefab, spawnPosition, Quaternion.identity);
            CactusSpike spike = spikeObj.GetComponent<CactusSpike>();

            if (spike != null)
            {
                spike.Initialize(shootDirection);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}