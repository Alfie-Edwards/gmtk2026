using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System;

public class DayNightCycle : MonoBehaviour
{
    [SerializeField] private float startDayLengthS = 30;

    [Header("UI & Lighting References")]
    public TextMeshProUGUI timerText;         // Drag your UI Text here
    public Light sunLight;                    // Drag your directional light here

    [Header("Lighting Settings")]
    public float daySunAngle = 50f;           // Max height angle during the day
    public float dayMaxIntensity = 1f;        // Intensity during the day
    public float sunsetMinBrightness = 0.3f;   // Brightness at the end of the sunset phase
    public float nightBrightness = 0.1f;       // Brightness during true night
    public float compassHeading = 0f;         // Y rotation of the sun/sky direction
    public float sunriseTransitionDuration = 3f; // Configurable duration for the start day transition
    public float sunsetTransitionDuration = 15f;  // Configurable duration for the gradual sunset transition
    public float trueNightTransitionDuration = 3f; // Configurable duration for the quick drop to true night

    [Header("Smooth Color Cycle")]
    public Color dayColor = Color.white;
    public Color sunsetColor = new Color(1f, 0.5f, 0.2f); // Warm sunset hue
    public Color nightColor = new Color(0.2f, 0.3f, 0.5f);     // Cool blue night hue

    public event Action DayStart;
    public event Action NightStart;

    public bool isNight { get; private set; }
    public float dayLengthS { get; private set; }
    public bool timePaused { get; private set; }

    private float timeRemainingS_ = 0;
    private float sunriseTimer = 0f;          // Tracks the sunrise transition animation
    private bool isTransitioningDay = false;
    private float trueNightTimer = 0f;        // Tracks the quick fade to true night
    private bool isTransitioningTrueNight = false;

    public float timeRemainingS
    {
        get
        {
            if (isNight || isTransitioningTrueNight) return 0;
            return timeRemainingS_;
        }
        set
        {
            timeRemainingS_ = value;
            if (timeRemainingS <= 0 && !isNight && !isTransitioningTrueNight) {
                StartTrueNightTransition();
            }
        }
    }

    void Start()
    {
        dayLengthS = startDayLengthS;
        timeRemainingS = dayLengthS;
        isNight = true; // Start at night so it properly waits for SetDay() to trigger sunrise
        timePaused = true;

        // Initialize lighting to night settings on start
        if (sunLight != null)
        {
            sunLight.intensity = nightBrightness;
            sunLight.transform.rotation = Quaternion.Euler(0f, compassHeading, 0f);
            sunLight.color = nightColor;
        }
    }

    void Update()
    {
        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            if (isNight) SetDay();
            else UnpauseTime();
        }

        // 1. Time Progression & Phase Handling
        if (!isNight && !timePaused) {
            if (isTransitioningDay)
            {
                sunriseTimer += Time.deltaTime;
                if (sunriseTimer >= sunriseTransitionDuration)
                {
                    isTransitioningDay = false;
                }
            }
            else if (isTransitioningTrueNight)
            {
                trueNightTimer += Time.deltaTime;
                if (trueNightTimer >= trueNightTransitionDuration)
                {
                    isTransitioningTrueNight = false;
                    SetNight();
                }
            }
            else
            {
                timeRemainingS -= Time.deltaTime;
            }
        }

        // 2. UI Clock Management
        if (timerText != null)
        {
            bool showTimer = !timePaused && !isNight && !isTransitioningTrueNight;
            timerText.gameObject.SetActive(showTimer);

            if (showTimer)
            {
                int seconds = Mathf.CeilToInt(timeRemainingS);
                timerText.text = $"Time Left: {seconds}s";
            }
        }

        // 3. Lighting & Sun Animation Management
        if (sunLight != null)
        {
            if (isNight)
            {
                // Night time: Locked at true night settings
                sunLight.intensity = nightBrightness;
                sunLight.transform.rotation = Quaternion.Euler(0f, compassHeading, 0f);
                sunLight.color = nightColor;
            }
            else if (isTransitioningTrueNight)
            {
                // True Night Phase: Smoothly transitions from sunset brightness/color to true night brightness/color
                float t = Mathf.Clamp01(trueNightTimer / trueNightTransitionDuration);
                float currentAngle = Mathf.Lerp(0f, -10f, t);
                float currentIntensity = Mathf.Lerp(sunsetMinBrightness, nightBrightness, t);

                sunLight.transform.rotation = Quaternion.Euler(currentAngle, compassHeading, 0f);
                sunLight.intensity = currentIntensity;
                sunLight.color = Color.Lerp(sunsetColor, nightColor, t);
            }
            else if (isTransitioningDay)
            {
                // Sunrise Phase: Smoothly transitions directly from night color to day color
                float t = Mathf.Clamp01(sunriseTimer / sunriseTransitionDuration);
                float currentAngle = Mathf.Lerp(-daySunAngle, daySunAngle, t);
                float currentIntensity = Mathf.Lerp(nightBrightness, dayMaxIntensity, t);

                sunLight.transform.rotation = Quaternion.Euler(Mathf.Abs(currentAngle), compassHeading, 0f);
                sunLight.intensity = currentIntensity;
                sunLight.color = Color.Lerp(nightColor, dayColor, t);
            }
            else if (timeRemainingS <= sunsetTransitionDuration)
            {
                // Sunset Phase: Smoothly transitions from day color/intensity to sunset color/brightness
                float t = Mathf.InverseLerp(sunsetTransitionDuration, 0f, timeRemainingS);

                float currentAngle = Mathf.Lerp(daySunAngle, 0f, t);
                float currentIntensity = Mathf.Lerp(dayMaxIntensity, sunsetMinBrightness, t);

                sunLight.transform.rotation = Quaternion.Euler(currentAngle, compassHeading, 0f);
                sunLight.intensity = currentIntensity;
                sunLight.color = Color.Lerp(dayColor, sunsetColor, t);
            }
            else
            {
                // Normal daytime progression
                sunLight.transform.rotation = Quaternion.Euler(daySunAngle, compassHeading, 0f);
                sunLight.intensity = dayMaxIntensity;
                sunLight.color = dayColor;
            }
        }
    }

    public void IncreaseDayLength(float amountS)
    {
        dayLengthS += amountS;
        timeRemainingS += amountS;
    }

    public void PauseTime()
    {
        timePaused = true;
    }

    public void UnpauseTime()
    {
        timePaused = false;
    }

    public void StartTrueNightTransition()
    {
        if (isNight || isTransitioningTrueNight) return;
        isTransitioningTrueNight = true;
        trueNightTimer = 0f;
    }

    public void SetNight()
    {
        if (isNight) return;
        isNight = true;
        isTransitioningTrueNight = false;
        NightStart?.Invoke();
    }

    public void SetDay()
    {
        if (!isNight && !isTransitioningTrueNight) return;
        isNight = false;
        isTransitioningTrueNight = false;
        timeRemainingS = dayLengthS;
        sunriseTimer = 0f;
        isTransitioningDay = true;
        DayStart?.Invoke();
    }
}