using System.Collections;
using UnityEngine;

public class Bridge : MonoBehaviour
{
    [SerializeField] private float fallDuration = 1f;
    [SerializeField] private Transform hingePoint; // The point/object in space to swing around
    [SerializeField] private Vector3 rotationAxis = Vector3.right; // The axis to swing on (e.g., X, Y, or Z)

    private bool hasFallen = false;

    public void Fall()
    {
        if (hasFallen) return;
        hasFallen = true;

        StartCoroutine(AnimateFall());
    }

    private IEnumerator AnimateFall()
    {
        // Fallback to self position if no hinge is assigned
        Vector3 pivot = hingePoint != null ? hingePoint.position : transform.position;

        float elapsed = 0f;
        float totalAngleRotated = 0f;
        float targetAngle = -90f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);

            // Quadratic ease-in acceleration (starts slow, speeds up)
            t = t * t;

            // Calculate how much angle to cover this specific frame based on the curve
            float targetTotalAngle = targetAngle * t;
            float angleStep = targetTotalAngle - totalAngleRotated;

            // Rotate around the external point in world space
            transform.RotateAround(pivot, rotationAxis, angleStep);

            totalAngleRotated = targetTotalAngle;
            yield return null;
        }

        // Ensure it lands precisely at the final -90 degrees
        transform.RotateAround(pivot, rotationAxis, targetAngle - totalAngleRotated);
    }
}