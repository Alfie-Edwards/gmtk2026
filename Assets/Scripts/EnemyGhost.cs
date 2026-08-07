using System.Collections;
using UnityEngine;

public class EnemyGhost : MonoBehaviour
{
    [Header("Detection & Despawn Settings")]
    public float detectionRadius = 15f;
    public float maxDistanceFromPlayer = 30f; // Distance at which the ghost despawns

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float verticalLerpSpeed = 0.5f; // Moves faster on the Y axis
    public float rotationSpeed = 5f;

    [Header("Stealth & Visibility Settings")]
    [Range(0f, 1f)] public float targetOpacity = 0.5f; // Configurable opacity when visible
    public float fadeDuration = 2f;                   // Time it takes to fade in

    [Header("Dash Settings")]
    public float minDashInterval = 4f;
    public float maxDashInterval = 8f;
    public float dashDistance = 8f;
    public float dashDuration = 0.5f;                  // How long the dash takes

    [Header("Renderer / Material")]
    public Renderer enemyRenderer;                    // Assign your mesh renderer here

    private bool isDashing = false;
    private bool isDashRoutineRunning = false;
    private Player cachedPlayer;
    private CharacterController cc;

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
        cc = GetComponent<CharacterController>();
        SetAlpha(0f); // Start completely invisible
    }

    void Update()
    {
        Player player = PlayerInstance;
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // Despawn if the ghost moves too far away from the player
        if (distanceToPlayer > maxDistanceFromPlayer)
        {
            Destroy(gameObject);
            return;
        }

        if (distanceToPlayer <= detectionRadius)
        {
            // Face the player smoothly
            Vector3 directionToPlayer = player.transform.position - transform.position;
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Normal movement if not currently dashing
            if (!isDashing)
            {
                MoveTowardsPlayer(player.transform.position);
            }

            // Trigger random dash loop
            if (!isDashRoutineRunning)
            {
                StartCoroutine(DashRoutine());
            }
        }
    }

    void MoveTowardsPlayer(Vector3 playerPosition)
    {
        float step = moveSpeed * Time.deltaTime;
        Vector3 delta = (playerPosition - transform.position).normalized * step;
        delta += Vector3.up * Mathf.Lerp(0, playerPosition.y - transform.position.y - delta.y, Time.deltaTime);
        cc.Move(delta);
    }

    IEnumerator DashRoutine()
    {
        isDashRoutineRunning = true;

        float randomInterval = Random.Range(minDashInterval, maxDashInterval);
        yield return new WaitForSeconds(randomInterval);

        Player player = PlayerInstance;
        if (player != null)
        {
            // Lock onto where the player is right now at the start of the dash
            Vector3 startPos = transform.position;
            Vector3 directionToPlayer = (player.transform.position - startPos).normalized;
            if (directionToPlayer == Vector3.zero) directionToPlayer = transform.forward;

            Vector3 endPos = startPos + (directionToPlayer * dashDistance);

            isDashing = true;
            float elapsedTime = 0f;

            // Dash with ease-in and ease-out
            while (elapsedTime < dashDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / dashDuration;

                // SmoothStep provides ease-in and ease-out curve
                float smoothedT = Mathf.SmoothStep(0f, 1f, t);

                Vector3 delta = Vector3.Lerp(startPos, endPos, smoothedT) - transform.position;
                cc.Move(delta);
                yield return null;
            }
            isDashing = false;
        }

        isDashRoutineRunning = false;
    }

    public IEnumerator FadeIn()
    {
        if (enemyRenderer == null) yield break;

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(0f, targetOpacity, elapsedTime / fadeDuration);
            SetAlpha(currentAlpha);
            yield return null;
        }
        SetAlpha(targetOpacity);
    }

    void SetAlpha(float alpha)
    {
        if (enemyRenderer == null) return;

        foreach (Material mat in enemyRenderer.materials)
        {
            if (mat.HasProperty("_Color"))
            {
                Color color = mat.color;
                color.a = alpha;
                mat.color = color;
            }
            else if (mat.HasProperty("_BaseColor"))
            {
                Color color = mat.GetColor("_BaseColor");
                color.a = alpha;
                mat.SetColor("_BaseColor", color);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistanceFromPlayer);
    }
}