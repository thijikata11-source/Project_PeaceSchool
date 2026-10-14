using UnityEngine;
using UnityEngine.UI;

public class CameraToRawImage : MonoBehaviour
{
    [Header("Camera that renders to the RenderTexture")]
    public Camera sourceCamera;

    [Header("RenderTexture to capture the camera output")]
    public RenderTexture renderTexture;

    [Header("UI RawImage to display the feed")]
    public RawImage targetRawImage;

    void Start()
    {
        // Ensure the camera renders into the RenderTexture
        if (sourceCamera != null && renderTexture != null)
        {
            sourceCamera.targetTexture = renderTexture;
        }

        // Assign the RenderTexture directly to the RawImage
        if (targetRawImage != null && renderTexture != null)
        {
            targetRawImage.texture = renderTexture;
        }
    }
}
