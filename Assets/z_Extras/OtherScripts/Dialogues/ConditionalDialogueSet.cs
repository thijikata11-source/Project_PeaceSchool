using System.Collections.Generic;
using UnityEngine;

[System.Flags]
public enum WeekdayCondition
{
    Any = 0,
    Mon = 1 << 0,
    Tue = 1 << 1,
    Wed = 1 << 2,
    Thu = 1 << 3,
    Fri = 1 << 4,
    Sat = 1 << 5,
    Sun = 1 << 6
}

public enum SegmentCondition
{
    Any,
    Dawn,
    EarlyMorning,
    Morning,
    Midday,
    Afternoon,
    LateAfternoon,
    Evening,
    Night,
    Midnight
}

public enum WeatherCondition
{
    Any,
    Sunny,
    Clear,
    Rainy,
    Thunderstorm,
    Blizzard,
    Windy,
    Snowy,
    Typhoon
}

public enum RequirementsCondition
{
    Any,
    No,
    Yes
}

public enum RelationshipCondition
{
    Any,
    Stranger,
    Acquaintance,
    Friend,
    BestFriend,
    Lover,
    Spouse
}

[System.Serializable]
public class ConditionalDialogueSet
{
    [Header("Conditions")]
    public SegmentCondition segmentCondition = SegmentCondition.Any;

    // ✅ Flags enum allows multiple weekdays in one dropdown
    public WeekdayCondition weekdayCondition = WeekdayCondition.Any;

    public WeatherCondition weatherCondition = WeatherCondition.Any;
    public RequirementsCondition requirementsCondition = RequirementsCondition.Any;
    public RelationshipCondition relationshipCondition = RelationshipCondition.Any;

    [Header("Dialogue Lines")]
    public List<DialogueEntry> lines = new List<DialogueEntry>();
}

[System.Serializable]
public class DialogueEntry
{
    [Header("Line Settings")]
    public bool isDecisionPoint = false;

    [TextArea(2, 5)]
    public string lineText;

    [Header("Decision Settings")]
    public DecisionDialogueSet decisionSet;
}

[System.Serializable]
public class DecisionDialogueSet
{
    [Header("Dialogue Prompt")]
    [TextArea(2, 5)]
    public string prompt;

    [Header("Decision Options")]
    public List<DecisionOption> options = new List<DecisionOption>();
}

[System.Serializable]
public class DecisionOption
{
    public string optionText;

    [TextArea(2, 5)]
    public List<string> resultingLines;

    [Header("Reward/Consequence")]
    public List<string> effectCommands;

    [Header("Failure Dialogue (if requirements not met)")]
    [TextArea(2, 5)]
    public List<string> failureLines;

    [Header("Success Dialogue (after requirements met)")]
    [TextArea(2, 5)]
    public List<string> successLines;
}
