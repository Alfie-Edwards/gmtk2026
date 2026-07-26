using System.Collections;
using UnityEngine;

public class EnemyLava : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 10f;

    [Header("Movement/Animation Settings")]
    public Transform visualRoot; // The part of the lava enemy that moves up and down
    public float hiddenYOffset = -2f; // Local Y position when ducked down
    public float poppedYOffset = 0f;  // Local Y position when popped up
    public float transitionSpeed = 5f;
    public float rotationSpeed = 10f; // How fast it turns to face the player

    [Header("Combat Settings")]
    public GameObject spikePrefab;
    public Transform firePoint;
    public float popWaitTime = 1f;   // Time to wait after popping up before shooting
    public float duckWaitTime = 2f;  // Cooldown time before it can pop up again

    private bool isActionRoutineRunning = false;
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
        if (visualRoot != null)
        {
            // Start fully ducked down
            Vector3 pos = visualRoot.localPosition;
            pos.y = hiddenYOffset;
            visualRoot.localPosition = pos;
        }
    }

    void Update()
    {
        Player player = PlayerInstance;
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            // Always turn to face the player on the Y axis while in range
            Vector3 directionToPlayer = player.transform.position - transform.position;
            directionToPlayer.y = 0f; // Keep rotation level
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Trigger the attack sequence if it's ready
            if (!isActionRoutineRunning)
            {
                StartCoroutine(LavaRoutine());
            }
        }
    }

    IEnumerator LavaRoutine()
    {
        isActionRoutineRunning = true;

        // 1. Pop up
        yield return StartCoroutine(MoveVisualY(poppedYOffset));

        // 2. Wait a bit
        yield return new WaitForSeconds(popWaitTime);

        // 3. Shoot cactus spike forward (in the direction the lava enemy is facing)
        ShootSpike();

        // 4. Duck back down
        yield return StartCoroutine(MoveVisualY(hiddenYOffset));

        // 5. Wait on cooldown before allowing another pop-up
        yield return new WaitForSeconds(duckWaitTime);

        isActionRoutineRunning = false;
    }

    IEnumerator MoveVisualY(float targetY)
    {
        if (visualRoot == null) yield break;

        while (Mathf.Abs(visualRoot.localPosition.y - targetY) > 0.01f)
        {
            Vector3 pos = visualRoot.localPosition;
            pos.y = Mathf.MoveTowards(pos.y, targetY, transitionSpeed * Time.deltaTime);
            visualRoot.localPosition = pos;
            yield return null;
        }

        Vector3 finalPos = visualRoot.localPosition;
        finalPos.y = targetY;
        visualRoot.localPosition = finalPos;
    }

    void ShootSpike()
    {
        if (spikePrefab == null || firePoint == null) return;

        GameObject spikeObj = Instantiate(spikePrefab, firePoint.position, Quaternion.identity);
        CactusSpike spike = spikeObj.GetComponent<CactusSpike>();

        if (spike != null)
        {
            // Shoot straight forward relative to where the enemy is facing
            Vector3 shootDirection = transform.forward;
            spike.Initialize(shootDirection);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}