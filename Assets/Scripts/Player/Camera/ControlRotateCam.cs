using UnityEngine;

public class ControlRotateCam : MonoBehaviour
{
    [Header("Target to Rotate")]
    public GameObject targetObject;   // Assign in Inspector

    [Header("Rotation Settings")]
    public float rotationSpeed = 90f; // degrees per second
    public float snapSpeed = 180f;     // Speed when automatically snapping to nearest angle

    [Header("Mode Configuration")]
    [Tooltip("True = Dynamic Snapping Mode (Calculates closest angle relative to current physical position).\nFalse = Fixed Step Mode (Strictly advances sequential target index regardless of drift).")]
    public bool useDynamicSnappingMode = true;

    // This is the exact sequence shared by both buttons
    // Index:      0       1        2        3
    // Angles:   [45] <-> [145] <-> [-145] <-> [-45]
    private readonly float[] targetAngles = { 45f, 145f, -145f, -45f };
    private int currentIndex = 0; // Tracks the single shared position in the sequence

    private bool rotateLeft = false;
    private bool rotateRight = false;
    private bool isSnapping = false; // Tracks if we are automatically moving to a sequence target

    // Public read-only access
    public bool RotateLeft => rotateLeft;
    public bool RotateRight => rotateRight;

    void Start()
    {
        // Initialize the target to the first angle (45) when the game starts
        SnapToCurrentSequenceIndex();
    }

    void Update()
    {
        if (targetObject == null) return;

        // 1. Manual Continuous Controls (Holding Left or Right)
        if (rotateLeft)
        {
            isSnapping = false; // Override any automatic snapping immediately
            targetObject.transform.Rotate(0f, -rotationSpeed * Time.deltaTime, 0f, Space.World);
        }
        else if (rotateRight)
        {
            isSnapping = false; // Override any automatic snapping immediately
            targetObject.transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
        }
        // 2. Automatic Sequence Snapping Control Loop
        else if (isSnapping)
        {
            // Calculate target rotation based on current sequence index array data
            float targetY = targetAngles[currentIndex];
            Quaternion targetRotation = Quaternion.Euler(targetObject.transform.localEulerAngles.x, targetY, targetObject.transform.localEulerAngles.z);

            // Move smoothly toward the chosen target angle orientation
            targetObject.transform.localRotation = Quaternion.RotateTowards(
                targetObject.transform.localRotation,
                targetRotation,
                snapSpeed * Time.deltaTime
            );

            // Turn off snapping once target position has been reached
            if (Quaternion.Angle(targetObject.transform.localRotation, targetRotation) < 0.01f)
            {
                targetObject.transform.localRotation = targetRotation; // Lock final angle transformation perfectly
                isSnapping = false;
            }
        }
    }

    // Public UI Method to toggle modes via UI Checkboxes/Toggles or Buttons
    public void ToggleRotationMode()
    {
        useDynamicSnappingMode = !useDynamicSnappingMode;
        Debug.Log($"Rotation Mode Changed! Dynamic Snapping is now: {useDynamicSnappingMode}");
    }

    // Explicitly set the mode from external elements (UI Toggle component passing an explicit bool)
    public void SetDynamicSnappingMode(bool status)
    {
        useDynamicSnappingMode = status;
    }

    // Left manual button press/hold
    public void ControlLeftDown() => rotateLeft = true;

    // Right manual button press/hold
    public void ControlRightDown() => rotateRight = true;

    // Called when manual Left button released
    public void ControlLeftUp()
    {
        rotateLeft = false;
        HandleManualRelease();
    }

    // Called when manual Right button released
    public void ControlRightUp()
    {
        rotateRight = false;
        HandleManualRelease();
    }

    // Decides how to respond when manual drift buttons are released
    private void HandleManualRelease()
    {
        if (useDynamicSnappingMode)
        {
            FindNearestAngleIndexOnly();
            isSnapping = true;
        }
        else
        {
            // Fixed Mode: Just hold current physical position upon manual control release.
            // Snapping won't begin until a sequence step button is explicitly clicked.
            isSnapping = false;
        }
    }

    // Called on click from Clockwise UI Button
    public void RotateClockwise()
    {
        if (useDynamicSnappingMode)
        {
            // Mode A: Context-aware shift relative to where the camera currently is sitting
            if (!isSnapping && !rotateLeft && !rotateRight)
            {
                FindNearestAngleIndexOnly();
            }
        }
        // Mode B (Fixed Step Mode): Ignores physical position drift, strictly increments index array loops

        currentIndex = (currentIndex + 1) % targetAngles.Length;
        isSnapping = true;
    }

    // Called on click from Counter-Clockwise UI Button
    public void RotateCounterClockwise()
    {
        if (useDynamicSnappingMode)
        {
            // Mode A: Context-aware shift relative to where the camera currently is sitting
            if (!isSnapping && !rotateLeft && !rotateRight)
            {
                FindNearestAngleIndexOnly();
            }
        }
        // Mode B (Fixed Step Mode): Ignores physical position drift, strictly decrements index array loops

        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = targetAngles.Length - 1;
        }
        isSnapping = true;
    }

    // Helper updates sequence index data based on current object physical rotation status
    private void FindNearestAngleIndexOnly()
    {
        if (targetObject == null) return;

        float currentY = targetObject.transform.localEulerAngles.y;

        // Normalize angle out-of-bounds calculations to 0-360 range space fields
        if (currentY < 0) currentY += 360f;

        float minAngleDiff = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < targetAngles.Length; i++)
        {
            float checkAngle = targetAngles[i];
            if (checkAngle < 0) checkAngle += 360f;

            // Calculate shortest path delta between the two relative circular points
            float diff = Mathf.Abs(Mathf.DeltaAngle(currentY, checkAngle));
            if (diff < minAngleDiff)
            {
                minAngleDiff = diff;
                closestIndex = i;
            }
        }
        currentIndex = closestIndex;
    }

    // Helper method to completely hard-lock structural transform positions instantly on run initialization sequences
    private void SnapToCurrentSequenceIndex()
    {
        if (targetObject == null) return;
        Vector3 currentRotation = targetObject.transform.localEulerAngles;
        currentRotation.y = targetAngles[currentIndex];
        targetObject.transform.localEulerAngles = currentRotation;
    }
}
