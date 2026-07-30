using UnityEngine;

public enum ItemType {
    RedKey,
    BlueKey,
    GreenKey,
    // Items
    Whip,
    Bow,
    BombBag,
    Pickaxe,
    // Pickups
    Gold,
    Whisky,
    Dynamite,
    Arrow,
    // Upgrades
    Seed,
    QuiverUpgrade,
    BombBagUpgrade,
    WhipUpgrade,
    Rooster,

    None,
}

public class Item : MonoBehaviour
{
    public ItemType type = ItemType.None;
    public int amount = 1;
    private float tStart;

    void Awake()
    {
        tStart = Time.time;
    }

    public float Age { get => Time.time - tStart; }

}
