using UnityEngine;

using System;

public class DayNightCycle : MonoBehaviour
{
    [SerializeField] private float startDayLengthS = 30;

    public event Action DayStart;
    public event Action NightStart;

    public bool isNight { get; private set; }
    public float dayLengthS { get; private set; }
    public bool timePaused { get; private set; }

    private float timeRemainingS_ = 0;

    public float timeRemainingS
    {
        get
        {
            if (isNight) return 0;  
            return timeRemainingS_;
        }
        set
        {
            timeRemainingS_ = value;
            if (timeRemainingS <= 0) {
                SetNight();
            }
        }
    }

    void Start()
    {
        dayLengthS = startDayLengthS;
        timeRemainingS = dayLengthS;
        isNight = false;
        timePaused = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isNight && !timePaused) {
            timeRemainingS -= Time.deltaTime;
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

    public void SetNight()
    {
        if (isNight) return;
        isNight = true;
        NightStart?.Invoke();
    }

    public void SetDay()
    {
        if (!isNight) return;
        isNight = false;
        timeRemainingS = dayLengthS;
        DayStart?.Invoke();
    }
}
