using UnityEngine;

[DisallowMultipleComponent]
public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("How strong the shake is.")]
    public float amplitude = 0.2f;

    [Tooltip("How quickly the shake oscillates.")]
    public float frequency = 25f;

    [Tooltip("How long the shake lasts (seconds).")]
    public float duration = 0.3f;

    [Tooltip("If true, shake ignores Time.timeScale.")]
    public bool unscaledTime = true;

    private float remainingTime;
    private float seed;
    private Vector3 originalPos;
    private Quaternion originalRot;
    public Transform cam;

    void Awake()
    {
        cam = GetComponent<Transform>();
        originalPos = cam.localPosition;
        originalRot = cam.localRotation;
        seed = Random.value * 100f;
    }

    void LateUpdate()
    {
        if (remainingTime > 0f)
        {
            float dt = unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            remainingTime -= dt;

            float t = (unscaledTime ? Time.unscaledTime : Time.time) * frequency + seed;

            // Perlin noise for smooth shake
            float offsetX = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * amplitude;
            float offsetY = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * amplitude;
            float rotZ = (Mathf.PerlinNoise(t, t) - 0.5f) * 2f * amplitude * 15f;

            cam.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);
            cam.localRotation = originalRot * Quaternion.Euler(0f, 0f, rotZ);
        }
        else
        {
            // Reset when done
            cam.localPosition = originalPos;
            cam.localRotation = originalRot;
        }
    }

    /// <summary>
    /// Triggers a shake with current inspector values.
    /// </summary>
    public void Shake()
    {
        remainingTime = duration;
    }

    /// <summary>
    /// Triggers a shake with custom values.
    /// </summary>
    public void Shake(float customDuration, float customAmplitude, float customFrequency)
    {
        duration = customDuration;
        amplitude = customAmplitude;
        frequency = customFrequency;
        remainingTime = duration;
    }
}
