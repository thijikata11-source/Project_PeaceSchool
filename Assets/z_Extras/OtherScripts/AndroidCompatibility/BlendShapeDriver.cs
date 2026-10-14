using UnityEngine;

public class BlendShapeDriver : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMeshRenderer; // Assign your mesh here
    public Animator animator;                       // Assign your Animator here

    // Map blend shape indices to Animator parameters
    [System.Serializable]
    public struct BlendShapeMapping
    {
        public string parameterName; // Animator float parameter
        public int blendShapeIndex;  // Blend shape index in the mesh
    }

    public BlendShapeMapping[] mappings;

    void LateUpdate()
    {
        foreach (var mapping in mappings)
        {
            float weight = animator.GetFloat(mapping.parameterName);
            skinnedMeshRenderer.SetBlendShapeWeight(mapping.blendShapeIndex, weight);
        }
    }
}
