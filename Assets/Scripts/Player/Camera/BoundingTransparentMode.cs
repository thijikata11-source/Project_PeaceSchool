using UnityEngine;
using System.Collections.Generic;

public class BoundingTransparentMode : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform cameraPivot;

    [Header("General Obstacle Settings")]
    public LayerMask obstacleLayer;
    public float sphereRadius = 0.5f;

    [Header("Separate Tree Settings")]
    public LayerMask treeLayer;
    [Tooltip("Independent radius for detecting trees. Make it larger if you want trees to vanish sooner.")]
    public float treeSphereRadius = 1.0f;

    [Header("Transparency Range Settings")]
    [Tooltip("Transparency value when a standard object is right next to the player.")]
    public float nearTransparency = 0.224f;
    [Tooltip("Transparency value when the object is completely faded (used for camera pivot or trees).")]
    public float farTransparency = 0.0f;

    [Header("Materials")]
    public Material solidMaterial;       // Assign your LitSolid shader here
    public Material transparentMaterial; // Assign your LitTransparentAdjustable shader here

    // Helper class to store original solid materials for restoration
    private class ObstacleData
    {
        public Material[] originalMaterials;
        public Material[] runtimeTransparentMaterials;
    }

    // Tracks currently faded objects and their material records
    private Dictionary<Renderer, ObstacleData> activeObstacles = new Dictionary<Renderer, ObstacleData>();

    void LateUpdate()
    {
        if (player == null || cameraPivot == null) return;

        Vector3 pivotPos = cameraPivot.position;
        Vector3 playerPos = player.position;
        Vector3 dir = playerPos - pivotPos;
        float maxDistance = dir.magnitude;
        Vector3 normalizedDir = dir.normalized;

        HashSet<Renderer> currentHitRenderers = new HashSet<Renderer>();

        // ==========================================
        // 1. SPHERECAST FOR REGULAR OBSTACLES
        // ==========================================
        RaycastHit[] obstacleHits = Physics.SphereCastAll(pivotPos, sphereRadius, normalizedDir, maxDistance, obstacleLayer);
        foreach (RaycastHit hit in obstacleHits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                currentHitRenderers.Add(rend);
                SetupRuntimeMaterials(rend);

                // Calculate standard distance-based transparency
                float currentDistToPlayer = Vector3.Distance(hit.collider.transform.position, playerPos);
                float t = Mathf.Clamp01(currentDistToPlayer / maxDistance);
                float calculatedTransparency = Mathf.Lerp(nearTransparency, farTransparency, t);

                ApplyTransparency(rend, calculatedTransparency);
            }
        }

        // ==========================================
        // 2. SEPARATE SPHERECAST FOR TREES
        // ==========================================
        RaycastHit[] treeHits = Physics.SphereCastAll(pivotPos, treeSphereRadius, normalizedDir, maxDistance, treeLayer);
        foreach (RaycastHit hit in treeHits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                currentHitRenderers.Add(rend);
                SetupRuntimeMaterials(rend);

                // Trees always fade out completely, skipping distance math
                ApplyTransparency(rend, farTransparency);
            }
        }

        // ==========================================
        // 3. CLEANUP AND MATERIAL RESTORATION
        // ==========================================
        List<Renderer> toRestore = new List<Renderer>();
        foreach (var pair in activeObstacles)
        {
            Renderer rend = pair.Key;
            if (!currentHitRenderers.Contains(rend))
            {
                if (rend != null)
                {
                    rend.materials = pair.Value.originalMaterials;
                }

                foreach (Material mat in pair.Value.runtimeTransparentMaterials)
                {
                    Destroy(mat);
                }

                toRestore.Add(rend);
            }
        }

        foreach (Renderer rend in toRestore)
        {
            activeObstacles.Remove(rend);
        }
    }

    // Helper to register new objects and instantiate runtime transparent materials
    private void SetupRuntimeMaterials(Renderer rend)
    {
        if (!activeObstacles.ContainsKey(rend))
        {
            ObstacleData data = new ObstacleData();
            data.originalMaterials = rend.sharedMaterials;

            Material[] newMats = new Material[data.originalMaterials.Length];
            for (int i = 0; i < newMats.Length; i++)
            {
                newMats[i] = new Material(transparentMaterial);
            }
            rend.materials = newMats;
            data.runtimeTransparentMaterials = rend.materials;

            activeObstacles.Add(rend, data);
        }
    }

    // Helper to push the calculated transparency to the custom shader property
    private void ApplyTransparency(Renderer rend, float alpha)
    {
        if (activeObstacles.TryGetValue(rend, out ObstacleData currentData))
        {
            foreach (Material mat in currentData.runtimeTransparentMaterials)
            {
                if (mat.HasProperty("_Transparency"))
                {
                    mat.SetFloat("_Transparency", alpha);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (player != null && cameraPivot != null)
        {
            // Baseline green visibility line
            Gizmos.color = Color.green;
            Gizmos.DrawLine(cameraPivot.position, player.position);

            // Obstacle wire sphere (White)
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(cameraPivot.position, sphereRadius);

            // Tree wire sphere (Red)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(cameraPivot.position, treeSphereRadius);
        }
    }
}
