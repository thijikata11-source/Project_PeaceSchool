using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class PlayerFootIK : MonoBehaviour
{
    private Animator animator;
    private CharacterController controller;

    [Header("IK Weights")]
    [Range(0f, 1f)] public float positionWeight = 1f;
    [Range(0f, 1f)] public float rotationWeight = 1f;

    [Header("Raycast Configurations")]
    public LayerMask stairLayer;
    public float raycastOffsetUp = 0.5f;
    public float raycastDistance = 1.0f;
    public float footHeightOffset = 0.05f;

    [Header("Smoothing")]
    public float lerpSpeed = 20f;

    [Header("Pelvis/Hips Adjustment")]
    public bool adjustPelvisHeight = true;
    public float pelvisLerpSpeed = 12f;

    private Vector3 leftFootIKPos, rightFootIKPos;
    private Quaternion leftFootIKRot, rightFootIKRot;
    private float lastPelvisOffset;

    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !animator.enabled) return;

        // Process Raycasts and target positions
        Vector3 targetLeftPos = EvaluateFootIK(AvatarIKGoal.LeftFoot, HumanBodyBones.LeftFoot, ref leftFootIKRot, out float leftWeightMod);
        Vector3 targetRightPos = EvaluateFootIK(AvatarIKGoal.RightFoot, HumanBodyBones.RightFoot, ref rightFootIKRot, out float rightWeightMod);

        // Dynamic Weights: Reduce IK impact if the foot is high in the air during its swing phase
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, positionWeight * leftWeightMod);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, rotationWeight * leftWeightMod);

        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, positionWeight * rightWeightMod);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rotationWeight * rightWeightMod);

        // Smoothly interpolate positions over time
        leftFootIKPos = Vector3.Lerp(animator.GetIKPosition(AvatarIKGoal.LeftFoot), targetLeftPos, Time.deltaTime * lerpSpeed);
        rightFootIKPos = Vector3.Lerp(animator.GetIKPosition(AvatarIKGoal.RightFoot), targetRightPos, Time.deltaTime * lerpSpeed);

        // Apply positions and rotations
        animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFootIKPos);
        animator.SetIKRotation(AvatarIKGoal.LeftFoot, leftFootIKRot);

        animator.SetIKPosition(AvatarIKGoal.RightFoot, rightFootIKPos);
        animator.SetIKRotation(AvatarIKGoal.RightFoot, rightFootIKRot);

        // Displace the character hips down naturally
        if (adjustPelvisHeight && controller.isGrounded)
        {
            AdjustPelvisHeight(leftFootIKPos, rightFootIKPos);
        }
    }

    private Vector3 EvaluateFootIK(AvatarIKGoal goal, HumanBodyBones bone, ref Quaternion targetRotation, out float weightModifier)
    {
        Vector3 footPosition = animator.GetBoneTransform(bone).position;
        Vector3 rayOrigin = new Vector3(footPosition.x, transform.position.y + raycastOffsetUp, footPosition.z);

        // Calculate how high the foot currently is relative to the character transform root
        float currentFootHeightFromGround = footPosition.y - transform.position.y;

        // If the foot is actively lifting high in its normal animation clip, fade out IK influence 
        // This stops the foot from snapping straight back down to the stair tread during a step forward
        weightModifier = currentFootHeightFromGround > 0.25f ? Mathf.Clamp01(1f - (currentFootHeightFromGround * 2f)) : 1f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, stairLayer))
        {
            Vector3 targetPosition = footPosition;
            targetPosition.y = hit.point.y + footHeightOffset;

            // Match rotation with step slope angle
            targetRotation = Quaternion.LookRotation(transform.forward, hit.normal);
            return targetPosition;
        }

        // Fallback to normal animation position
        targetRotation = animator.GetIKRotation(goal);
        weightModifier = 0f; // No stair hit, clear IK override
        return footPosition;
    }

    private void AdjustPelvisHeight(Vector3 leftTarget, Vector3 rightTarget)
    {
        // Find how far each foot wants to adjust relative to the root position
        float leftOffset = leftTarget.y - transform.position.y;
        float rightOffset = rightTarget.y - transform.position.y;

        // Use the absolute difference to create a clean balance point between steps
        float targetPelvisOffset = Mathf.Min(leftOffset, rightOffset);

        // Ensure pelvis adjustment only acts on stepping up/slopes
        if (targetPelvisOffset < 0.01f) targetPelvisOffset = 0f;

        // Smoothly approach target displacement position
        lastPelvisOffset = Mathf.Lerp(lastPelvisOffset, targetPelvisOffset, Time.deltaTime * pelvisLerpSpeed);

        Vector3 bodyPosition = animator.bodyPosition;
        bodyPosition.y += lastPelvisOffset;
        animator.bodyPosition = bodyPosition;
    }
}
