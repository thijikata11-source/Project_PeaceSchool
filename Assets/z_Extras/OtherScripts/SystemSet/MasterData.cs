using UnityEngine;

/// <summary>
/// MasterData ensures the GameObject persists across scenes
/// and cannot be destroyed unless explicitly allowed.
/// </summary>
public class MasterData : MonoBehaviour
{
    [Header("Persistence Settings")]
    [Tooltip("If true, this GameObject will not be destroyed.")]
    [SerializeField] private bool indestructible = true;

    private static MasterData instance;

    void Awake()
    {
        // Singleton pattern to ensure only one MasterData exists
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"Duplicate MasterData detected on {name}. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (indestructible)
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log($"{name} marked as indestructible and will persist across scenes.");
        }
    }

    /// <summary>
    /// Attempts to destroy this GameObject.
    /// Will only succeed if indestructible is false.
    /// </summary>
    public void TryDestroy()
    {
        if (!indestructible)
        {
            Debug.Log($"{name} is allowed to be destroyed.");
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"{name} is indestructible and cannot be destroyed!");
        }
    }

    /// <summary>
    /// Allows toggling indestructibility at runtime.
    /// </summary>
    public void SetIndestructible(bool value)
    {
        indestructible = value;
        Debug.Log($"{name} indestructible set to {indestructible}");
    }
}
