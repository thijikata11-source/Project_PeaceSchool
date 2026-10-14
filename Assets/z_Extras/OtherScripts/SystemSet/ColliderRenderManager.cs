using UnityEngine;
using System.Collections.Generic;

public class ColliderRenderManager : MonoBehaviour
{
    [Header("Assign the Box Collider here")]
    public BoxCollider targetCollider;

    [Header("Drag parent GameObjects here")]
    public List<GameObject> parentObjects = new List<GameObject>();

    // Internal list of active renderers collected from parents
    private List<Renderer> renderersToCheck = new List<Renderer>();

    void Start()
    {
        CollectChildRenderers();
    }

    void Update()
    {
        if (targetCollider == null) return;

        Bounds bounds = targetCollider.bounds;

        foreach (Renderer r in renderersToCheck)
        {
            if (r == null) continue;

            // Check if the renderer's bounds intersect with the collider
            bool inside = bounds.Intersects(r.bounds);

            // Toggle renderer based on intersection
            r.enabled = inside;
        }
    }

    // Collect only enabled child renderers from the parentObjects list
    void CollectChildRenderers()
    {
        renderersToCheck.Clear();

        foreach (GameObject parent in parentObjects)
        {
            if (parent == null) continue;

            Renderer[] childRenderers = parent.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer r in childRenderers)
            {
                if (r.enabled) // ✅ only collect active renderers
                {
                    renderersToCheck.Add(r);
                }
            }
        }
    }

    // Refresh manually from inspector
    [ContextMenu("Refresh Child Renderers")]
    void RefreshRenderers()
    {
        CollectChildRenderers();
    }
}
