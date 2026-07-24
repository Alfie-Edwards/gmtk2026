using UnityEngine;

public enum ItemType {
    RedKey,
    BlueKey,
    GreenKey,
    Unknown,
}

public class Item : MonoBehaviour
{
    public ItemType type = ItemType.Unknown;

}
