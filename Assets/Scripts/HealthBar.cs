using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Enemy targetEnemy;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.3f;

    private float lastKnownHealth;
    private Coroutine fadeCoroutine;
    private bool hidden;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Start hidden immediately
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        hidden = true;
    }

    private void Start()
    {
        if (targetEnemy != null)
        {
            lastKnownHealth = targetEnemy.currentHealth;

            if (healthSlider != null)
            {
                healthSlider.maxValue = targetEnemy.maxHealth;
                healthSlider.value = targetEnemy.currentHealth;
            }
        }
    }

    private void Update()
    {
        if (targetEnemy == null)
        {
            if (!hidden)
            {
                healthSlider.value = 0;
                Hide();
            }
            return;
        }

        // Poll for health changes since the Enemy class doesn't have events
        if (!Mathf.Approximately(targetEnemy.currentHealth, lastKnownHealth))
        {
            float previousHealth = lastKnownHealth;
            lastKnownHealth = targetEnemy.currentHealth;

            if (healthSlider != null)
            {
                healthSlider.maxValue = targetEnemy.maxHealth;
                healthSlider.value = lastKnownHealth;
            }

            // If health decreased, show the bar (e.g. when taking damage)
            if (lastKnownHealth < previousHealth)
            {
                Show();
            }

            // If health has emptied, auto-hide
            if (lastKnownHealth <= 0f)
            {
                Hide();
            }
        }
    }

    public void Show()
    {
        if (!hidden) return;
        hidden = false;
        gameObject.SetActive(true);
        FadeTo(1f);
    }

    public void Hide()
    {
        if (hidden) return;
        hidden = true;
        FadeTo(0f);
    }

    private void FadeTo(float targetAlpha)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        canvasGroup.blocksRaycasts = targetAlpha > 0f;
        canvasGroup.interactable = targetAlpha > 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}