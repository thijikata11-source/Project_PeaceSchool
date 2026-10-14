using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeMaterialController : MonoBehaviour
{
    [Header("Target Renderer")]
    public SkinnedMeshRenderer skinnedMeshRenderer;

    [Header("Material Index to Control")]
    public int materialIndex = 0; // Which material in the array to modify

    [Header("Offset Settings")]
    public float smoothSpeed = 5f; // How quickly the offset interpolates

    private Vector2 targetOffset;
    private Vector2 currentOffset;

    void Update()
    {
        if (skinnedMeshRenderer == null || materialIndex >= skinnedMeshRenderer.materials.Length)
            return;

        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Horizontal rotation → X offset
        if (mouseX < 0) // rotate left
            targetOffset.x = Mathf.Lerp(targetOffset.x, 0.09f, Time.deltaTime * smoothSpeed);
        else if (mouseX > 0) // rotate right
            targetOffset.x = Mathf.Lerp(targetOffset.x, -0.07f, Time.deltaTime * smoothSpeed);
        else
            targetOffset.x = Mathf.Lerp(targetOffset.x, 0f, Time.deltaTime * smoothSpeed);

        // Vertical rotation → Y offset
        if (mouseY > 0) // rotate upward
            targetOffset.y = Mathf.Lerp(targetOffset.y, -0.05f, Time.deltaTime * smoothSpeed);
        else if (mouseY < 0) // rotate downward
            targetOffset.y = Mathf.Lerp(targetOffset.y, 0.06f, Time.deltaTime * smoothSpeed);
        else
            targetOffset.y = Mathf.Lerp(targetOffset.y, 0f, Time.deltaTime * smoothSpeed);

        // Smoothly interpolate current offset toward target
        currentOffset = Vector2.Lerp(currentOffset, targetOffset, Time.deltaTime * smoothSpeed);

        // Apply offset to chosen material
        Material[] mats = skinnedMeshRenderer.materials;
        mats[materialIndex].mainTextureOffset = currentOffset;
    }
}

