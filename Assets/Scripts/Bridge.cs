using System.Collections;
using UnityEngine;

public class Bridge : MonoBehaviour
{
    [SerializeField] private float fallDuration = 1f;
    [SerializeField] private Transform hingePoint;

    private bool hasFallen = false;

    public void Fall()
    {
        if (hasFallen) return;
        hasFallen = true;

        StartCoroutine(AnimateFall());
    }

    private IEnumerator AnimateFall()
    {
        Transform targetTransform = hingePoint != null ? hingePoint : transform;

        Quaternion startRotation = targetTransform.localRotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(-90f, 0f, 0f);

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);

            // Quadratic ease-in: starts slow and accelerates over time like gravity
            t = t * t;

            targetTransform.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
            yield return null;
        }

        targetTransform.localRotation = endRotation;
    }
}