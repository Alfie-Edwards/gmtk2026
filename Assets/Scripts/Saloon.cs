using TMPro;
using UnityEngine;

public class Saloon : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI saloonText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timings")]
    [SerializeField] private float fadeSpeed = 5f;

    private bool wasReuppedThisFrame = false;

    public string message
    {
        get => saloonText?.text ?? "";
        set
        {
            if (saloonText != null)
            {
                saloonText.text = value;
                saloonText.enabled = !string.IsNullOrEmpty(value);
            }
        }
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
    }
}