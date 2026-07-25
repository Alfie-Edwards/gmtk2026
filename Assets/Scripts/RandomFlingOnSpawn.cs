using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RandomFlingOnSpawn : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float radius = 2.0f;

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.value * radius;
        Vector3 randomHorizontalDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        
        float speed = Mathf.Sqrt(distance * Mathf.Abs(Physics.gravity.y)) * 1.2f;
        float upwardBias = 0.8f;
        
        Vector3 upwardDiagonalDir = (randomHorizontalDir * (distance / radius) + Vector3.up * upwardBias).normalized;
        rb.linearVelocity = upwardDiagonalDir * speed;
    }
}   