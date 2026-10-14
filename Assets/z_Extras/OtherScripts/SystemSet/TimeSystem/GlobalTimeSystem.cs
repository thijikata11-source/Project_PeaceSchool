using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GlobalTimeSystem : MonoBehaviour
{
    [Header("Time Settings")]
    public int realSecondsPerDay = 120;
    public TMP_Text timeText;
    public TMP_Text amPmText;
    public TMP_Text dayText;
    public TMP_Text yearText;
    public TMP_Text weekDayText;

    [Header("Season Settings")]
    public Image seasonImage;
    public Sprite springSprite;
    public Sprite summerSprite;
    public Sprite fallSprite;
    public Sprite winterSprite;

    [Header("Lighting & Skybox Settings")]
    public Light directionalLight;
    public Material defaultSkybox;
    public Material dawnSkybox;
    public Material earlyMorningSkybox;
    public Material morningSkybox;
    public Material middaySkybox;
    public Material afternoonSkybox;
    public Material lateAfternoonSkybox;
    public Material eveningSkybox;
    public Material nightSkybox;
    public Material midnightSkybox;

    [Tooltip("Gradient colors for each segment (start,end).")]
    public Gradient dawnGradient;
    public Gradient earlyMorningGradient;
    public Gradient morningGradient;
    public Gradient middayGradient;
    public Gradient afternoonGradient;
    public Gradient lateAfternoonGradient;
    public Gradient eveningGradient;
    public Gradient nightGradient;
    public Gradient midnightGradient;

    [Header("Skybox Intensity Settings")]
    public float dawnIntensity = 1f;
    public float earlyMorningIntensity = 1.2f;
    public float morningIntensity = 1.5f;
    public float middayIntensity = 2f;
    public float afternoonIntensity = 1.5f;
    public float lateAfternoonIntensity = 1.3f;
    public float eveningIntensity = 1.1f;
    public float nightIntensity = 0.8f;
    public float midnightIntensity = 0.6f;

    [Header("Light Rotation Settings")]
    public float rotationSmoothSpeed = 1f;
    public Vector3 dawnRotation = new Vector3(15f, 0f, 0f);
    public Vector3 earlyMorningRotation = new Vector3(30f, 0f, 0f);
    public Vector3 morningRotation = new Vector3(60f, 0f, 0f);
    public Vector3 middayRotation = new Vector3(90f, 0f, 0f);
    public Vector3 afternoonRotation = new Vector3(120f, 0f, 0f);
    public Vector3 lateAfternoonRotation = new Vector3(150f, 0f, 0f);
    public Vector3 eveningRotation = new Vector3(180f, 0f, 0f);
    public Vector3 nightRotation = new Vector3(210f, 0f, 0f);
    public Vector3 midnightRotation = new Vector3(270f, 0f, 0f);

    [Header("Weather Skyboxes")]
    public Material rainySkybox;
    public Material thunderstormSkybox;
    public Material blizzardSkybox;
    public Material windySkybox;
    public Material snowySkybox;
    public Material typhoonSkybox;

    [Header("Weather Light Colors")]
    public Gradient rainyGradient;
    public Gradient thunderstormGradient;
    public Gradient blizzardGradient;
    public Gradient windyGradient;
    public Gradient snowyGradient;
    public Gradient typhoonGradient;

    [Header("Weather Intensities")]
    public float rainyIntensity = 1.0f;
    public float thunderstormIntensity = 0.8f;
    public float blizzardIntensity = 0.7f;
    public float windyIntensity = 1.2f;
    public float snowyIntensity = 0.9f;
    public float typhoonIntensity = 0.6f;

    [Header("Debug Time Control")]
    public bool autoAdvanceTime = true;
    [Range(1, 12)] public int currentHour = 12;
    [Range(0, 59)] public int currentMinute = 0;
    public bool isAM = true;

    [Header("Runtime Debug (Inspector Only)")]
    [SerializeField] private string currentSegment;
    [SerializeField] private string currentWeekDay;
    [SerializeField] private int rawMinuteCount;
    [SerializeField] private int currentSeasonLength;

    [Header("Editable Debug Values")]
    public int currentDay = 1;
    public int currentYear = 1;

    private float timeCounter;
    private int currentSeasonIndex = 0;

    private string[] weekDays = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

    public float blinkInterval = 0.5f;
    private float blinkTimer;
    private bool showMinutes = true;

    [Header("Reference to Weather System")]
    public WeatherCycleSystem weatherSystem;

    // Weather consistency tracking
    private int consecutiveRainyDays = 0;
    private int consecutiveSnowyDays = 0;
    private int specialWeatherDaysRemaining = 0;
    private string specialWeather = null;

    public int CurrentSeasonIndex => currentSeasonIndex;

    private void Start()
    {
        UpdateSeasonImage();
        UpdateSeasonLength();
        AssignDailyWeather(); // initialize weather
        UpdateLightingAndSkybox();
    }

    private void Update()
    {
        if (autoAdvanceTime)
        {
            timeCounter += Time.deltaTime;
            float secondsPerMinute = realSecondsPerDay / (24f * 60f);

            if (timeCounter >= secondsPerMinute)
            {
                timeCounter = 0;
                AdvanceTime();
            }
        }

        blinkTimer += Time.deltaTime;
        if (blinkTimer >= blinkInterval)
        {
            blinkTimer = 0f;
            showMinutes = !showMinutes;
        }

        UpdateUI();
    }

    private void AdvanceTime()
    {
        currentMinute++;
        rawMinuteCount = currentMinute;

        if (currentMinute >= 60)
        {
            currentMinute = 0;
            currentHour++;

            if (currentHour > 12) currentHour = 1;

            if (currentHour == 12)
            {
                isAM = !isAM;

                if (isAM) NextDay(); // midnight rollover
            }
        }
    }

    private void NextDay()
    {
        currentDay++;
        if (currentDay > currentSeasonLength)
        {
            currentDay = 1;
            currentSeasonIndex++;
            if (currentSeasonIndex > 3)
            {
                currentSeasonIndex = 0;
                currentYear++;
            }
            UpdateSeasonImage();
            UpdateSeasonLength();
        }

        AssignDailyWeather();

        // ✅ Reset requirementsMet for ALL DialogueDecisionManagers
        DialogueDecisionManager[] managers = FindObjectsOfType<DialogueDecisionManager>();
        foreach (var manager in managers)
        {
            manager.ResetRequirements();
        }
    }


    private void UpdateUI()
    {
        int displayMinute = Mathf.RoundToInt(currentMinute / 5f) * 5;
        if (displayMinute == 60) displayMinute = 55;

        string minuteString = showMinutes ? $":{displayMinute:D2}" : ":";
        timeText.text = $"{currentHour:D2}{minuteString}";
        amPmText.text = isAM ? "AM" : "PM";
        dayText.text = $"Day {currentDay}";
        yearText.text = $"Year {currentYear}";

        int weekIndex = (currentDay - 1) % 7;
        currentWeekDay = weekDays[weekIndex];
        weekDayText.text = currentWeekDay;

        currentSegment = GetCurrentDailySegment();
        UpdateLightingAndSkybox();
    }

    private void UpdateSeasonImage()
    {
        switch (currentSeasonIndex)
        {
            case 0: seasonImage.sprite = springSprite; break;
            case 1: seasonImage.sprite = summerSprite; break;
            case 2: seasonImage.sprite = fallSprite; break;
            case 3: seasonImage.sprite = winterSprite; break;
        }
    }

    private void UpdateSeasonLength()
    {
        currentSeasonLength = 30; // always 30 days
    }

    public string GetCurrentDailySegment()
    {
        int hour24 = ConvertTo24Hour(currentHour, isAM);

        if (hour24 >= 0 && hour24 < 4) return "Dawn";
        if (hour24 >= 4 && hour24 < 7) return "Early Morning";
        if (hour24 >= 7 && hour24 < 10) return "Morning";
        if (hour24 >= 10 && hour24 < 12) return "Midday";
        if (hour24 >= 12 && hour24 < 15) return "Afternoon";
        if (hour24 >= 15 && hour24 < 17) return "Late Afternoon";
        if (hour24 >= 17 && hour24 < 19) return "Evening";
        if (hour24 >= 19 && hour24 < 22) return "Night";
        return "Midnight";
    }

    private void AssignDailyWeather()
    {
        if (specialWeatherDaysRemaining > 0)
        {
            weatherSystem.currentWeather = specialWeather;
            specialWeatherDaysRemaining--;
            return;
        }

        string chosenWeather = RollSeasonalWeather(currentSeasonIndex, currentDay);

        // Track consecutive days
        if (chosenWeather == "Rainy")
        {
            consecutiveRainyDays++;
            consecutiveSnowyDays = 0;
        }
        else if (chosenWeather == "Snowy")
        {
            consecutiveSnowyDays++;
            consecutiveRainyDays = 0;
        }
        else
        {
            consecutiveRainyDays = 0;
            consecutiveSnowyDays = 0;
        }

        // Trigger Typhoon
        if (consecutiveRainyDays >= 3)
        {
            specialWeather = "Typhoon";
            specialWeatherDaysRemaining = Random.Range(3, 5);
            consecutiveRainyDays = 0;
            weatherSystem.currentWeather = specialWeather;
            return;
        }

        // Trigger Blizzard
        if (consecutiveSnowyDays >= 3)
        {
            specialWeather = "Blizzard";
            specialWeatherDaysRemaining = Random.Range(3, 5);
            consecutiveSnowyDays = 0;
            weatherSystem.currentWeather = specialWeather;
            return;
        }

        weatherSystem.currentWeather = chosenWeather;
    }

    private string RollSeasonalWeather(int seasonIndex, int day)
    {
        int week = (day - 1) / 7; // 0–3
        float roll = Random.value * 100f;

        switch (seasonIndex)
        {
            case 0: // Spring
                if (week == 0 || week == 1) return roll < 70 ? "Clear" : roll < 90 ? "Sunny" : "Rainy";
                if (week == 2) return roll < 70 ? "Rainy" : roll < 80 ? "Clear" : roll < 90 ? "Sunny" : "Windy";
                return roll < 50 ? "Clear" : roll < 90 ? "Sunny" : "Rainy";

            case 1: // Summer
                if (week == 0 || week == 3) return roll < 50 ? "Sunny" : "Clear";
                return roll < 70 ? "Clear" : roll < 90 ? "Sunny" : "Rainy";

            case 2: // Autumn
                if (week == 0) return roll < 50 ? "Clear" : "Sunny";
                if (week == 1) return roll < 55 ? "Clear" : roll < 60 ? "Sunny" : roll < 90 ? "Rainy" : roll < 95 ? "Windy" : "Thunderstorm";
                if (week == 2) return roll < 10 ? "Clear" : roll < 15 ? "Sunny" : roll < 20 ? "Windy" : roll < 95 ? "Rainy" : "Thunderstorm";
                return roll < 50 ? "Clear" : "Rainy";

            case 3: // Winter
                if (week == 0) return roll < 50 ? "Snowy" : "Clear";
                if (week == 1) return roll < 50 ? "Clear" : roll < 55 ? "Windy" : "Snowy";
                if (week == 2) return roll < 45 ? "Clear" : roll < 50 ? "Windy" : "Snowy";
                return roll < 50 ? "Clear" : roll < 55 ? "Sunny" : "Snowy";
        }
        return "Clear";
    }

    private void UpdateLightingAndSkybox()
    {
        string currentWeather = weatherSystem != null ? weatherSystem.currentWeather : "Sunny";

        Gradient g = null;
        Material sky = null;
        float intensity = 1f;
        Vector3 targetRotation = Vector3.zero;

        // --- Weather overrides ---
        switch (currentWeather)
        {
            case "Rainy": g = rainyGradient; sky = rainySkybox; intensity = rainyIntensity; break;
            case "Thunderstorm": g = thunderstormGradient; sky = thunderstormSkybox; intensity = thunderstormIntensity; break;
            case "Blizzard": g = blizzardGradient; sky = blizzardSkybox; intensity = blizzardIntensity; break;
            case "Windy": g = windyGradient; sky = windySkybox; intensity = windyIntensity; break;
            case "Snowy": g = snowyGradient; sky = snowySkybox; intensity = snowyIntensity; break;
            case "Typhoon": g = typhoonGradient; sky = typhoonSkybox; intensity = typhoonIntensity; break;
            default:
                // Sunny/Clear → normal segment-based lighting
                switch (currentSegment)
                {
                    case "Dawn": g = dawnGradient; sky = dawnSkybox; intensity = dawnIntensity; targetRotation = dawnRotation; break;
                    case "Early Morning": g = earlyMorningGradient; sky = earlyMorningSkybox; intensity = earlyMorningIntensity; targetRotation = earlyMorningRotation; break;
                    case "Morning": g = morningGradient; sky = morningSkybox; intensity = morningIntensity; targetRotation = morningRotation; break;
                    case "Midday": g = middayGradient; sky = middaySkybox; intensity = middayIntensity; targetRotation = middayRotation; break;
                    case "Afternoon": g = afternoonGradient; sky = afternoonSkybox; intensity = afternoonIntensity; targetRotation = afternoonRotation; break;
                    case "Late Afternoon": g = lateAfternoonGradient; sky = lateAfternoonSkybox; intensity = lateAfternoonIntensity; targetRotation = lateAfternoonRotation; break;
                    case "Evening": g = eveningGradient; sky = eveningSkybox; intensity = eveningIntensity; targetRotation = eveningRotation; break;
                    case "Night": g = nightGradient; sky = nightSkybox; intensity = nightIntensity; targetRotation = nightRotation; break;
                    case "Midnight": g = midnightGradient; sky = midnightSkybox; intensity = midnightIntensity; targetRotation = midnightRotation; break;
                }
                break;
        }

        if (directionalLight != null && directionalLight.gameObject.activeSelf)
        {
            if (g != null)
            {
                float t = currentMinute / 60f;
                directionalLight.color = g.Evaluate(t);
            }

            if (sky != null)
            {
                RenderSettings.skybox = sky;
                if (RenderSettings.skybox.HasProperty("_Exposure"))
                    RenderSettings.skybox.SetFloat("_Exposure", intensity);
                else
                    RenderSettings.ambientIntensity = intensity;

                DynamicGI.UpdateEnvironment();
            }

            if (targetRotation != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.Euler(targetRotation);
                directionalLight.transform.rotation = Quaternion.Slerp(
                    directionalLight.transform.rotation,
                    targetRot,
                    Time.deltaTime * rotationSmoothSpeed
                );
            }
        }
        else if (defaultSkybox != null)
        {
            RenderSettings.skybox = defaultSkybox;
            DynamicGI.UpdateEnvironment();
        }
    }

    private int ConvertTo24Hour(int hour, bool am)
    {
        if (am) return hour == 12 ? 0 : hour;
        else return hour == 12 ? 12 : hour + 12;
    }

    public void AdvanceToNextDay(int hour, int minute)
    {
        NextDay();
        currentHour = hour;
        currentMinute = minute;
        isAM = hour < 12;
        UpdateUI();
    }

    public void AdvanceDays(int days, int hour, int minute)
    {
        for (int i = 0; i < days; i++) NextDay();
        currentHour = hour;
        currentMinute = minute;
        isAM = hour < 12;
        UpdateUI();
    }
}
