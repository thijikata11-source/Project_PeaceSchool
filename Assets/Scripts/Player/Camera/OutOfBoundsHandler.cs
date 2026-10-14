using UnityEngine;

[RequireComponent(typeof(Animator), typeof(Rigidbody))]
public class OutOfBoundsHandler : MonoBehaviour
{
    [Header("Detection Settings")]
    public LayerMask outOfBoundsLayer;
    public float rayDistance = 0.6f;

    [Header("Scripts To Disable")]
    public MonoBehaviour[] scriptsToDisable;

    private Animator animator;
    private Rigidbody rb;
    private bool isOutOfBounds = false;
    private bool isInStop = false;
    private Vector3 lastBlockNormal;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Forward raycast check
        bool hitWall = Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, rayDistance, outOfBoundsLayer);

        if (hitWall && !isOutOfBounds)
        {
            EnterStop(hit.normal);
        }
        else if (!hitWall && isOutOfBounds && !isInStop)
        {
            // Only exit once we've left Stop state
            ExitOutOfBounds();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & outOfBoundsLayer) != 0 && !isOutOfBounds)
        {
            EnterStop(collision.contacts[0].normal);
        }
    }

    private void EnterStop(Vector3 blockNormal)
    {
        isOutOfBounds = true;
        isInStop = true;
        lastBlockNormal = blockNormal;

        rb.velocity = Vector3.zero;
        animator.Play("Stop");

        foreach (var script in scriptsToDisable)
            if (script != null) script.enabled = false;

        Debug.Log("[OutOfBoundsHandler] Stop → scripts disabled.");
    }

    private void ExitOutOfBounds()
    {
        isOutOfBounds = false;
        isInStop = false;

        foreach (var script in scriptsToDisable)
            if (script != null) script.enabled = true;

        Debug.Log("[OutOfBoundsHandler] Exit → scripts re‑enabled.");
    }

    /// <summary>
    /// Call this from your movement script with the current input direction.
    /// Returns true if movement is allowed.
    /// </summary>
    public bool CanMove(Vector3 inputDir)
    {
        if (!isOutOfBounds) return true;

        if (isInStop)
        {
            if (inputDir.sqrMagnitude < 0.01f)
                return false; // no input → stay in Stop

            // Predict if input direction still collides
            bool pushingIntoWall = Physics.Raycast(transform.position, inputDir.normalized, rayDistance, outOfBoundsLayer);

            if (pushingIntoWall)
            {
                // Still pressing into wall → remain in Stop
                return false;
            }
            else
            {
                // Moving away or sideways → switch to Idle
                animator.Play("Idle");
                isInStop = false;

                foreach (var script in scriptsToDisable)
                    if (script != null) script.enabled = true;

                return true;
            }
        }

        return true;
    }
}
