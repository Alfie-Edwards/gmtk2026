using UnityEngine;
using System.Collections;

public class Pickaxe : MonoBehaviour
{
    [Header("Animation Settings")]
    public float swingDuration = 0.4f;
    public Vector3 swingOffset = new Vector3(0f, -0.5f, 1.5f);
    public Vector3 swingRotationOffset = new Vector3(45f, 0f, 0f);

    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private bool isSwinging = false;

    [Header("Mining & Combat Settings")]
    public float hitRadius = 1.0f;
    [SerializeField] private Transform hitPoint; // Position where hits are checked (defaults to pickaxe transform if unassigned)

    void Start()
    {
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;

        if (hitPoint == null)
        {
            hitPoint = transform;
        }
    }

    public void Swing()
    {
        if (isSwinging) return;
        StartCoroutine(PerformSwing());
    }

    private IEnumerator PerformSwing()
    {
        isSwinging = true;
        float elapsed = 0f;
        float halfDuration = swingDuration / 2f;

        Vector3 targetPos = initialLocalPos + swingOffset;
        Quaternion targetRot = initialLocalRot * Quaternion.Euler(swingRotationOffset);

        // 1. Swing forward / down
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);

            transform.localPosition = Vector3.Lerp(initialLocalPos, targetPos, t);
            transform.localRotation = Quaternion.Slerp(initialLocalRot, targetRot, t);
            yield return null;
        }

        // 2. Check for targets at the peak of the swing
        CheckHits();

        elapsed = 0f;

        // 3. Return to initial position
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);

            transform.localPosition = Vector3.Lerp(targetPos, initialLocalPos, t);
            transform.localRotation = Quaternion.Slerp(targetRot, initialLocalRot, t);
            yield return null;
        }

        transform.localPosition = initialLocalPos;
        transform.localRotation = initialLocalRot;
        isSwinging = false;
    }

    void CheckHits()
    {
        Vector3 center = hitPoint.position;
        Collider[] hits = Physics.OverlapSphere(center, hitRadius);

        foreach (Collider hit in hits)
        {
            // Check for Rock component
            Rock rock = hit.GetComponent<Rock>();
            if (rock != null)
            {
                rock.Break();
                continue;
            }

            // Check for CrackedWall component
            CrackedWall wall = hit.GetComponent<CrackedWall>();
            if (wall != null)
            {
                wall.Break();
                continue;
            }

            // Check for general Enemy to deal small damage after break
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                Vector3 hitDirection = (hit.transform.position - transform.position).normalized;
                enemy.TakeDamage(10f, hitDirection);
            }
        }
    }

    // Optional: Draw the hit radius in the Unity Editor for easy visualization
    void OnDrawGizmosSelected()
    {
        Transform checkTransform = hitPoint != null ? hitPoint : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(checkTransform.position, hitRadius);
    }
}