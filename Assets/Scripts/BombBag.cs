using UnityEngine;

public class BombBag : MonoBehaviour
{
    [Header("Bomb Settings")]
    public GameObject bombPrefab;
    public Transform spawnPoint; // Optional: where the bomb spawns from (defaults to bag position if unassigned)

    public void ThrowBomb(Vector3 throwDirection)
    {
        if (bombPrefab == null)
        {
            Debug.LogWarning("Bomb prefab is not assigned in the BombBag!");
            return;
        }

        // Determine spawn position
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;

        // Instantiate the bomb
        GameObject bombObj = Instantiate(bombPrefab, spawnPos, Quaternion.identity);

        // Get the Bomb component and initialize its throw direction
        Bomb bomb = bombObj.GetComponent<Bomb>();
        if (bomb != null)
        {
            bomb.Initialize(throwDirection);
        }
    }
}