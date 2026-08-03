using System.Collections;
using UnityEngine;

public class ArenaFloor : MonoBehaviour
{
    private bool lowered;

    void Start()
    {
        lowered = false;
    }

    void Update()
    {
        if (!lowered && FindAnyObjectByType<EnemyRock>() == null)
        {
            Lower();
        }
    }

    public void Lower()
    {
        lowered = true;
        StartCoroutine(SmoothLowerRoutine());
    }

    private IEnumerator SmoothLowerRoutine()
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition - new Vector3(0, 7f, 0);
        float duration = 10f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // Interpolate position
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            yield return null;
        }

        transform.position = targetPosition;
    }
}