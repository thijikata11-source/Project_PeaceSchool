using UnityEngine;

public class CameraAngleCullingByLayer : MonoBehaviour
{
    [Header("References")]
    public Camera cam;                  // Assign your Main Camera

    [Header("Settings")]
    [Range(-1f, 1f)]
    public float baseThreshold = 0f;    // Default cutoff
    public float thresholdShift = 0.5f; // How much to shift depending on rotation

    [Header("Rotation Reference")]
    public Transform rotationTarget;    // Assign your pivot/targetObject

    [Header("Layer Settings")]
    public LayerMask cullLayer;         // Choose the layer to cull

    void Update()
    {
        float angleY = rotationTarget.eulerAngles.y;

        // Find all objects in the specified layer
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & cullLayer) == 0) continue; // Skip if not in layer

            Renderer rend = obj.GetComponent<Renderer>();
            if (rend == null) continue;

            // Direction from object to camera
            Vector3 toCam = (cam.transform.position - obj.transform.position).normalized;

            // Use object's forward vector as "front face"
            float dot = Vector3.Dot(obj.transform.forward, toCam);

            // Dynamic threshold based on rotation
            float dynamicThreshold = baseThreshold +
                Mathf.Sin(angleY * Mathf.Deg2Rad + obj.transform.eulerAngles.y * Mathf.Deg2Rad) * thresholdShift;

            // Show only if camera is in front (dot > dynamicThreshold)
            rend.enabled = dot > dynamicThreshold;
        }
    }
}
