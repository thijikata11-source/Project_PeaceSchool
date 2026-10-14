using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class Mirror : MonoBehaviour
{
    public Camera mirrorCamera;
    public RenderTexture mirrorTexture;

    void Start()
    {
        if (mirrorCamera != null && mirrorTexture != null)
            mirrorCamera.targetTexture = mirrorTexture;

        // Assign texture to material
        var mr = GetComponent<MeshRenderer>();
        if (mr && mr.material)
            mr.material.mainTexture = mirrorTexture;
    }

    void LateUpdate()
    {
        if (!mirrorCamera) return;

        // Just render from mirrorCamera’s current transform
        mirrorCamera.Render();
    }
}
