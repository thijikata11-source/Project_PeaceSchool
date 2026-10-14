using UnityEngine;

public class CharacterLookAt : MonoBehaviour
{
    [Header("References")]
    public Transform player;          // Assign your Player transform here

    [Header("Look Settings")]
    public float maxLookAngle = 120f; // Max angle character can turn head
    public float lookWeight = 1f;     // Overall IK weight
    public float bodyWeight = 0.2f;   // Body influence
    public float headWeight = 0.8f;   // Head influence
    public float smoothSpeed = 3f;    // How fast IK blends in/out

    [Header("Detection")]
    public float detectionRadius = 10f;

    private Animator animator;
    private float currentWeight = 0f;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || player == null) return;

        // Distance check
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRadius)
        {
            // Too far → fade out IK
            currentWeight = Mathf.Lerp(currentWeight, 0f, Time.deltaTime * smoothSpeed);
            animator.SetLookAtWeight(currentWeight, bodyWeight, headWeight);
            return;
        }

        // Angle check
        Vector3 toPlayer = player.position - transform.position;
        Vector3 flatToPlayer = Vector3.ProjectOnPlane(toPlayer, Vector3.up).normalized;
        float angle = Vector3.Angle(transform.forward, flatToPlayer);

        float targetWeight = angle < maxLookAngle ? 1f : 0f;
        currentWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * smoothSpeed);

        animator.SetLookAtWeight(currentWeight, bodyWeight, headWeight);
        if (currentWeight > 0.01f)
        {
            animator.SetLookAtPosition(player.position);
        }
    }
}
