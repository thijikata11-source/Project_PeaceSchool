using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats Asset")]
    [Tooltip("Drag the EnemyStats ScriptableObject asset here")]
    public EnemyStats baseStats;

    // Runtime variables (so individual enemies can lose health, etc.)
    [Header("Runtime State")]
    public string currentName;
    public int currentHP;
    public int currentLevel;

    private void Awake()
    {
        if (baseStats != null)
        {
            InitializeEnemy();
        }
        else
        {
            Debug.LogWarning($"EnemyStats asset is missing on {gameObject.name}!", gameObject);
        }
    }

    // Copies data from the scriptable object to this specific enemy instance
    public void InitializeEnemy()
    {
        currentName = baseStats.enemyName;
        currentLevel = baseStats.enemyLevel;
        currentHP = baseStats.enemyHP;

        // You can access other static stats directly via: baseStats.enemySpeed, baseStats.enemyAttackDamage, etc.
        Debug.Log($"{currentName} (Lv. {currentLevel}) initialized with {currentHP} HP!");
    }

    // Example function for taking damage
    public void TakeDamage(int amount)
    {
        // Simple mitigation formula example
        float damageReduction = baseStats.enemyDefense * 0.1f;
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(amount - damageReduction));

        currentHP -= finalDamage;
        Debug.Log($"{currentName} took {finalDamage} damage! Remaining HP: {currentHP}");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{currentName} has been defeated!");
        Destroy(gameObject);
    }
}
