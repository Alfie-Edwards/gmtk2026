using UnityEngine;

public class CrackedWall : MonoBehaviour
{
    [SerializeField] private GameObject intactModel;
    [SerializeField] private GameObject brokenModel;

    private bool isBroken = false;

    private void Start()
    {
        if (intactModel != null)
        {
            intactModel.SetActive(true);
        }

        if (brokenModel != null)
        {
            brokenModel.SetActive(false);
        }
    }

    public void Break()
    {
        if (isBroken) return;
        isBroken = true;

        if (intactModel != null)
        {
            intactModel.SetActive(false);
        }

        if (brokenModel != null)
        {
            brokenModel.SetActive(true);
        }
    }
}