using System.Collections.Generic;
using UnityEngine;

public class MultiTargetIndicatorManager : MonoBehaviour
{
    public static MultiTargetIndicatorManager Instance;

    [Header("Indicator Configuration")]
    [Tooltip("Drag your targetSigil prefab here from your project folder.")]
    public GameObject indicatorPrefab;
    public Vector3 offset = Vector3.up * 2f;

    private List<GameObject> activeIndicators = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Clones the target sigil prefab and parents it directly to each selected enemy.
    /// </summary>
    public void ShowIndicatorsOnTargets(List<GameObject> targets)
    {
        ClearAllIndicators();

        if (indicatorPrefab == null || targets == null) return;

        foreach (GameObject target in targets)
        {
            if (target == null) continue;

            // 🛠️ CLONE THE SIGIL PREFAB DIRECTLY
            GameObject clonedIndicator = Instantiate(indicatorPrefab);

            // 🛠️ PARENT IT IMMEDIATELY to stop it from drifting away in global space
            clonedIndicator.transform.SetParent(target.transform, false);

            // 1. Start with your default baseline inspector offset height (e.g., 2.0f)
            float calculatedHeight = offset.y + 0.2f;

            Collider col = target.GetComponent<Collider>();
            if (col == null) col = target.GetComponentInChildren<Collider>();

            // 2. 🛠️ SMART SAFEGUARD: Only use the collider bounds if it's found AND actually has a height greater than 0!
            if (col != null && col.bounds.size.y > 0.1f)
            {
                calculatedHeight = col.bounds.size.y + 0.2f;
            }
            else
            {
                // Fallback metric if the collider evaluates to 0 or doesn't exist
                // Forces the Y-axis position to exactly 2.22f based on your setup preferences
                calculatedHeight = 2.22f;
            }

            // Apply positions locally relative to the enemy parent transform pivot point
            clonedIndicator.transform.localPosition = new Vector3(0f, calculatedHeight, 0f);
            clonedIndicator.transform.localRotation = Quaternion.identity;

            activeIndicators.Add(clonedIndicator);
        }
    }

    /// <summary>
    /// Safely clears and purges active indicators from hierarchy memory.
    /// </summary>
    public void ClearAllIndicators()
    {
        foreach (GameObject ind in activeIndicators)
        {
            if (ind != null) Destroy(ind);
        }
        activeIndicators.Clear();
    }
}
