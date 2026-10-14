using UnityEngine;

public class CullMaskTwoWays : MonoBehaviour
{
    [Header("References")]
    public Camera cam;                 // Assign your Main Camera
    public GameObject[] objectsToCull; // Assign blocking objects

    [Header("Thresholds")]
    [Range(-1f, 1f)]
    public float leftBaseThreshold = -0.2f;   // Base cutoff when camera rotates left
    [Range(-1f, 1f)]
    public float rightBaseThreshold = 0.2f;   // Base cutoff when camera rotates right

    [Header("Shift Settings")]
    public float thresholdShift = 0.5f;       // Dynamic shift amount

    [Header("Rotation Reference")]
    public Transform rotationTarget;          // Assign your pivot/targetObject

    void Update()
    {
        float angleY = rotationTarget.eulerAngles.y;

        foreach (GameObject obj in objectsToCull)
        {
            if (obj == null) continue;

            Renderer rend = obj.GetComponent<Renderer>();
            if (rend == null) continue;

            // Direction from object to camera
            Vector3 toCam = (cam.transform.position - obj.transform.position).normalized;

            // Use object's forward vector as "front face"
            float dot = Vector3.Dot(obj.transform.forward, toCam);

            // Decide which base threshold to use depending on rotation side
            bool isLeftSide = Mathf.Sin(angleY * Mathf.Deg2Rad) < 0f;
            float baseThreshold = isLeftSide ? leftBaseThreshold : rightBaseThreshold;

            // Apply dynamic shift based on rotation angle
            float dynamicThreshold = baseThreshold + Mathf.Sin(angleY * Mathf.Deg2Rad) * thresholdShift;

            // Show only if camera is in front (dot > dynamicThreshold)
            rend.enabled = dot > dynamicThreshold;
        }
    }
}
