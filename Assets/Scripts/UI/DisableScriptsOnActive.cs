using System.Collections.Generic;
using UnityEngine;

public class DisableScriptsOnActive : MonoBehaviour
{
    [Header("Scripts to disable when this GameObject is active")]
    public List<MonoBehaviour> scriptsToToggle = new List<MonoBehaviour>();

    [Header("GameObjects to disable when this GameObject is active")]
    public List<GameObject> gameObjectsToDisable = new List<GameObject>();

    // Internal memory of original states
    private Dictionary<GameObject, bool> originalStates = new Dictionary<GameObject, bool>();

    void OnEnable()
    {
        SetScriptsEnabled(false);

        // Save original states and disable
        originalStates.Clear();
        foreach (var go in gameObjectsToDisable)
        {
            if (go != null)
            {
                originalStates[go] = go.activeSelf;
                go.SetActive(false);
            }
        }

        Debug.Log($"[DisableScriptsOnActive] => Disabled {scriptsToToggle.Count} scripts and {gameObjectsToDisable.Count} GameObjects");
    }

    void OnDisable()
    {
        SetScriptsEnabled(true);

        // Restore original states
        foreach (var kvp in originalStates)
        {
            if (kvp.Key != null)
            {
                kvp.Key.SetActive(kvp.Value);
            }
        }

        Debug.Log($"[DisableScriptsOnActive] => Restored {gameObjectsToDisable.Count} GameObjects to original states");
    }

    private void SetScriptsEnabled(bool enabled)
    {
        foreach (var script in scriptsToToggle)
        {
            if (script != null)
            {
                script.enabled = enabled;
            }
        }
    }
}
