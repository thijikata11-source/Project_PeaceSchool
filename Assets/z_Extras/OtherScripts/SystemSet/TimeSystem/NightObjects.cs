using UnityEngine;
using System.Collections.Generic;

public class NightObjects : MonoBehaviour
{
    [Header("References")]
    public GlobalTimeSystem timeSystem;   // Drag your GlobalTimeSystem here
    public List<GameObject> nightObjects; // Assign objects to toggle

    [Header("Nighttime Range")]
    public int nightStartHour = 18;   // 6:00 PM
    public int nightEndHour = 5;      // 5:30 AM
    public int nightEndMinute = 30;   // 5:30 cutoff

    private bool lastState = false;

    void Update()
    {
        if (timeSystem == null) return;

        int hour24 = ConvertTo24Hour(timeSystem.currentHour, timeSystem.isAM);
        int minute = timeSystem.currentMinute;

        bool isNight = IsNightTime(hour24, minute);

        if (isNight != lastState)
        {
            ToggleObjects(isNight);
            lastState = isNight;
        }
    }

    private bool IsNightTime(int hour24, int minute)
    {
        // Night starts at 18:00 (6 PM) → continues until 05:30
        if (hour24 >= nightStartHour) return true;
        if (hour24 < nightEndHour) return true;
        if (hour24 == nightEndHour && minute <= nightEndMinute) return true;

        return false;
    }

    private void ToggleObjects(bool enable)
    {
        foreach (var obj in nightObjects)
        {
            if (obj != null) obj.SetActive(enable);
        }
    }

    private int ConvertTo24Hour(int hour, bool am)
    {
        if (am) return hour == 12 ? 0 : hour;
        else return hour == 12 ? 12 : hour + 12;
    }
}
