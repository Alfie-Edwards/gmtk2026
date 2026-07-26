using UnityEngine;

public class Target : MonoBehaviour
{
    [SerializeField] private Bridge targetBridge;

    public void Trigger()
    {
        if (targetBridge != null)
        {
            targetBridge.Fall();
        }
    }
}