using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    public event Action<ItemType> OnItemAdded;

    private List<BagItem> bag = new List<BagItem>();

    public int numUniqueItems { get => bag.Count; }

    public bool persistItems = false;

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
        if (count == 0) return;
        int i = 0;
        while (i < bag.Count && bag[i].type != type) ++i;
        if (i == bag.Count) bag.Add(new BagItem(type, 0));
        bag[i].count += count;
        OnContentsChanged?.Invoke();
        OnItemAdded?.Invoke(type);
    }

    public void Remove(ItemType type, int count = 1)
    {
        if (count == 0) return;
        for (int i = 0; i != bag.Count; ++i)
        {
            if (bag[i].type == type)
            {
                bag[i].count -= count;
                if (bag[i].count <= 0) {
                    if (persistItems) {
                        bag[i].count = 0;
                    } else {
                        bag.RemoveAt(i);
                    }
                }
                OnContentsChanged?.Invoke();
                return;
            }
        }
    }

    public IEnumerator EmptyInto(Bag targetBag, float itemsPerSecond=5f)
    {
        float maxMatchesPerSecond = 10f;
        if (targetBag == null || itemsPerSecond <= 0) yield break;

        float batchSize = itemsPerSecond > maxMatchesPerSecond ? itemsPerSecond / maxMatchesPerSecond : 1f;
        float batchesPerSecond = MathF.Min(maxMatchesPerSecond, itemsPerSecond);
        int elapsedBatches = 0;
        float elapsed = 0;

        while (bag.Where(x => x.count > 0).FirstOrDefault() is BagItem currentItem)
        {
            elapsed += Time.deltaTime;
            int targetBatches = Mathf.FloorToInt(elapsed * batchesPerSecond);
            int numItemsToMove = Mathf.FloorToInt(targetBatches * batchSize) - Mathf.FloorToInt(elapsedBatches * batchSize);
            if (numItemsToMove >= currentItem.count)
            {
                numItemsToMove = currentItem.count;
                elapsedBatches = 0;
                elapsed = 0;
            }
            else
            {
                elapsedBatches = targetBatches;
            }
            Remove(currentItem.type, numItemsToMove);
            targetBag.Add(currentItem.type, numItemsToMove);
            yield return null;
        }
    }

    public void Empty()
    {
        while (bag.Count > 0)
        {
            BagItem currentItem = bag[0];
            Remove(currentItem.type, currentItem.count);
        }
    }

    public IEnumerator<BagItem> GetEnumerator() => bag.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => bag.GetEnumerator();
}