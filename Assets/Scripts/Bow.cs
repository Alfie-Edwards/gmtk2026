using UnityEngine;

public class Bow : MonoBehaviour
{
    [Header("Arrow Settings")]
    public GameObject arrowPrefab;
    public Transform spawnPoint;
    public Collider playerCollider; // Assign the player's collider in the inspector

    public void Fire(Vector3 fireDirection)
    {
        if (arrowPrefab == null)
        {
            Debug.LogWarning("Arrow prefab is not assigned in the Bow!");
            return;
        }

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject arrowObj = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);

        Arrow arrow = arrowObj.GetComponent<Arrow>();
        if (arrow != null)
        {
            // Pass the player's collider so it gets ignored
            arrow.Initialize(fireDirection, playerCollider);
        }
    }
}