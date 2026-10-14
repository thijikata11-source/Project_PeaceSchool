using UnityEngine;

public class BlendShapeManualController : MonoBehaviour
{
    [Tooltip("Assign the SkinnedMeshRenderer you want to control.")]
    public SkinnedMeshRenderer skinnedMeshRenderer;

    [System.Serializable]
    public struct BlendShapeAssignment
    {
        [Tooltip("Index of the blendshape in the mesh (0 = first blendshape).")]
        public int index;

        [Tooltip("Weight to apply (0–100).")]
        [Range(0f, 100f)]
        public float weight;
    }

    [Tooltip("List of blendshapes to apply by index.")]
    public BlendShapeAssignment[] assignments;

    private void Start()
    {
        ApplyBlendShapes();
    }

    private void ApplyBlendShapes()
    {
        if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null) return;

        int count = skinnedMeshRenderer.sharedMesh.blendShapeCount;

        foreach (var assignment in assignments)
        {
            if (assignment.index >= 0 && assignment.index < count)
            {
                skinnedMeshRenderer.SetBlendShapeWeight(assignment.index, assignment.weight);
            }
            else
            {
                Debug.LogWarning($"Blendshape index {assignment.index} is out of range (0–{count - 1}).");
            }
        }
    }
}
