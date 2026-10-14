using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For UI Image

public class CharacterSystem : MonoBehaviour
{
    public enum Gender { Boy, Girl }
    public enum Relationship
    {
        Stranger = 0,
        Acquaintance = 1,
        Friend = 80,
        BestFriend = 200, // Same gender only
        Lover = 500,      // Opposite gender only
        Spouse            // Opposite gender only and Married
    }

    [Header("Character Info")]
    public string CharacterName;
    public Gender CharacterGender;
    public string BirthSeason;
    public int BirthDay;

    [Header("Relationship")]
    public Relationship RelationshipStatus = Relationship.Stranger;
    public int affectionLevel;
    public int affectionExperience;
    public int heartLevel;
    public bool isMarried; // ✅ add marriage flag

    [Header("UI")]
    public Image HeartIcon; // Assign in Inspector

    private Dictionary<int, string> heartColors = new Dictionary<int, string>()
    {
        {1000, "Black"},
        {5000, "Purple"},
        {7000, "Blue"},
        {9000, "Green"},
        {12000, "Yellow"},
        {13000, "Orange"},
        {15000, "Red"}
    };

    public void UpdateRelationship(PlayerProfile player)
    {
        // ✅ Update RelationshipStatus based on affection thresholds
        if (affectionLevel <= 0)
        {
            RelationshipStatus = Relationship.Stranger;
        }
        else if (affectionLevel >= 1 && affectionLevel < 80)
        {
            RelationshipStatus = Relationship.Acquaintance;
        }
        else if (affectionLevel >= 80 && affectionLevel < 200)
        {
            RelationshipStatus = Relationship.Friend;
        }
        else if (affectionLevel >= 200 && affectionLevel < 500)
        {
            if (player.PlayerGender == CharacterGender.ToString())
                RelationshipStatus = Relationship.BestFriend;
            else
                RelationshipStatus = Relationship.Friend; // fallback
        }
        else if (affectionLevel >= 500)
        {
            if (player.PlayerGender != CharacterGender.ToString())
            {
                if (isMarried)
                    RelationshipStatus = Relationship.Spouse;
                else
                    RelationshipStatus = Relationship.Lover;
            }
            else
            {
                RelationshipStatus = Relationship.Friend; // fallback for same gender
            }
        }

        UpdateHeartIcon(player);
    }

    private void UpdateHeartIcon(PlayerProfile player)
    {
        if (HeartIcon == null) return;

        if (player.PlayerGender != CharacterGender.ToString())
        {
            HeartIcon.enabled = true;

            foreach (var kvp in heartColors)
            {
                if (affectionLevel >= kvp.Key)
                {
                    // Example: HeartIcon.sprite = Resources.Load<Sprite>("Hearts/" + kvp.Value);
                }
            }
        }
        else
        {
            HeartIcon.enabled = false;
        }
    }
}
