using UnityEngine;

public static class BattleRulesStats
{
    /// <summary>
    /// Evaluates if an attack successfully hits its target based on accuracy and agility stats.
    /// </summary>
    public static bool CheckIfAttackHits(float attackerAccuracy, float targetAgility, float targetDodgeChance)
    {
        // Absolute base evasion pool combining target's direct dodge and agility metrics
        float totalEvasionPool = targetDodgeChance + (targetAgility * 0.1f);

        // Dynamic accuracy roll offset by the defender's dodge threshold
        float hitProbability = Mathf.Clamp(attackerAccuracy - totalEvasionPool, 5f, 100f);
        float randomRoll = Random.Range(0f, 100f);

        return randomRoll <= hitProbability;
    }

    /// <summary>
    /// Evaluates if an attack lands as a critical strike based on the attacker's critical stat.
    /// </summary>
    public static bool CheckIfCriticalHit(float attackerCritical)
    {
        float randomRoll = Random.Range(0f, 100f);
        return randomRoll <= attackerCritical;
    }

    /// <summary>
    /// Calculates raw base physical damage mitigation.
    /// </summary>
    public static int CalculatePhysicalDamage(int pAttack, int targetPDefense, bool isCritical)
    {
        float baseDamage = Mathf.Max(1, pAttack - (targetPDefense * 0.5f));

        if (isCritical)
        {
            baseDamage *= 1.5f; // Standard 150% critical damage multiplier
            Debug.Log("💥 [BattleRulesStats] Critical Strike Landed!");
        }

        return Mathf.RoundToInt(baseDamage);
    }
}
