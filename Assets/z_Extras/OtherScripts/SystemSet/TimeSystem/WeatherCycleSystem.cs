using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WeatherCycleSystem : MonoBehaviour
{
    [Header("Dependencies")]
    public GlobalTimeSystem timeSystem;

    [Header("Weather UI")]
    public Image weatherIcon;
    public Sprite sunnySprite;
    public Sprite clearSprite;
    public Sprite rainySprite;
    public Sprite thunderstormSprite;
    public Sprite blizzardSprite;
    public Sprite windySprite;
    public Sprite snowySprite;
    public Sprite typhoonSprite;

    [Header("Weather Settings")]
    public string currentWeather = "Sunny";
    public int consecutiveRainyDays = 0;
    public int consecutiveSnowyDays = 0;
    public int specialWeatherDaysRemaining = 0;
    private string specialWeather = null;

    [Header("Weather Objects Control")]
    public List<GameObject> sunnyEnableObjects;
    public List<GameObject> sunnyDisableObjects;
    public List<GameObject> clearEnableObjects;
    public List<GameObject> clearDisableObjects;
    public List<GameObject> rainyEnableObjects;
    public List<GameObject> rainyDisableObjects;
    public List<GameObject> thunderstormEnableObjects;
    public List<GameObject> thunderstormDisableObjects;
    public List<GameObject> blizzardEnableObjects;
    public List<GameObject> blizzardDisableObjects;
    public List<GameObject> windyEnableObjects;
    public List<GameObject> windyDisableObjects;
    public List<GameObject> snowyEnableObjects;
    public List<GameObject> snowyDisableObjects;
    public List<GameObject> typhoonEnableObjects;
    public List<GameObject> typhoonDisableObjects;

    private Dictionary<GameObject, bool> originalStates = new Dictionary<GameObject, bool>();

    private void Start()
    {
        UpdateWeatherForToday(); // initialize
    }

    // Public so SleepSystem or GlobalTimeSystem can call it after advancing the day
    public void UpdateWeatherForToday()
    {
        if (specialWeatherDaysRemaining > 0)
        {
            currentWeather = specialWeather;
            specialWeatherDaysRemaining--;
        }
        else
        {
            string chosenWeather = RollSeasonalWeather(timeSystem.CurrentSeasonIndex, timeSystem.currentDay);

            // Track streaks
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

            // Typhoon trigger
            if (consecutiveRainyDays >= 3)
            {
                specialWeather = "Typhoon";
                specialWeatherDaysRemaining = Random.Range(3, 5);
                consecutiveRainyDays = 0;
                currentWeather = specialWeather;
            }
            // Blizzard trigger
            else if (consecutiveSnowyDays >= 3)
            {
                specialWeather = "Blizzard";
                specialWeatherDaysRemaining = Random.Range(3, 5);
                consecutiveSnowyDays = 0;
                currentWeather = specialWeather;
            }
            else
            {
                currentWeather = chosenWeather;
            }
        }

        UpdateWeatherIcon();
        UpdateWeatherObjects();
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

    private void UpdateWeatherIcon()
    {
        if (weatherIcon == null) return;

        switch (currentWeather)
        {
            case "Sunny": weatherIcon.sprite = sunnySprite; break;
            case "Clear": weatherIcon.sprite = clearSprite; break;
            case "Rainy": weatherIcon.sprite = rainySprite; break;
            case "Thunderstorm": weatherIcon.sprite = thunderstormSprite; break;
            case "Blizzard": weatherIcon.sprite = blizzardSprite; break;
            case "Windy": weatherIcon.sprite = windySprite; break;
            case "Snowy": weatherIcon.sprite = snowySprite; break;
            case "Typhoon": weatherIcon.sprite = typhoonSprite; break;
        }
    }

    private void UpdateWeatherObjects()
    {
        RestoreOriginalStates();

        switch (currentWeather)
        {
            case "Sunny": EnableObjects(sunnyEnableObjects); DisableObjects(sunnyDisableObjects); break;
            case "Clear": EnableObjects(clearEnableObjects); DisableObjects(clearDisableObjects); break;
            case "Rainy": EnableObjects(rainyEnableObjects); DisableObjects(rainyDisableObjects); break;
            case "Thunderstorm": EnableObjects(thunderstormEnableObjects); DisableObjects(thunderstormDisableObjects); break;
            case "Blizzard": EnableObjects(blizzardEnableObjects); DisableObjects(blizzardDisableObjects); break;
            case "Windy": EnableObjects(windyEnableObjects); DisableObjects(windyDisableObjects); break;
            case "Snowy": EnableObjects(snowyEnableObjects); DisableObjects(snowyDisableObjects); break;
            case "Typhoon": EnableObjects(typhoonEnableObjects); DisableObjects(typhoonDisableObjects); break;
        }
    }

    private void EnableObjects(List<GameObject> objects)
    {
        foreach (var obj in objects)
            if (obj != null) obj.SetActive(true);
    }

    private void DisableObjects(List<GameObject> objects)
    {
        foreach (var obj in objects)
        {
            if (obj != null)
            {
                if (!originalStates.ContainsKey(obj))
                    originalStates[obj] = obj.activeSelf;
                obj.SetActive(false);
            }
        }
    }

    private void RestoreOriginalStates()
    {
        foreach (var kvp in originalStates)
        {
            if (kvp.Key != null)
                kvp.Key.SetActive(kvp.Value);
        }
        originalStates.Clear();
    }

    private string GetSeasonName(int index)
    {
        switch (index)
        {
            case 0: return "Spring";
            case 1: return "Summer";
            case 2: return "Autumn";
            case 3: return "Winter";
            default: return "Spring";
        }
    }
}
