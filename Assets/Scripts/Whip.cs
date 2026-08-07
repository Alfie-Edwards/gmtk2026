using FMODUnity;
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
    private Vector3 previousEndPosition; // Tracked for capsule casting

    void Awake()
    {
        SnapToStart();
        if (rope != null && rope.endTransform != null)
        {
            previousEndPosition = rope.endTransform.position;
        }
    }

    void Update()
    {
        if (!isAttacking && rope != null && rope.endTransform != null)
        {
            SnapToStart();
            previousEndPosition = rope.endTransform.position;
        }
    }

    void SnapToStart()
    {
        if (rope != null && rope.endTransform != null && rope.startTransform != null)
        {
            Vector3 localIdleOffset = new Vector3(0f, 0f, idleOffsetZ);
            rope.endTransform.position = rope.startTransform.position + transform.TransformDirection(localIdleOffset);
            previousEndPosition = rope.endTransform.position;
        }
    }

    public void Attack(Vector3 direction)
    {
        if (isAttacking || rope == null || rope.endTransform == null || rope.startTransform == null) return;
        direction = Quaternion.FromToRotation(Vector3.forward, localThrowDirection) * direction;
        StartCoroutine(PerformWhipAttack(direction));
    }

    public Vector3 GetCurrentEndPosition()
    {
        return (rope != null && rope.endTransform != null) ? rope.endTransform.position : transform.position;
    }

    private IEnumerator PerformWhipAttack(Vector3 direction)
    {
        RuntimeManager.PlayOneShot("event:/SFX/Weapons/SFX_WhipCrack");
        isAttacking = true;

        SnapToStart();
        Vector3 initialPos = rope.endTransform.position;
        previousEndPosition = initialPos; // Initialize sweep start

        float elapsed = 0f;

        Vector3 WorldPeakPos() {
            Vector3 peak = rope.startTransform.position + direction.normalized * rope.ropeLength;
            peak.y = rope.startTransform.position.y;
            return peak;
        }

        bool hitEnemy = false;

        // 1. Throw out
        while (elapsed < throwDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / throwDuration);

            // Update positions for capsule sweep
            previousEndPosition = rope.endTransform.position;
            rope.endTransform.position = Vector3.Lerp(initialPos, WorldPeakPos(), t);

            // Check for hits during the throw using capsule sweep
            if (CheckHitAndRegister(out Vector3 pos))
            {
                hitEnemy = true;
                SpawnWhipCrack(pos);
                break;
            }

            yield return null;
        }

        // If we didn't break early, ensure it reaches the peak position
        if (!hitEnemy)
        {
            previousEndPosition = rope.endTransform.position;
            rope.endTransform.position = WorldPeakPos();
            
            if (CheckHitAndRegister(out Vector3 pos))
            {
                SpawnWhipCrack(pos);
            }
        }

        Vector3 currentPosAtRetract = rope.endTransform.position;
        previousEndPosition = currentPosAtRetract;

        elapsed = 0f;

        // 2. Retract back
        while (elapsed < retractDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / retractDuration);

            Vector3 localIdleOffset = new Vector3(0f, 0f, idleOffsetZ);
            Vector3 returnPos = rope.startTransform.position + transform.TransformDirection(localIdleOffset);
            returnPos.y = rope.startTransform.position.y;

            previousEndPosition = rope.endTransform.position;
            rope.endTransform.position = Vector3.Lerp(currentPosAtRetract, returnPos, t);

            yield return null;
        }

        SnapToStart();
        isAttacking = false;
    }

    private void SpawnWhipCrack(Vector3 spawnPosition)
    {
        RuntimeManager.PlayOneShot("event:/SFX/Player/SFX_PlayerGetsHit");
        if (whipCrackPrefab != null)
        {
            Instantiate(whipCrackPrefab, spawnPosition, Random.rotation);
        }
    }

    private bool CheckHitAndRegister(out Vector3 pos)
    {
        pos = Vector3.zero;
        Vector3 currentEndPos = rope.endTransform.position;
        Vector3 startPos = rope.startTransform.position;
        Vector3 hitDirection = (currentEndPos - startPos).normalized;

        Collider[] hits = Physics.OverlapCapsule(previousEndPosition + hitDirection * hitRadius, currentEndPos, hitRadius, enemyLayer);
        
        bool anyHit = false;
        foreach (Collider hit in hits)
        {
            if (hit.GetComponent<Enemy>() is Enemy enemy)
            {
                enemy.TakeDamage(damage, hitDirection);
            }
            else if (hit.GetComponent<Rooster>() is Rooster rooster)
            {
                rooster.Hit();
            }
            if (hit.name != "Player")
            {
                pos = ProjectPointOnLineSegment(previousEndPosition, currentEndPos, hit.ClosestPoint(startPos));
                anyHit = true;
            }
        }
        return anyHit;
    }

    public void Reset()
    {
        StopAllCoroutines();
        SnapToStart();
        rope.Reset();
        isAttacking = false;
    }

    private Vector3 ProjectPointOnLineSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 lineVector = lineEnd - lineStart;
        float lineLengthSquared = lineVector.sqrMagnitude;

        // If the segment has zero length, just return one of the endpoints
        if (lineLengthSquared < 0.0001f)
        {
            return lineStart;
        }

        // Get the vector from lineStart to the target point
        Vector3 pointVector = point - lineStart;

        // Find the projection factor (t) using the dot product, normalized by the line's squared length
        float t = Vector3.Dot(pointVector, lineVector) / lineLengthSquared;

        // Clamp t between 0 and 1 so it stays strictly on the line segment
        t = Mathf.Clamp01(t);

        // Return the final projected point on the segment
        return lineStart + (lineVector * t);
    }
}