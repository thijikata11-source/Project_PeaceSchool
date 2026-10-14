using System.Collections.Generic;
using UnityEngine;

public class CharacterToggleManager : MonoBehaviour
{
    [Header("Scripts to Monitor")]
    [Tooltip("If ANY of these scripts are enabled, the target GameObjects will turn OFF.")]
    [SerializeField] private List<MonoBehaviour> monitoringScripts;

    [Header("Objects to Toggle")]
    [Tooltip("These entire GameObjects will be turned ON/OFF on every frame.")]
    [SerializeField] private List<GameObject> objectsToToggle;

    void Update()
    {
        // 1. Check if ANY script in the monitoring list is enabled
        bool isAnyScriptEnabled = false;

        foreach (MonoBehaviour script in monitoringScripts)
        {
            // Note: If the monitored script's GameObject is turned off completely, 
            // script.enabled will evaluate to false.
            if (script != null && script.enabled)
            {
                isAnyScriptEnabled = true;
                break; // Found one active script, stop checking
            }
        }

        // 2. Determine the desired state for the GameObjects
        // If a monitored script IS enabled, objects should be OFF (false). Otherwise, ON (true).
        bool desiredObjectState = !isAnyScriptEnabled;

        // 3. Always update the GameObjects every frame (state caching removed)
        SetListActiveState(desiredObjectState);
    }

    private void SetListActiveState(bool state)
    {
        if (objectsToToggle == null) return;

        foreach (GameObject obj in objectsToToggle)
        {
            if (obj != null)
            {
                obj.SetActive(state);
            }
        }
    }
}
