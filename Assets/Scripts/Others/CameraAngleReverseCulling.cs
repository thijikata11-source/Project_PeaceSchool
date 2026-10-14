using UnityEngine;

public class CameraAngleReverseCulling : MonoBehaviour
{
    [Header("References")]
    public Camera cam;                 // Assign your Main Camera
    public GameObject[] objectsToCull; // Assign blocking objects

    [Header("Settings")]
    [Range(-1f, 1f)]
    public float baseThreshold = 0f;    // Default cutoff
    public float thresholdShift = 0.5f; // How much to shift depending on rotation

    [Header("Rotation Reference")]
    public Transform rotationTarget;    // Assign your pivot/targetObject

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

            // Each object gets its own dynamic threshold based on rotation
            float dynamicThreshold = baseThreshold + Mathf.Sin(angleY * Mathf.Deg2Rad + obj.transform.eulerAngles.y * Mathf.Deg2Rad) * thresholdShift;

            // Reverse logic: show only if camera is BEHIND (dot < dynamicThreshold)
            rend.enabled = dot < dynamicThreshold;
        }
    }
}
