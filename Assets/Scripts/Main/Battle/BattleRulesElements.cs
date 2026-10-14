using UnityEngine;

public static class BattleRulesElements
{
    /// <summary>
    /// Evaluates elemental relationships to output an offensive/defensive modifier score.
    /// </summary>
    public static float GetElementalDamageMultiplier(CharacterStats.ElementType skillElement, CharacterStats.ElementType targetStrongWith, CharacterStats.ElementType targetWeakWith)
    {
        float multiplier = 1.0f; // Default standard damage modifier baseline

        // 🛠️ BITWISE AND CHECK: Check if the skill's element hits a target's weakness flag
        if ((skillElement & targetWeakWith) != CharacterStats.ElementType.None)
        {
            multiplier *= 1.5f; // Deal 150% damage against weaknesses
            Debug.Log($"🔥 [BattleRulesElements] Weakness Exploited! Element: {skillElement}");
        }

        // 🛠️ BITWISE AND CHECK: Check if the skill's element hits a target's resistance flag
        if ((skillElement & targetStrongWith) != CharacterStats.ElementType.None)
        {
            multiplier *= 0.5f; // Deal 50% damage against resistances
            Debug.Log($"🛡️ [BattleRulesElements] Attack Resisted! Element: {skillElement}");
        }

        return multiplier;
    }

    /// <summary>
    /// Processes magical or elemental damage scaling rules.
    /// </summary>
    public static int CalculateMagicalDamage(int mAttack, int targetMDefense, CharacterStats.ElementType skillElement, CharacterStats.ElementType targetStrong, CharacterStats.ElementType targetWeak)
    {
        float baseDamage = Mathf.Max(1, mAttack - (targetMDefense * 0.5f));
        float elementMultiplier = GetElementalDamageMultiplier(skillElement, targetStrong, targetWeak);

        return Mathf.RoundToInt(baseDamage * elementMultiplier);
    }
}
