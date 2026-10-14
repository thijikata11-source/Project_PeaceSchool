using UnityEngine;
using System.Collections.Generic;

public class ScriptToggleOnActive : MonoBehaviour
{
    [Header("The GameObject to watch")]
    public GameObject target;

    [Header("Objects to disable when Target is ACTIVE")]
    public List<GameObject> disableWhenActive = new List<GameObject>();

    [Header("Objects to enable when Target is INACTIVE")]
    public List<GameObject> enableWhenInactive = new List<GameObject>();

    private bool lastState;

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning($"{name}: No target assigned!");
            enabled = false;
            return;
        }

        lastState = target.activeSelf;
        ApplyState(lastState);
    }

    void Update()
    {
        if (target.activeSelf != lastState)
        {
            lastState = target.activeSelf;
            ApplyState(lastState);
        }
    }

    private void ApplyState(bool isActive)
    {
        // Disable these when target is active
        foreach (var obj in disableWhenActive)
        {
            if (obj != null) obj.SetActive(!isActive);
        }

        // Enable these when target is inactive
        foreach (var obj in enableWhenInactive)
        {
            if (obj != null) obj.SetActive(!isActive);
        }
    }
}
