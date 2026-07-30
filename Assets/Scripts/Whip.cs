using UnityEngine;
using System.Collections;

public class Whip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rope rope;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private GameObject whipCrackPrefab; // Field for the whip crack prefab

    public float throwDuration = 0.5f;
    public float retractDuration = 0.1f;
    public Vector3 localThrowDirection = new Vector3(0f, 0f, 1f);
    public float idleOffsetZ = 0.001f;
    public float hitRadius = 0.5f;
    public float damage = 25f;
    public LayerMask enemyLayer;

    private bool isAttacking = false;

    void Awake()
    {
        SnapToStart();
    }

    void Update()
    {
        if (!isAttacking && rope != null && rope.endTransform != null)
        {
            SnapToStart();
        }
    }

    void SnapToStart()
    {
        if (rope != null && rope.endTransform != null && rope.startTransform != null)
        {
            Vector3 localIdleOffset = new Vector3(0f, 0f, idleOffsetZ);
            rope.endTransform.position = rope.startTransform.position + transform.TransformDirection(localIdleOffset);
        }
    }

    public void Attack()
    {
        if (isAttacking || rope == null || rope.endTransform == null || rope.startTransform == null) return;
        StartCoroutine(PerformWhipAttack());
    }

    public Vector3 GetCurrentEndPosition()
    {
        return (rope != null && rope.endTransform != null) ? rope.endTransform.position : transform.position;
    }

    private IEnumerator PerformWhipAttack()
    {
        isAttacking = true;

        SnapToStart();
        Vector3 initialPos = rope.endTransform.position;

        float elapsed = 0f;

        Vector3 WorldPeakPos() {
            Vector3 peak = rope.startTransform.position + transform.TransformDirection(localThrowDirection.normalized * rope.ropeLength);
            peak.y = rope.startTransform.position.y;
            return peak;
        }

        bool hitEnemy = false;

        // 1. Throw out
        while (elapsed < throwDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / throwDuration);

            rope.endTransform.position = Vector3.Lerp(initialPos, WorldPeakPos(), t);

            // Check for hits during the throw; if something is hit, spawn prefab and break early
            if (CheckHitAndRegister())
            {
                hitEnemy = true;
                SpawnWhipCrack(rope.endTransform.position);
                break;
            }

            yield return null;
        }

        // If we didn't break early, ensure it reaches the peak position
        if (!hitEnemy)
        {
            rope.endTransform.position = WorldPeakPos();
        }

        Vector3 currentPosAtRetract = rope.endTransform.position;

        elapsed = 0f;

        // 2. Retract back
        while (elapsed < retractDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / retractDuration);

            Vector3 localIdleOffset = new Vector3(0f, 0f, idleOffsetZ);
            Vector3 returnPos = rope.startTransform.position + transform.TransformDirection(localIdleOffset);
            returnPos.y = rope.startTransform.position.y;

            rope.endTransform.position = Vector3.Lerp(currentPosAtRetract, returnPos, t);
            yield return null;
        }

        SnapToStart();
        isAttacking = false;
    }

    private void SpawnWhipCrack(Vector3 spawnPosition)
    {
        if (whipCrackPrefab != null)
        {
            Instantiate(whipCrackPrefab, spawnPosition, Random.rotation);
        }
    }

    private bool CheckHitAndRegister()
    {
        Vector3 endPos = rope.endTransform.position;
        Vector3 startPos = rope.startTransform.position;
        Vector3 hitDirection = (endPos - startPos).normalized;

        Collider[] hits = Physics.OverlapSphere(endPos, hitRadius, enemyLayer);
        bool anyHit = false;
        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage, hitDirection);
            }
            if (hit.name != "Player") anyHit = true;
        }
        return anyHit;
    }

    public void Reset()
    {
        rope.InitializeWhip();
    }
}