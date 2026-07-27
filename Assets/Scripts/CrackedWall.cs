using UnityEngine;

public class CrackedWall : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;       
    [SerializeField] private MeshCollider meshCollider;
    [SerializeField] private Mesh intactMesh;       
    [SerializeField] private Mesh brokenMesh;       

    private bool isBroken = false;

    private void Start()
    {
        if (meshFilter != null && intactMesh != null)
        {
            meshFilter.sharedMesh = intactMesh;
        }

        if (meshCollider != null && intactMesh != null)
        {
            meshCollider.sharedMesh = intactMesh;
        }
    }

    public void Break()
    {
        if (isBroken) return;
        isBroken = true;

        if (meshFilter != null && brokenMesh != null)
        {
            meshFilter.sharedMesh = brokenMesh;
        }

        if (meshCollider != null && brokenMesh != null)
        {
            meshCollider.sharedMesh = brokenMesh;
        }
    }
}
