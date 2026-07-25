using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using System.Collections.Generic;

[RequireComponent(typeof(Transform))]
public class Hotbar : MonoBehaviour
{
    [System.Serializable]
    public struct ItemSprite {
        public ItemType type;
        public Sprite sprite;
    }

    [SerializeField] public List<ItemSprite> sprites;
    [SerializeField] public GameObject itemTemplate;
    
    private Bag bag_;
    private Transform container;
    private int iSelected;

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
    
    void Start() {
        container = GetComponent<Transform>();
    }

    public ItemType Selected
    {
        get => bag?.AtIndex(iSelected) ?? ItemType.None;
    }

    public void Update()
    {
        if (bag == null) return;
        float scrollY = Mouse.current.scroll.ReadValue().y;

        if (scrollY > 0f)
        {
            iSelected = Mathf.Clamp(0, bag.numUniqueItems - 1, iSelected + 1);
        }
        else if (scrollY < 0f)
        {
            iSelected = Mathf.Clamp(0, bag.numUniqueItems - 1, iSelected - 1);
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

        iSelected = Mathf.Clamp(0, bag.numUniqueItems - 1, iSelected);

        // Clear
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        foreach (BagItem item in bag)
        {
            GameObject uiItem = Instantiate(itemTemplate, container);

            // Set the icon
            Image icon = uiItem.GetComponentInChildren<Image>();
            Debug.Log(icon);
            Debug.Log(GetSprite(item.type));
            icon.sprite = GetSprite(item.type);
            icon.SetAllDirty();

            // Set the amount text
            TextMeshProUGUI count = uiItem.GetComponentInChildren<TextMeshProUGUI>();
            if (item.count > 1)
            {
                count.text = $"x{item.count}";
                count.gameObject.SetActive(true);
            }
            else
            {
                count.gameObject.SetActive(false);
            }
        }
    }
}
