using UnityEngine;

public class EyeMovement : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMeshRenderer;

    // Material indices
    public int leftEyeIndex = 1;
    public int rightEyeIndex = 9;

    // Max offsets
    public float maxUpOffsetY = -0.03f;
    public float maxDownOffsetY = 0.03f;
    public float maxLeftOffsetX = 0.05f;
    public float maxRightOffsetX = -0.05f;

    // Sensitivity and return speed
    public float sensitivity = 0.01f;
    public float returnSpeed = 5f;

    // Reference to ControlRotateCam
    public ControlRotateCam controlRotateCam;

    private Material leftEyeMat;
    private Material rightEyeMat;

    private Vector2 currentOffset = Vector2.zero;
    private Vector2 targetOffset = Vector2.zero;

    private float lastYRotation;

    void Start()
    {
        if (skinnedMeshRenderer != null)
        {
            Material[] mats = skinnedMeshRenderer.materials;
            if (leftEyeIndex >= 0 && leftEyeIndex < mats.Length)
                leftEyeMat = mats[leftEyeIndex];
            if (rightEyeIndex >= 0 && rightEyeIndex < mats.Length)
                rightEyeMat = mats[rightEyeIndex];
        }

        if (controlRotateCam != null && controlRotateCam.targetObject != null)
        {
            lastYRotation = controlRotateCam.targetObject.transform.eulerAngles.y;
        }
    }

    void Update()
    {
        if (leftEyeMat == null || rightEyeMat == null) return;
        if (controlRotateCam == null || controlRotateCam.targetObject == null) return;

        // Detect rotation delta from ControlRotateCam target
        float currentYRotation = controlRotateCam.targetObject.transform.eulerAngles.y;
        float deltaY = Mathf.DeltaAngle(lastYRotation, currentYRotation);
        lastYRotation = currentYRotation;

        // If rotating, accumulate offset
        if (controlRotateCam.RotateLeft || controlRotateCam.RotateRight)
        {
            targetOffset.x += deltaY * sensitivity;
        }
        else
        {
            // If not rotating, smoothly return to center
            targetOffset = Vector2.Lerp(targetOffset, Vector2.zero, Time.deltaTime * returnSpeed);
        }

        // Clamp to max ranges
        targetOffset.x = Mathf.Clamp(targetOffset.x, maxRightOffsetX, maxLeftOffsetX);
        targetOffset.y = Mathf.Clamp(targetOffset.y, maxUpOffsetY, maxDownOffsetY);

        // Smoothly move current offset toward target
        currentOffset = Vector2.Lerp(currentOffset, targetOffset, Time.deltaTime * returnSpeed);

        // Apply same offset to both eyes
        leftEyeMat.mainTextureOffset = currentOffset;
        rightEyeMat.mainTextureOffset = currentOffset;
    }
}
