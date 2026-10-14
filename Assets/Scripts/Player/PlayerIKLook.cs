using UnityEngine;

public class PlayerIKLook : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;

    [Header("Look Settings")]
    public float maxLookAngle = 120f;
    public float lookWeight = 1f;
    public float bodyWeight = 0.2f;
    public float headWeight = 0.8f;

    [Header("Detection")]
    public float detectionRadius = 10f;
    public LayerMask detectionMask;

    private Animator animator;
    private Transform currentTarget;
    private float currentWeight = 0f;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        currentTarget = FindPriorityTarget();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || currentTarget == null) return;

        Vector3 toTarget = currentTarget.position - transform.position;
        Vector3 flatToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;
        float angle = Vector3.Angle(transform.forward, flatToTarget);

        float targetWeight = angle < maxLookAngle ? 1f : 0f;
        currentWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * 3f);

        animator.SetLookAtWeight(currentWeight, bodyWeight, headWeight);
        if (currentWeight > 0.01f)
        {
            animator.SetLookAtPosition(currentTarget.position);
        }
    }

    Transform FindPriorityTarget()
    {
        Transform t = FindClosestByTag("Enemy");
        if (t != null) return t;

        t = FindClosestByTag("Object");
        if (t != null) return t;

        t = FindClosestByTag("NPC");
        if (t != null) return t;

        return cameraTransform; // fallback
    }

    Transform FindClosestByTag(string tag)
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag(tag);
        Transform closest = null;
        float closestSqrDist = Mathf.Infinity;

        foreach (GameObject go in targets)
        {
            float sqrDist = (go.transform.position - transform.position).sqrMagnitude;
            if (sqrDist < detectionRadius * detectionRadius && sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closest = go.transform;
            }
        }

        return closest;
    }
}
