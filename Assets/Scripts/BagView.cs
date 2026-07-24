using UnityEngine;
using UnityEngine.UI;
using TMPro;

using System.Collections.Generic;

[RequireComponent(typeof(Transform))]
public class BagView : MonoBehaviour
{
    [System.Serializable]
    public struct ItemSprite {
        public ItemType type;
        public Sprite sprite;
    }

    [SerializeField] public List<ItemSprite> sprites;
    [SerializeField] public GameObject itemTemplate;
    public Bag bag;

    private Transform container;

    public void Start()
    {
        container = GetComponent<Transform>();
        bag.OnContentsChanged += Refresh;
        Refresh();
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
