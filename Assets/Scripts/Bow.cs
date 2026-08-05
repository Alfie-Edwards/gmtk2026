using FMODUnity;
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

        RuntimeManager.PlayOneShot("event:/SFX/Weapons/SFX_WhipCrack");
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject arrowObj = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
        arrowObj.GetComponent<Arrow>()?.Initialize(fireDirection, playerCollider);
    }
}