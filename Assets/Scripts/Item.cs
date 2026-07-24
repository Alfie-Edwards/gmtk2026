using UnityEngine;

public enum ItemType {
    RedKey,
    Unknown,
}

public class Item : MonoBehaviour
{
    public ItemType type = ItemType.Unknown;

}
