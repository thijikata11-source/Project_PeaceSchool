using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Character/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("Permanent Identity Profiles")]
    public int characterId;
    public string characterName;
    public Sprite characterPortrait;

    /// <summary>
    /// Static data table tracking experience progression charts perfectly.
    /// </summary>
    public int CalculateRequiredExpForLevel(int level)
    {
        if (level <= 1) return 50;
        if (level <= 5) return 70;
        if (level <= 10) return 100;
        if (level <= 13) return 140;
        if (level <= 18) return 200;
        if (level <= 20) return 240;
        if (level <= 24) return 300;
        if (level <= 30) return 400;
        if (level <= 33) return 500;
        if (level <= 37) return 550;
        if (level <= 39) return 620;
        if (level <= 45) return 730;
        if (level <= 50) return 850;
        if (level <= 55) return 910;
        if (level <= 59) return 1050;
        if (level <= 62) return 1250;
        if (level <= 68) return 1450;
        if (level <= 75) return 1550;
        if (level <= 79) return 1650;
        if (level <= 88) return 1850;
        if (level <= 98) return 2050;
        return 3000;
    }


}
