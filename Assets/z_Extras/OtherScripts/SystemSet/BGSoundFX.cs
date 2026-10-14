using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGSoundFX : MonoBehaviour
{
    [Header("Reference to Global Time System")]
    public GlobalTimeSystem globalTimeSystem;

    [Header("Reference to Weather System")]
    public WeatherCycleSystem weatherSystem;

    [Header("Ambient Clips for Segments")]
    public AudioClip dawnClip;
    public AudioClip earlyMorningClip;
    public AudioClip morningClip;
    public AudioClip middayClip;
    public AudioClip afternoonClip;
    public AudioClip lateAfternoonClip;
    public AudioClip eveningClip;
    public AudioClip nightClip;
    public AudioClip midnightClip;

    [Header("Weather Clips (no Sunny/Clear override)")]
    public AudioClip rainyClip;
    public AudioClip thunderstormClip;
    public AudioClip blizzardClip;
    public AudioClip windyClip;
    public AudioClip snowyClip;
    public AudioClip typhoonClip;

    [Header("Special Clips")]
    public AudioClip chickenCrowClip;
    public AudioClip churchBellClip;

    private AudioSource ambientSource;
    private AudioSource chickenSource;
    private AudioSource bellSource;

    private string lastSegment;
    private string lastWeather;

    [Header("Fade Settings")]
    public float fadeDuration = 2f;

    [Header("Chicken Crow Settings")]
    public float crowInterval = 180f;
    private float crowTimer;

    private void Awake()
    {
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.loop = true;
        ambientSource.volume = 1f;

        chickenSource = gameObject.AddComponent<AudioSource>();
        chickenSource.loop = false;
        chickenSource.volume = 0.3f;

        bellSource = gameObject.AddComponent<AudioSource>();
        bellSource.loop = false;
        bellSource.volume = 5f;
    }

    private void Update()
    {
        if (globalTimeSystem == null || weatherSystem == null) return;

        string currentSegment = globalTimeSystem.GetCurrentDailySegment();
        string currentWeather = weatherSystem.currentWeather;

        int hour24 = ConvertTo24Hour(globalTimeSystem.currentHour, globalTimeSystem.isAM);
        int minute = globalTimeSystem.currentMinute;
        string weekDay = globalTimeSystem.weekDayText.text;

        HandleChickenCrow(hour24, minute, currentWeather);
        HandleChurchBell(hour24, minute, weekDay);

        // Weather overrides ambient
        if (currentWeather != lastWeather)
        {
            lastWeather = currentWeather;
            SwitchWeatherAmbient(currentWeather);
        }

        // Sunny/Clear → allow ambient segments
        if (IsNeutralWeather(currentWeather))
        {
            if (currentSegment != lastSegment)
            {
                lastSegment = currentSegment;
                SwitchAmbient(currentSegment);
            }
        }
    }

    private void HandleChickenCrow(int hour24, int minute, string weather)
    {
        // Prevent crowing in bad weather
        if (weather == "Rainy" || weather == "Typhoon" || weather == "Blizzard" || weather == "Thunderstorm")
        {
            crowTimer = 0f;
            if (chickenSource.isPlaying)
                FadeOutChickenCrow();
            return;
        }

        // Normal crowing logic
        if (hour24 == 6 || (hour24 == 7 && minute <= 20))
        {
            crowTimer += Time.deltaTime;
            if (crowTimer >= crowInterval)
            {
                PlayChickenCrow();
                crowTimer = 0f;
            }
        }
        else
        {
            crowTimer = 0f;
            if (chickenSource.isPlaying)
                FadeOutChickenCrow();
        }
    }

    private void HandleChurchBell(int hour24, int minute, string weekDay)
    {
        if (weekDay == "Sun")
        {
            if ((hour24 == 7 && minute >= 0 && minute <= 45) ||
                (hour24 == 9 && minute >= 0 && minute <= 10))
            {
                PlayChurchBell();
            }
        }
    }

    private void PlayChickenCrow()
    {
        if (chickenCrowClip != null)
        {
            chickenSource.clip = chickenCrowClip;
            chickenSource.Play();
        }
    }

    private void FadeOutChickenCrow()
    {
        chickenSource.volume = Mathf.MoveTowards(chickenSource.volume, 0f, Time.deltaTime / fadeDuration);
        if (chickenSource.volume <= 0.01f && chickenSource.isPlaying)
        {
            chickenSource.Stop();
        }
    }

    private void PlayChurchBell()
    {
        if (churchBellClip != null && !bellSource.isPlaying)
        {
            bellSource.clip = churchBellClip;
            bellSource.Play();
        }
    }

    private void SwitchAmbient(string segment)
    {
        AudioClip clip = null;

        switch (segment)
        {
            case "Dawn": clip = dawnClip; break;
            case "Early Morning": clip = earlyMorningClip; break;
            case "Morning": clip = morningClip; break;
            case "Midday": clip = middayClip; break;
            case "Afternoon": clip = afternoonClip; break;
            case "Late Afternoon": clip = lateAfternoonClip; break;
            case "Evening": clip = eveningClip; break;
            case "Night": clip = nightClip; break;
            case "Midnight": clip = midnightClip; break;
        }

        if (clip != null && ambientSource.clip != clip)
        {
            ambientSource.Stop();
            ambientSource.clip = clip;
            ambientSource.Play();
        }
    }

    private void SwitchWeatherAmbient(string weather)
    {
        AudioClip clip = null;

        switch (weather)
        {
            case "Rainy": clip = rainyClip; break;
            case "Thunderstorm": clip = thunderstormClip; break;
            case "Blizzard": clip = blizzardClip; break;
            case "Windy": clip = windyClip; break;
            case "Snowy": clip = snowyClip; break;
            case "Typhoon": clip = typhoonClip; break;
        }

        if (clip != null)
        {
            ambientSource.Stop();
            ambientSource.clip = clip;
            ambientSource.Play();
        }
        else
        {
            // Neutral weather (Sunny/Clear) → keep ambient
            ambientSource.Stop();
        }
    }

    private bool IsNeutralWeather(string weather)
    {
        return weather == "Sunny" || weather == "Clear";
    }

    private int ConvertTo24Hour(int hour, bool am)
    {
        if (am) return hour == 12 ? 0 : hour;
        else return hour == 12 ? 12 : hour + 12;
    }
}
