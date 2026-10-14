using UnityEngine;
using System.Collections;

public class PlayerBlinkMaterial : MonoBehaviour
{
    [Header("Blink Materials")]
    public Renderer faceRenderer;     // Drag your character's Renderer here
    public Material openEyesMaterial; // Drag the "open eyes" material
    public Material closeEyesMaterial; // Drag the "closed eyes" material

    [Header("Blink Settings")]
    public float blinkDuration = 0.1f;     // time to keep eyes closed
    public float blinkHoldTime = 0.05f;    // hold closed before reopening
    public Vector2 blinkIntervalRange = new Vector2(1f, 3f); // random interval

    [Header("Double Blink Settings")]
    [Range(0f, 1f)] public float doubleBlinkChance = 0.3f;
    public float doubleBlinkDelay = 0.15f;

    private float blinkTimer;
    private bool isBlinking;
    private bool isDoubleBlinkQueued;
    private float nextBlinkTime;

    void Start()
    {
        if (faceRenderer != null && openEyesMaterial != null)
        {
            faceRenderer.material = openEyesMaterial;
        }
        ScheduleNextBlink();
    }

    void LateUpdate()
    {
        blinkTimer += Time.unscaledDeltaTime;

        if (!isBlinking && blinkTimer >= nextBlinkTime)
        {
            StartCoroutine(BlinkRoutine());
        }
    }

    IEnumerator BlinkRoutine()
    {
        isBlinking = true;
        blinkTimer = 0f;

        // swap to closed eyes
        if (faceRenderer != null && closeEyesMaterial != null)
            faceRenderer.material = closeEyesMaterial;

        yield return new WaitForSecondsRealtime(blinkDuration + blinkHoldTime);

        // swap back to open eyes
        if (faceRenderer != null && openEyesMaterial != null)
            faceRenderer.material = openEyesMaterial;

        isBlinking = false;

        // handle double blink
        if (!isDoubleBlinkQueued && Random.value < doubleBlinkChance)
        {
            isDoubleBlinkQueued = true;
            nextBlinkTime = doubleBlinkDelay;
        }
        else
        {
            isDoubleBlinkQueued = false;
            ScheduleNextBlink();
        }
    }


    void ScheduleNextBlink()
    {
        nextBlinkTime = Random.Range(blinkIntervalRange.x, blinkIntervalRange.y);
    }
}
