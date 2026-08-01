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
    public CanvasGroup sunriseFadeCanvasGroup; // Drag a UI Panel covering the screen with a CanvasGroup component here

    [Header("Clean Orbit Controls")]
    [Range(0f, 180f)]
    public float dayStartAngle = 0f;          // 1. Angle the sun settles at after sunrise
    [Range(0f, 180f)]
    public float dayEndAngle = 90f;           // 2. Angle the sun reaches when timer hits zero
    public float hoopTiltZ = 0f;              // 3. Rotate the hoop sideways (Z-axis roll) about its path plane
    public float compassHeading = 0f;         // 4. Rotate the whole system about the Y-axis (compass heading)

    [Header("Midday Slice & Ambient Environment")]
    [Range(0f, 180f)]
    public float middaySliceWidth = 30f;      // Width of the peak midday zone centered between dayStartAngle and dayEndAngle
    
    [Header("Lighting Intensity & Ambient Colors")]
    public float maxSunIntensity = 1f;        // Peak sun intensity during the day
    public Color middayAmbientColor = new Color(0.8f, 0.9f, 1f);  // Ambient color during peak midday
    public Color sunsetAmbientColor = new Color(1f, 0.6f, 0.3f);  // Ambient color at 0f and 180f horizons
    public Color nightAmbientColor = new Color(0.05f, 0.05f, 0.1f); // Ambient color during true night

    public float sunriseTransitionDuration = 3f; // Configurable duration for the start day transition
    public float sunriseFadeToBlackDuration = 1.5f; // Duration of the absolute black hold/fade at sunrise start
    public float trueNightTransitionDuration = 3f; // Configurable duration for the quick drop to true night
    [SerializeField] private GameObject wildernessPrefab;
    [SerializeField] private GameObject currentWilderness;

    [Header("Smooth Temperature Cycle (Kelvin)")]
    public float dayTemperature = 6500f;      // Peak Daylight Kelvin (Cool/Neutral) inside the midday slice
    public float sunsetTemperature = 3000f;   // Sunset/Sunrise Kelvin at 0f and 180f

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
        get => timeRemainingS_;
        set
        {
            timeRemainingS_ = value;
            if (timeRemainingS_ <= 0 && !isNight && !isTransitioningTrueNight) {
                SetNight();
                StartTrueNightTransition();
            }
        }
    }

    public void ResetWilderness()
    {
        if (currentWilderness != null && wildernessPrefab != null)
        {
            Transform t = currentWilderness.transform;
            Vector3 pos = t.position;
            Quaternion rot = t.rotation;
            Transform parent = t.parent;

            Destroy(currentWilderness);
            currentWilderness = Instantiate(wildernessPrefab, pos, rot, parent);
        }

        // Destroy all drops from the ground.
        foreach (Item item in FindObjectsByType<Item>())
        {
            if (item.transform.position.z >= 0 && (item.type == ItemType.Gold || item.type == ItemType.Whisky))
            {
                Destroy(item.gameObject);
            }
        }
    }

    void Start()
    {
        dayLengthS = startDayLengthS;
        timeRemainingS = dayLengthS;
        isNight = false;
        timePaused = true;

        if (sunLight != null)
        {
            sunLight.useColorTemperature = true;
            sunLight.colorTemperature = GetTemperatureForAngle(dayStartAngle);
            sunLight.intensity = maxSunIntensity;
            
            Quaternion compassRot = Quaternion.Euler(0f, compassHeading, 0f);
            Quaternion hoopTiltRot = Quaternion.Euler(0f, 0f, hoopTiltZ);
            Quaternion orbitalRot = Quaternion.Euler(dayStartAngle, 0f, 0f);
            
            sunLight.transform.rotation = compassRot * hoopTiltRot * orbitalRot;
        }

        RenderSettings.ambientLight = GetAmbientColorForAngle(dayStartAngle);

        if (sunriseFadeCanvasGroup != null)
        {
            sunriseFadeCanvasGroup.alpha = 0f;
            sunriseFadeCanvasGroup.blocksRaycasts = false;
        }
    }

    void Update()
    {
        if (!isNight && !timePaused && !isTransitioningDay && !isTransitioningTrueNight) {
            timeRemainingS -= Time.deltaTime;
        }

        if (isTransitioningDay)
        {
            sunriseTimer += Time.deltaTime;
            
            if (sunriseFadeCanvasGroup != null)
            {
                if (sunriseTimer <= sunriseFadeToBlackDuration)
                {
                    float fadeProgress = Mathf.Clamp01(sunriseTimer / sunriseFadeToBlackDuration);
                    sunriseFadeCanvasGroup.alpha = fadeProgress;
                    sunriseFadeCanvasGroup.blocksRaycasts = true;
                }
                else
                {
                    float clearProgress = Mathf.Clamp01((sunriseTimer - sunriseFadeToBlackDuration) / (sunriseTransitionDuration - sunriseFadeToBlackDuration));
                    sunriseFadeCanvasGroup.alpha = 1f - clearProgress;
                    if (clearProgress >= 1f)
                    {
                        sunriseFadeCanvasGroup.blocksRaycasts = false;
                    }
                }
            }

            if (sunriseTimer >= sunriseTransitionDuration)
            {
                isTransitioningDay = false;
                if (sunriseFadeCanvasGroup != null)
                {
                    sunriseFadeCanvasGroup.alpha = 0f;
                    sunriseFadeCanvasGroup.blocksRaycasts = false;
                }
            }
        }

        if (isTransitioningTrueNight)
        {
            trueNightTimer += Time.deltaTime;
            if (trueNightTimer >= trueNightTransitionDuration)
            {
                isTransitioningTrueNight = false;
            }
        }

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

        if (sunLight != null)
        {
            sunLight.useColorTemperature = true;

            Quaternion compassRot = Quaternion.Euler(0f, compassHeading, 0f);
            Quaternion hoopTiltRot = Quaternion.Euler(0f, 0f, hoopTiltZ);

            if (isTransitioningTrueNight)
            {
                float t = Mathf.Clamp01(trueNightTimer / trueNightTransitionDuration);
                float currentAngle = Mathf.Lerp(dayEndAngle, 190f, t);
                
                sunLight.colorTemperature = Mathf.Lerp(GetTemperatureForAngle(dayEndAngle), sunsetTemperature, t);
                sunLight.intensity = Mathf.Lerp(maxSunIntensity, 0f, t);
                RenderSettings.ambientLight = Color.Lerp(GetAmbientColorForAngle(dayEndAngle), nightAmbientColor, t);

                Quaternion orbitalRot = Quaternion.Euler(currentAngle, 0f, 0f);
                sunLight.transform.rotation = compassRot * hoopTiltRot * orbitalRot;
            }
            else if (isNight)
            {
                sunLight.colorTemperature = sunsetTemperature;
                sunLight.intensity = 0f;
                RenderSettings.ambientLight = nightAmbientColor;
                Quaternion orbitalRot = Quaternion.Euler(190f, 0f, 0f);
                sunLight.transform.rotation = compassRot * hoopTiltRot * orbitalRot;
            }
            else if (isTransitioningDay)
            {
                float t = Mathf.Clamp01(sunriseTimer / sunriseTransitionDuration);
                float currentAngle = Mathf.Lerp(0f, dayStartAngle, t); 
                
                sunLight.colorTemperature = GetTemperatureForAngle(currentAngle);
                sunLight.intensity = Mathf.Lerp(0f, maxSunIntensity, t);
                RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, GetAmbientColorForAngle(currentAngle), t);

                Quaternion orbitalRot = Quaternion.Euler(currentAngle, 0f, 0f);
                sunLight.transform.rotation = compassRot * hoopTiltRot * orbitalRot;
            }
            else if (timePaused)
            {
                sunLight.colorTemperature = GetTemperatureForAngle(dayStartAngle);
                sunLight.intensity = maxSunIntensity;
                RenderSettings.ambientLight = GetAmbientColorForAngle(dayStartAngle);

                Quaternion orbitalRot = Quaternion.Euler(dayStartAngle, 0f, 0f);
                sunLight.transform.rotation = compassRot * hoopTiltRot * orbitalRot;
            }
            else
            {
                float dayProgress = Mathf.InverseLerp(dayLengthS, 0f, timeRemainingS);
                float currentAngle = Mathf.Lerp(dayStartAngle, dayEndAngle, dayProgress);

                sunLight.colorTemperature = GetTemperatureForAngle(currentAngle);
                sunLight.intensity = maxSunIntensity;
                RenderSettings.ambientLight = GetAmbientColorForAngle(currentAngle);

                Quaternion orbitalRot = Quaternion.Euler(currentAngle, 0f, 0f);
                sunLight.transform.rotation = compassRot * hoopTiltRot * orbitalRot;
            }
        }
    }

    private float GetTemperatureForAngle(float angle)
    {
        float middayCenter = (dayStartAngle + dayEndAngle) * 0.5f;
        float halfSlice = middaySliceWidth * 0.5f;
        float sliceMin = middayCenter - halfSlice;
        float sliceMax = middayCenter + halfSlice;

        if (angle >= sliceMin && angle <= sliceMax)
        {
            return dayTemperature;
        }
        else if (angle < sliceMin)
        {
            return Mathf.Lerp(sunsetTemperature, dayTemperature, Mathf.InverseLerp(0f, sliceMin, angle));
        }
        else
        {
            return Mathf.Lerp(dayTemperature, sunsetTemperature, Mathf.InverseLerp(sliceMax, 180f, angle));
        }
    }

    private Color GetAmbientColorForAngle(float angle)
    {
        float middayCenter = (dayStartAngle + dayEndAngle) * 0.5f;
        float halfSlice = middaySliceWidth * 0.5f;
        float sliceMin = middayCenter - halfSlice;
        float sliceMax = middayCenter + halfSlice;

        if (angle >= sliceMin && angle <= sliceMax)
        {
            return middayAmbientColor;
        }
        else if (angle < sliceMin)
        {
            return Color.Lerp(sunsetAmbientColor, middayAmbientColor, Mathf.InverseLerp(0f, sliceMin, angle));
        }
        else
        {
            return Color.Lerp(middayAmbientColor, sunsetAmbientColor, Mathf.InverseLerp(sliceMax, 180f, angle));
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
        if (isTransitioningTrueNight) return;
        isTransitioningTrueNight = true;
        trueNightTimer = 0f;
    }

    public void SetNight()
    {
        if (isNight) return;
        isNight = true;
        NightStart?.Invoke();
    }

    public void SetDay()
    {
        if (!isNight) return;
        foreach (Enemy enemy in FindObjectsByType<Enemy>())
        {
            Destroy(enemy.gameObject);
        }
        ResetWilderness();
        isNight = false;
        timeRemainingS = dayLengthS;
        sunriseTimer = 0f;
        isTransitioningDay = true;
        timePaused = true; 

        if (sunLight != null)
        {
            sunLight.useColorTemperature = true;
            Quaternion compassRot = Quaternion.Euler(0f, compassHeading, 0f);
            Quaternion hoopTiltRot = Quaternion.Euler(0f, 0f, hoopTiltZ);
            Quaternion startRot = Quaternion.Euler(0f, 0f, 0f); 
            sunLight.transform.rotation = compassRot * hoopTiltRot * startRot;

            sunLight.colorTemperature = sunsetTemperature;
            sunLight.intensity = 0f;
        }

        RenderSettings.ambientLight = nightAmbientColor;

        DayStart?.Invoke();
    }
}