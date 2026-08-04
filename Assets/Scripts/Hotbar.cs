using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using System;
using System.Collections.Generic;

[RequireComponent(typeof(Transform))]
public class Hotbar : MonoBehaviour
{
    [System.Serializable]
    public struct ItemSprite {
        public ItemType type;
        public Sprite sprite;
    }

    [SerializeField] public bool enableSelection;
    [SerializeField] public List<ItemSprite> sprites;
    [SerializeField] public GameObject itemTemplate;
    [SerializeField] public bool show1 = false;

    private Bag bag_;
    private int iSelected_;
    private int iSelected
    {
        get => iSelected_;
        set
        {
            if (iSelected == value) return;
            iSelected_ = value;
            SelectedChanged?.Invoke(Selected);
        }
    }
    private Dictionary<ItemType, int> limits = new Dictionary<ItemType, int>();

    public Bag bag {
        get => bag_;
        set {
            bag_ = value;
            if (bag_ != null) {
                iSelected = 0;
                bag.OnContentsChanged += Refresh;
                Refresh();
            }
        }
    }

    public event Action<ItemType> SelectedChanged;

    void Start() {
        iSelected_ = 0;
    }

    public void SetLimit(ItemType type, int limit) {
        limits[type] = limit;
    }

    public ItemType Selected
    {
        get => bag?.AtIndex(iSelected) ?? ItemType.None;
    }

    public void Update()
    {
        if (bag == null || !enableSelection) return;
        float scrollY = Mouse.current.scroll.ReadValue().y;
        if (scrollY > 0f || Keyboard.current.sKey.wasPressedThisFrame)
        {
            iSelected = Mathf.Clamp(iSelected + 1, 0, bag.numUniqueItems - 1);
            Refresh();
        }
        else if (scrollY < 0f || (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame))
        {
            iSelected = Mathf.Clamp(iSelected - 1, 0, bag.numUniqueItems - 1);
            Refresh();
        }
    }

    private Sprite GetSprite(ItemType type)
    {
        foreach (ItemSprite x in sprites)
        {
            if (x.type == type)
            {
                return x.sprite;
            }
        }
        return null;
    }

    private void Refresh()
    {
        if (bag == null) return;

        if (enableSelection) iSelected = iSelected = Mathf.Clamp(iSelected, 0, bag.numUniqueItems - 1);

        // Clear
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        int i = 0;
        foreach (BagItem item in bag)
        {
            GameObject uiItem = Instantiate(itemTemplate, transform);

            if (enableSelection && i == iSelected)
            {
                if (uiItem.GetComponent<RectTransform>() is RectTransform x)
                {
                    x.sizeDelta *= 1.3f;
                }
            }

            // Set the icon
            Image icon = uiItem.GetComponentsInChildren<Image>()[1];
            icon.sprite = GetSprite(item.type);
            icon.SetAllDirty();
            if (icon.sprite == null)
            {
                icon.gameObject.SetActive(false);
            }

            // Set the amount text
            TextMeshProUGUI count = uiItem.GetComponentInChildren<TextMeshProUGUI>();
            if (show1 || item.count > 1 || bag_.persistItems)
            {
                if (limits.ContainsKey(item.type)) {
                    count.text = $"{item.count} / {limits[item.type]}";
                } else {
                    count.text = $"{item.count}";
                }
                count.gameObject.SetActive(true);
            }
            else
            {
                count.gameObject.SetActive(false);
            }
            i++;
        }
    }
}
