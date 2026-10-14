using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Battle/Skill Asset")]
public class SkillScriptable : ScriptableObject
{
    public enum SkillAbilityType
    {
        Offensive,
        Support
    }

    public enum SkillScopeType
    {
        SelfOnly,
        OneCharacter,
        ThreeCharacters,
        AllCharacters
    }

    [Header("Identity & Presentation")]
    public string skillName;
    [TextArea(2, 4)]
    public string skillDescription;
    public Sprite skillIcon;

    [Header("Behavior & Scope Settings")]
    [Tooltip("Offensive targets an enemy, Support restores resources or strengthens allies/self.")]
    public SkillAbilityType skillAbility;
    [Tooltip("Defines how many targets are hit by this ability loop selection.")]
    public SkillScopeType skillScope;

    [Header("Execution Parameters")]
    [Tooltip("Type the EXACT name of the target animation clip state inside your animator controller.")]
    public string skillAnimationName;

    // ✨ NEW VISUAL EFFECT PREFAB SLOT (Draggable inside the inspector, fine if empty!)
    [Tooltip("Optional particle or visual effect prefab to spawn on the target during the animation event hook frame.")]
    public GameObject skillEffectPrefab;



    // 🛠️ FIXED REFERENCE: Now points cleanly to CharacterStats enum wrapper
    [Tooltip("Select the elemental identity bitmask of this ability.")]
    public CharacterStats.ElementType skillElement;

    [Header("Resource Activation Cost")]
    public int hpCost; // 🩸 NEW HEALTH COST REQUIREMENT FIELD
    public int manaCost;
    public int staminaCost;
    public int energyCost;


    [Header("Offensive Metrics (If Offensive)")]
    [Tooltip("Base modification points applied when computing opponent defense mitigations.")]
    public int skillDamage;

    [Header("Restoration Metrics (If Support)")]
    [Tooltip("Amount of Health Points restored to target(s).")]
    public int healHP;
    [Tooltip("Amount of Mana restored to target(s).")]
    public int restoreMana;
    [Tooltip("Amount of Stamina points restored to target(s).")]
    public int restoreStamina;
    [Tooltip("Amount of Energy points restored to target(s).")]
    public int restoreEnergy;

    [Header("Movement Rule")]
    [Tooltip("Check this if the skill stands still to execute. Uncheck if it requires running up to the enemy.")]
    public bool isRangedSkill = true;
}
