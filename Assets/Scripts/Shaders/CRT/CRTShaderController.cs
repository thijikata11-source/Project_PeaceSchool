using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CRTShaderController : MonoBehaviour
{
    public Material crtMaterial;
    public float scanlineSpeed = 2.0f;
    public float scanlineIntensity = 0.5f;
    public int scanlineCount = 400; // Adjustable number of scanlines

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (crtMaterial != null)
        {
            crtMaterial.SetFloat("_ScanlineSpeed", scanlineSpeed);
            crtMaterial.SetFloat("_ScanlineIntensity", scanlineIntensity);
            crtMaterial.SetInt("_ScanlineCount", scanlineCount);
            Graphics.Blit(src, dest, crtMaterial);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}
