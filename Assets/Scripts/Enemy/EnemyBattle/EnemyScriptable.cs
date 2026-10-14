using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Battle/EnemyScriptable")]
public class EnemyScriptable : ScriptableObject
{
    [Header("Base Assets & Info")]
    public GameObject enemyGameObject;
    public string enemyName;
    public int enemyLevel = 1; // Used to compute party vs enemy dynamic balance algorithms

    [Header("Stats")]
    public int enemyHp;
    public int enemyMana;
    public int enemyStamina;
    public string enemyElement;

    [Header("Inventory & Loadout")]
    [Tooltip("Up to 8 active skills")]
    public string[] enemySkills = new string[8];
    [Tooltip("Up to 8 active items")]
    public string[] enemyItems = new string[8];
}
