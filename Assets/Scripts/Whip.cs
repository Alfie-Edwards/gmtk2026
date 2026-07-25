using UnityEngine;
using System.Collections;

public class Whip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rope rope;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Attack Settings")]
    public float throwDuration = 0.5f;
    public float retractDuration = 0.1f;
    public Vector3 localThrowDirection = new Vector3(0f, 1f, 1f);
    public float idleOffsetZ = 0.001f;

    [Header("Combat Settings")]
    public float hitRadius = 0.1f;
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

        if (isAttacking && rope != null && rope.startTransform != null && rope.endTransform != null)
        {
            CheckHits();
        }
    }

    void CheckHits()
    {
        Debug.Log("Check hits");
        Vector3 startPos = rope.startTransform.position;
        Vector3 endPos = rope.endTransform.position;
        Vector3 hitDirection = (endPos - startPos).normalized;

        Collider[] hits = Physics.OverlapSphere(endPos, hitRadius, enemyLayer);
        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage, hitDirection);
            }
        }
    }

    void SnapToStart()
    {
        if (rope != null && rope.endTransform != null && rope.startTransform != null)
        {
            rope.endTransform.position = rope.startTransform.position + new Vector3(0f, 0f, idleOffsetZ);
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
        Vector3 localDirNormalized = localThrowDirection.normalized;
        Vector3 worldPeakPos = transform.TransformPoint(localDirNormalized * rope.ropeLength);

        float elapsed = 0f;

        // 1. Throw out
        while (elapsed < throwDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / throwDuration);

            worldPeakPos = transform.TransformPoint(localDirNormalized * rope.ropeLength);
            rope.endTransform.position = Vector3.Lerp(initialPos, worldPeakPos, t);
            yield return null;
        }
        rope.endTransform.position = worldPeakPos;

        elapsed = 0f;
        Vector3 returnPos = rope.startTransform.position + new Vector3(0f, 0f, idleOffsetZ);

        // 2. Retract back
        while (elapsed < retractDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / retractDuration);

            returnPos = rope.startTransform.position + new Vector3(0f, 0f, idleOffsetZ);
            rope.endTransform.position = Vector3.Lerp(worldPeakPos, returnPos, t);
            yield return null;
        }

        SnapToStart();
        isAttacking = false;
    }
}