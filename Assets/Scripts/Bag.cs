using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

public class BagItem {
    public ItemType type;
    public int count;

    public BagItem(ItemType type, int count)
    {
        this.type = type;
        this.count = count;
    }
}

public class Bag : IEnumerable<BagItem>
{
    public event Action OnContentsChanged;

    private List<BagItem> bag = new List<BagItem>();

    public int numUniqueItems { get => bag.Count; }

    public ItemType AtIndex(int i)
    {
        if (i < 0 || i >= bag.Count ) {
            return ItemType.None;
        }
        return bag[i].type;
    }

    public int Amount(ItemType type)
    {
        foreach (BagItem item in bag)
        {
            if (item.type == type) {
                return item.count;
            }
        }
        return 0;
    }

    public bool Has(ItemType type) => Amount(type) > 0;

    public void Add(ItemType type, int count = 1)
    {
        int i = 0;
        while (i < bag.Count && bag[i].type != type) ++i;
        if (i == bag.Count) bag.Add(new BagItem(type, 0));
        bag[i].count += count;
        Debug.Log($"Added item to bag {type} x {count} ({bag[i].count} total)");
        OnContentsChanged?.Invoke();
    }

    public void Remove(ItemType type, int count = 1)
    {
        for (int i = 0; i != bag.Count; ++i)
        {
            if (bag[i].type == type)
            {
                bag[i].count -= count;
                Debug.Log($"Removed item from bag {type} x {count} ({bag[i].count} remaining)");
                if (bag[i].count <= 0) {
                    bag.RemoveAt(i);
                }
                OnContentsChanged?.Invoke();
                return;
            }
        }
    }

    public IEnumerator<BagItem> GetEnumerator() => bag.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => bag.GetEnumerator();
}
