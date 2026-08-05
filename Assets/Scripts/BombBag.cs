using FMODUnity;
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

        RuntimeManager.PlayOneShot("event:/SFX/Weapons/SFX_WhipCrack");
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject bombObj = Instantiate(bombPrefab, spawnPos, Quaternion.identity);
        bombObj.GetComponent<Bomb>()?.Initialize(throwDirection);
    }
}