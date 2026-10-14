using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    // =========================================================================
    //  🛠️ ELEMENTAL REGISTRY BITMASK (Hosted natively inside the component)
    // =========================================================================
    [System.Flags]
    public enum ElementType
    {
        None = 0,
        Neutral = 1 << 0,
        Lightning = 1 << 1,
        Fire = 1 << 2,
        Water = 1 << 3,
        Ice = 1 << 4,
        Air = 1 << 5,
        Earth = 1 << 6,
        Dark = 1 << 7
    }

    [Header("Base Identity Blueprint Asset")]
    [Tooltip("Drag your cleaned identity-only CharacterData asset profile here.")]
    public CharacterData baseCharacterData;

    // =========================================================================
    //  📊 THE ABSOLUTE SOURCE OF TRUTH (Fully editable inside the inspector!)
    // =========================================================================

    [Header("Identity & Leveling Status")]
    public string Name;
    public int Level = 1;
    public int CurrentExp;
    public int ExpNeeded = 50;

    [Header("Elemental Affinities")]
    public ElementType strongWith;
    public ElementType weakWith;

    [Header("Attributes Pools")]
    public int CurrentHP = 100;
    public int MaxHP = 100;
    public int CurrentMana = 50;
    public int MaxMana = 50;
    public int CurrentStamina = 20;
    public int MaxStamina = 20;
    public int CurrentEnergy = 100;
    public int MaxEnergy = 100;

    [Header("Combat Power Scaling Attributes")]
    public int PhysicalAttack = 15;
    public int PhysicalDefense = 8;
    public int MagicAttack = 12;
    public int MagicDefense = 6;

    [Header("Percentage Performance Modifiers")]
    public float Speed = 10f;
    public float Agility = 10f;
    public float Accuracy = 95f;
    public float Critical = 5f;
    public float Dodge = 5f;
    public float Luck = 5f;
    private void Awake()
    {
        UpdateRequiredExp();
    }

    private void OnValidate()
    {
        // Shorthand Helper: If you drag a scriptable template asset in, 
        // fill the Name field automatically from the template, but keep everything else unlocked!
        if (baseCharacterData != null && string.IsNullOrEmpty(Name))
        {
            Name = baseCharacterData.characterName;
            UpdateRequiredExp();
        }
    }

    /// <summary>
    /// Live Experience routing logic completely managed on the Monobehaviour layer fields.
    /// </summary>
    public void RewardExperiencePoints(int amount)
    {
        if (Level >= 100)
        {
            CurrentExp = 0;
            return;
        }

        CurrentExp += amount;

        while (CurrentExp >= ExpNeeded && Level < 100)
        {
            CurrentExp -= ExpNeeded;
            TriggerLevelUpGrowth();
        }
    }

    private void TriggerLevelUpGrowth()
    {
        Level++;
        UpdateRequiredExp();

        // Native allocation growth math directly scales your editable variables
        int hpGrowth = Random.Range(10, 16);
        MaxHP += hpGrowth;
        CurrentHP += hpGrowth; // Give a partial health restore upon level up

        int manaGrowth = Random.Range(5, 9);
        MaxMana += manaGrowth;
        CurrentMana += manaGrowth;

        MaxStamina += Random.Range(4, 7);
        PhysicalAttack += Random.Range(2, 5);
        PhysicalDefense += Random.Range(1, 4);
        MagicAttack += Random.Range(2, 5);
        MagicDefense += Random.Range(1, 4);

        Debug.Log($"🎉 {Name} leveled up to Level {Level}! Next milestone needs {ExpNeeded} EXP.");
    }

    private void UpdateRequiredExp()
    {
        if (baseCharacterData != null)
        {
            ExpNeeded = baseCharacterData.CalculateRequiredExpForLevel(Level);
        }
        else
        {
            // Simple backup baseline rule calculation if no scriptable file asset is assigned
            ExpNeeded = Level * 50;
        }
    }
}
