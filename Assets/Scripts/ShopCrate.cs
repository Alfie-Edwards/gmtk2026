using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopCrate : MonoBehaviour
{
    [Header("UI References")]
    [field: SerializeField] public ItemType itemType { get; private set; }
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image coinImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timings")]
    [SerializeField] private float fadeSpeed = 5f;

    // private static readonly Dictionary<ItemType, int> INTIIAL_PRICES = new Dictionary<ItemType, int>
    // {
    //     { ItemType.Seed, 10 },
    //     { ItemType.Arrow, 10 },
    //     { ItemType.Dynamite, 50 },
    //     { ItemType.WhipUpgrade, 50 },
    //     { ItemType.QuiverUpgrade, 50 },
    //     { ItemType.BombBagUpgrade, 50 },
    //     { ItemType.Rooster, 50 },
    // };


    private bool wasReuppedThisFrame = false;

    public string title
    {
        get => titleText?.text ?? "";
        set
        {
            if (titleText != null)
            {
                titleText.text = value;
                titleText.enabled = !string.IsNullOrEmpty(value);
            }
        }
    }

    public string description
    {
        get => descriptionText?.text ?? "";
        set
        {
            if (descriptionText != null)
            {
                descriptionText.text = value;
                descriptionText.enabled = !string.IsNullOrEmpty(value);
            }
        }
    }

    public int cost
    {
        get => int.TryParse(costText?.text, out int result) ? result : 0;
        set
        {
            if (costText != null)
            {
                costText.text = value.ToString();
                costText.enabled = true;
            }
        }
    }

    public void SetSoldOut() {
        description = "Sold out...";
        cost = int.MaxValue;
        coinImage.gameObject.SetActive(false);
    }

    void Awake()
    {
        SetAlpha(0f);
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    void Update()
    {
        float targetAlpha = wasReuppedThisFrame ? 1f : 0f;

        if (canvasGroup != null)
        {
            float newAlpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
            SetAlpha(newAlpha);

            bool isVisible = newAlpha > 0.01f;
            canvasGroup.blocksRaycasts = isVisible;
            canvasGroup.interactable = isVisible;
        }

        wasReuppedThisFrame = false;
    }

    public void Show()
    {
        wasReuppedThisFrame = true;
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }

        if (backgroundImage != null)
        {
            Color color = backgroundImage.color;
            color.a = alpha;
            backgroundImage.color = color;
        }
    }
}