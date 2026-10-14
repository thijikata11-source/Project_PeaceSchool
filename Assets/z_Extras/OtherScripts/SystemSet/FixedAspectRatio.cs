using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FixedAspectRatio : MonoBehaviour
{
    public float targetAspectWidth = 16f;
    public float targetAspectHeight = 9f;

    private Rect camRect;

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        float targetAspect = targetAspectWidth / targetAspectHeight;
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            // Letterbox (black bars top/bottom)
            camRect = new Rect(0, (1.0f - scaleHeight) / 2.0f, 1.0f, scaleHeight);
        }
        else
        {
            // Pillarbox (black bars left/right)
            float scaleWidth = 1.0f / scaleHeight;
            camRect = new Rect((1.0f - scaleWidth) / 2.0f, 0, scaleWidth, 1.0f);
        }

        cam.rect = camRect;
    }

    void OnGUI()
    {
        // Convert normalized rect to pixel rect
        Rect pixelRect = new Rect(
            camRect.x * Screen.width,
            camRect.y * Screen.height,
            camRect.width * Screen.width,
            camRect.height * Screen.height
        );

        // Draw black bars around the camera rect
        Color oldColor = GUI.color;
        GUI.color = Color.black;

        // Top
        if (pixelRect.y > 0)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, pixelRect.y), Texture2D.blackTexture);

        // Bottom
        float bottomY = pixelRect.y + pixelRect.height;
        if (bottomY < Screen.height)
            GUI.DrawTexture(new Rect(0, bottomY, Screen.width, Screen.height - bottomY), Texture2D.blackTexture);

        // Left
        if (pixelRect.x > 0)
            GUI.DrawTexture(new Rect(0, pixelRect.y, pixelRect.x, pixelRect.height), Texture2D.blackTexture);

        // Right
        float rightX = pixelRect.x + pixelRect.width;
        if (rightX < Screen.width)
            GUI.DrawTexture(new Rect(rightX, pixelRect.y, Screen.width - rightX, pixelRect.height), Texture2D.blackTexture);

        GUI.color = oldColor;
    }
}
