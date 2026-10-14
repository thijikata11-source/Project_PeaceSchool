using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyStats", menuName = "ScriptableObjects/EnemyStats")]
public class EnemyStats : ScriptableObject
{
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

    [Header("Identity & Core Stats")]
    public string enemyName;
    public int enemyLevel;
    public int enemyHP;

    [Header("Turn & Combat Multipliers")]
    [Tooltip("Decides turn order in the queue; faster units act earlier or more often.")]
    public float enemySpeed;
    [Tooltip("Chance to easily evade attacks.")]
    public float enemyAgility;
    [Tooltip("Chance for accurate attacks, without miss.")]
    public float enemyAccuracy;
    [Tooltip("Chance for multiplying attack damage.")]
    public float enemyCritical;
    [Tooltip("Chance to avoid an incoming attack entirely.")]
    public float enemyEvasion;

    [Header("Elements (Select Multiple)")]
    public ElementType enemyElement;
    public ElementType enemyElementWeakness;

    [Header("Base Combat Stats")]
    public float enemyAttackDamage;
    public float enemyDefense;
    public float enemyElementalDamage;
    public float enemyElementalDefense;
}
