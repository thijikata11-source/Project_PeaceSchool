using UnityEngine;

public class SeasonalObjectsChanger : MonoBehaviour
{
    [Header("Seasonal Materials")]
    public Material springMaterial;
    public Material summerMaterial;
    public Material autumnMaterial;
    public Material winterMaterial;

    [Header("Material Slot Settings")]
    [Tooltip("Which material index to replace (0 or 1).")]
    [Range(0, 1)] public int materialIndex = 0;

    private Renderer objectRenderer;
    private GlobalTimeSystem timeSystem;
    private int lastSeasonIndex = -1; // track changes

    private void Awake()
    {
        // Automatically grab the Renderer from this object
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogError($"{name} has no Renderer component!");
        }
    }

    private void Start()
    {
        // Find the GlobalTimeSystem in the scene
        timeSystem = FindObjectOfType<GlobalTimeSystem>();
        if (timeSystem == null)
        {
            Debug.LogError("GlobalTimeSystem not found in scene!");
            return;
        }

        UpdateSeasonalMaterial(); // Initialize once
    }

    private void Update()
    {
        if (timeSystem == null || objectRenderer == null) return;

        // Only update when season changes
        if (lastSeasonIndex != timeSystem.CurrentSeasonIndex)
        {
            UpdateSeasonalMaterial();
        }
    }

    private void UpdateSeasonalMaterial()
    {
        if (objectRenderer == null || timeSystem == null) return;

        Material[] mats = objectRenderer.materials;

        switch (timeSystem.CurrentSeasonIndex)
        {
            case 0: mats[materialIndex] = springMaterial; break;
            case 1: mats[materialIndex] = summerMaterial; break;
            case 2: mats[materialIndex] = autumnMaterial; break;
            case 3: mats[materialIndex] = winterMaterial; break;
        }

        objectRenderer.materials = mats;
        lastSeasonIndex = timeSystem.CurrentSeasonIndex;
    }
}
