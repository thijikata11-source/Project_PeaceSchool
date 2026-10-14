using UnityEngine;

public class PlayerBlink : MonoBehaviour
{
    [Header("Blink Settings")]
    public SkinnedMeshRenderer faceRenderer;
    public int blinkBlendShapeIndex = 0;
    public float blinkDuration = 0.1f;
    public float blinkHoldTime = 0.05f;
    public Vector2 blinkIntervalRange = new Vector2(1f, 3f);

    [Header("Initial Blink State")]
    [Range(0f, 100f)] public float startingBlinkValue = 20f;

    [Header("Double Blink Settings")]
    [Range(0f, 1f)] public float doubleBlinkChance = 0.3f; // 30% chance
    public float doubleBlinkDelay = 0.15f; // Time between blinks

    [Header("Blink Countdown")]
    public float blinkCountdown; // Time remaining before next blink

    private float blinkTimer;
    private float blinkProgress;
    private bool isBlinking;
    private bool isDoubleBlinkQueued;
    private float nextBlinkTime;

    void Start()
    {
        if (faceRenderer != null)
        {
            faceRenderer.SetBlendShapeWeight(blinkBlendShapeIndex, startingBlinkValue);
        }
        ScheduleNextBlink();
    }

    // Use LateUpdate so we override Animator changes
    void LateUpdate()
    {
        UpdateBlinkLogic();
    }

    private void UpdateBlinkLogic()
    {
        // Use unscaled time so blinking is independent of timescale/physics
        blinkTimer += Time.unscaledDeltaTime;

        // Update countdown if not blinking
        if (!isBlinking)
        {
            blinkCountdown = Mathf.Max(0f, nextBlinkTime - blinkTimer);
        }

        if (!isBlinking && blinkTimer >= nextBlinkTime)
        {
            isBlinking = true;
            blinkTimer = 0f;
        }

        if (isBlinking)
        {
            if (blinkTimer < blinkDuration)
            {
                blinkProgress = Mathf.Lerp(startingBlinkValue, 100f, blinkTimer / blinkDuration);
            }
            else if (blinkTimer < blinkDuration + blinkHoldTime)
            {
                blinkProgress = 100f;
            }
            else if (blinkTimer < blinkDuration * 2 + blinkHoldTime)
            {
                blinkProgress = Mathf.Lerp(100f, startingBlinkValue,
                    (blinkTimer - blinkDuration - blinkHoldTime) / blinkDuration);
            }
            else
            {
                blinkProgress = startingBlinkValue;
                isBlinking = false;
                blinkTimer = 0f;

                // Decide if a double blink should happen
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

            if (faceRenderer != null)
            {
                faceRenderer.SetBlendShapeWeight(blinkBlendShapeIndex, blinkProgress);
            }
        }
    }

    void ScheduleNextBlink()
    {
        nextBlinkTime = Random.Range(blinkIntervalRange.x, blinkIntervalRange.y);
        blinkCountdown = nextBlinkTime;
    }
}
