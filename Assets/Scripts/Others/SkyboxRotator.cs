using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Degrees per second for skybox rotation")]
    public float rotationSpeed = 1.0f;

    // Internal rotation tracker
    private float currentRotation = 0f;

    void Update()
    {
        // Increase rotation based on time and speed
        currentRotation += rotationSpeed * Time.deltaTime;

        // Keep it clean (0–360 range)
        if (currentRotation > 360f) currentRotation -= 360f;

        // Apply rotation to the skybox material
        if (RenderSettings.skybox.HasProperty("_Rotation"))
        {
            RenderSettings.skybox.SetFloat("_Rotation", currentRotation);
        }

        // Optional: update global illumination so lighting matches
        DynamicGI.UpdateEnvironment();
    }
}
