/*
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

        if (distanceToPlayer <= detectionRadius && Mathf.Abs(transform.position.y - 0.5f - player.transform.position.y) < 1f)
        {
            // Handle shooting cooldown
            fireTimer += Time.deltaTime;
            if (fireTimer >= fireCooldown)
            {
                ShootSpikeRing();
                fireTimer = 0f;
            }
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
}*/

using System.Collections;
using UnityEngine;

public class EnemyCactus : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 10f;

    [Header("Blend Shape Animation")]
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private string squashBlendShapeName = "squash";

    [Tooltip("Time taken to squash from 0% to 100%.")]
    [SerializeField] private float squashDuration = 1f;

    [Tooltip("Time taken to return from 100% to 0%.")]
    [SerializeField] private float releaseDuration = 0.15f;

    [Header("Combat Settings")]
    public GameObject spikePrefab;
    public Transform firePoint;
    public float fireCooldown = 2f;
    public float spawnOffset = 1.5f;

    private float fireTimer;
    private bool isAttacking;
    private int squashBlendShapeIndex = -1;
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

    private void Awake()
    {
        FindSquashBlendShape();
    }

    private void Start()
    {
        SetSquashWeight(0f);
    }

    private void Update()
    {
        Player player = PlayerInstance;

        if (player == null)
        {
            return;
        }

        float distanceToPlayer = Vector3.Distance(
            transform.position,
            player.transform.position
        );

        bool playerIsInRange =
            distanceToPlayer <= detectionRadius &&
            Mathf.Abs(
                transform.position.y - 0.5f -
                player.transform.position.y
            ) < 1f;

        if (!playerIsInRange || isAttacking)
        {
            return;
        }

        fireTimer += Time.deltaTime;

        if (fireTimer >= fireCooldown)
        {
            fireTimer = 0f;
            StartCoroutine(AttackRoutine());
        }
    }

    private void FindSquashBlendShape()
    {
        if (skinnedMeshRenderer == null)
        {
            Debug.LogError(
                $"{name}: No SkinnedMeshRenderer has been assigned.",
                this
            );

            return;
        }

        Mesh mesh = skinnedMeshRenderer.sharedMesh;

        if (mesh == null)
        {
            Debug.LogError(
                $"{name}: The SkinnedMeshRenderer has no mesh.",
                this
            );

            return;
        }

        squashBlendShapeIndex =
            mesh.GetBlendShapeIndex(squashBlendShapeName);

        if (squashBlendShapeIndex < 0)
        {
            Debug.LogError(
                $"{name}: Blend shape '{squashBlendShapeName}' was not found.",
                this
            );
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // Slowly compress from 0% to 100%.
        yield return AnimateSquash(
            startWeight: 0f,
            targetWeight: 100f,
            duration: squashDuration
        );

        // Quickly spring back from 100% to 0%.
        yield return AnimateSquash(
            startWeight: 100f,
            targetWeight: 0f,
            duration: releaseDuration
        );

        // Fire as soon as the cactus finishes springing back.
        ShootSpikeRing();

        isAttacking = false;
    }

    private IEnumerator AnimateSquash(
        float startWeight,
        float targetWeight,
        float duration
    )
    {
        if (squashBlendShapeIndex < 0)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            SetSquashWeight(targetWeight);
            yield break;
        }

        float elapsedTime = 0f;

        SetSquashWeight(startWeight);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float amount = Mathf.Clamp01(
                elapsedTime / duration
            );

            float weight = Mathf.Lerp(
                startWeight,
                targetWeight,
                amount
            );

            SetSquashWeight(weight);

            yield return null;
        }

        SetSquashWeight(targetWeight);
    }

    private void SetSquashWeight(float weight)
    {
        if (skinnedMeshRenderer == null ||
            squashBlendShapeIndex < 0)
        {
            return;
        }

        skinnedMeshRenderer.SetBlendShapeWeight(
            squashBlendShapeIndex,
            Mathf.Clamp(weight, 0f, 100f)
        );
    }

    private void ShootSpikeRing()
    {
        if (spikePrefab == null || firePoint == null)
        {
            return;
        }

        const int spikeCount = 16;
        float angleStep = 360f / spikeCount;

        for (int i = 0; i < spikeCount; i++)
        {
            float currentAngle = i * angleStep;

            Vector3 shootDirection =
                Quaternion.Euler(0f, currentAngle, 0f) *
                Vector3.forward;

            Vector3 spawnPosition =
                firePoint.position +
                shootDirection * spawnOffset;

            GameObject spikeObject = Instantiate(
                spikePrefab,
                spawnPosition,
                Quaternion.LookRotation(shootDirection)
            );

            CactusSpike spike =
                spikeObject.GetComponent<CactusSpike>();

            if (spike != null)
            {
                spike.Initialize(shootDirection);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRadius
        );
    }
}