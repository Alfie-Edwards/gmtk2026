using UnityEngine;

public class CrackedWall : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;       
    [SerializeField] private MeshCollider meshCollider;
    [SerializeField] private BoxCollider boxCollider;
    [SerializeField] private Mesh intactMesh;       
    [SerializeField] private Mesh brokenMesh;       

    private bool isBroken = false;

    private void Start()
    {
        boxCollider.enabled = true;
        if (meshFilter != null && intactMesh != null)
        {
            meshFilter.sharedMesh = intactMesh;
        }

        if (meshCollider != null && intactMesh != null)
        {
            meshCollider.sharedMesh = intactMesh;
        }
        meshCollider.enabled = false;
    }

    public void Break()
    {
        if (isBroken) return;
        isBroken = true;
        meshCollider.enabled = true;

        if (meshFilter != null && brokenMesh != null)
        {
            meshFilter.sharedMesh = brokenMesh;
        }

        if (meshCollider != null && brokenMesh != null)
        {
            meshCollider.sharedMesh = brokenMesh;
        }
        boxCollider.enabled = false;
    }
}
