using FMODUnity;
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
        Reset();

        if (hitPoint == null)
        {
            hitPoint = transform;
        }
    }

    public void Reset()
    {
        StopAllCoroutines();
        transform.localPosition = initialLocalPos;
        transform.localRotation = initialLocalRot;
        isSwinging = false;
    }

    public void Swing()
    {
        if (isSwinging) return;
        StartCoroutine(PerformSwing());
    }

    private IEnumerator PerformSwing()
    {
        RuntimeManager.PlayOneShot("event:/SFX/Weapons/SFX_WhipCrack");
        isSwinging = true;
        float elapsed = 0f;
        float forwardDuration = swingDuration * 0.3f;
        float backwardDuration = swingDuration - forwardDuration;

        Vector3 targetPos = initialLocalPos + swingOffset;
        Quaternion targetRot = initialLocalRot * Quaternion.Euler(swingRotationOffset);

        while (elapsed < forwardDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / forwardDuration);

            transform.localPosition = Vector3.Lerp(initialLocalPos, targetPos, t);
            transform.localRotation = Quaternion.Slerp(initialLocalRot, targetRot, t);
            yield return null;
        }

        CheckHits();

        while (elapsed < backwardDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / backwardDuration);

            transform.localPosition = Vector3.Lerp(targetPos, initialLocalPos, t);
            transform.localRotation = Quaternion.Slerp(targetRot, initialLocalRot, t);
            yield return null;
        }

        transform.localPosition = initialLocalPos;
        transform.localRotation = initialLocalRot;
        isSwinging = false;
    }

    private void CheckHits()
    {
        Vector3 center = hitPoint.position;
        Collider[] hits = Physics.OverlapSphere(center, hitRadius);

        if (hits.Length > 0)
        {
            RuntimeManager.PlayOneShot("event:/SFX/Player/SFX_PlayerGetsHit");
        }

        foreach (Collider hit in hits)
        {
            if (hit.GetComponent<Rock>() is Rock rock)
            {
                rock.Break();
            }
            else if (hit.GetComponent<CrackedWall>() is CrackedWall wall)
            {
                wall.Break();
            }
            else if (hit.GetComponent<Enemy>() is Enemy enemy)
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