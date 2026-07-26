using UnityEngine;

public class Rock : MonoBehaviour
{
    public int coinCount = 500;
    public GameObject coinPrefab;

    public void Break()
    {
        if (coinPrefab != null)
        {
            for (int i = 0; i < coinCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle;
                Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

                Instantiate(coinPrefab, spawnPosition, Random.rotation);
            }
        }
        Destroy(gameObject);
    }
}
