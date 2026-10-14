using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MobileSafeAreaHandler : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea = Rect.zero;
    private Vector2 lastScreenSize = Vector2.zero;
    private ScreenOrientation lastOrientation = ScreenOrientation.Unknown;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Force the anchor configurations to completely stretch out to full screen limits natively
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        ApplySafeAreaCalculations();
    }

    void Update()
    {
        // Continuously check if the screen size, orientation, or hardware safe zones changed during runtime
        if (Screen.safeArea != lastSafeArea ||
            new Vector2(Screen.width, Screen.height) != lastScreenSize ||
            Screen.orientation != lastOrientation)
        {
            ApplySafeAreaCalculations();
        }
    }

    private void ApplySafeAreaCalculations()
    {
        Rect safeArea = Screen.safeArea;

        // Cache the layout configuration properties to avoid recalculating frames needlessly
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        // Convert the raw pixel dimensions relative into proportional matrix percentages
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // Snap the container bounding borders to sit strictly within hardware boundaries
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        Debug.Log($"📱 Safe Area adjusted completely: Min({anchorMin.x:F2}, {anchorMin.y:F2}) | Max({anchorMax.x:F2}, {anchorMax.y:F2})");
    }
}
