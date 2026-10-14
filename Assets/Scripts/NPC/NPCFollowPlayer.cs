using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class NPCFollowPlayer : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator;

    [Header("Party")]
    public PartyMembers PMembers;
    public GameObject[] characters;
    public int currentIdFront;

    [Header("Follow Settings")]
    public float followDistance = 4f;
    public float stopDistance = 2f;

    [Header("Sprint Settings")]
    public float sprintDistance = 8f;
    public float sprintStopDistance = 4f;

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 9f;
    public float rotationSpeed = 8f;

    [Header("Wide Obstacle Avoidance")]
    public float obstacleDistance = 3f; // Slightly increased for earlier detection of wide objects
    public LayerMask obstacleLayers;
    public float npcWidthRadius = 0.5f;   // Matches the physical radius of your NPC body

    [Header("Gravity")]
    public float gravity = -60f;
    public float groundedSnap = -2f;

    private CharacterController controller;
    private float verticalVelocity;

    private bool isFollowing;
    private bool isRunning;
    private Vector3 lastSlideDir = Vector3.zero;
    private bool wasBlockedLastFrame = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        currentIdFront = PMembers.currentFrontId;

        if (characters == null || characters.Length == 0)
            return;

        player = characters[currentIdFront].transform;

        if (player == null)
            return;

        // Gravity
        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = groundedSnap;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        float distance = direction.magnitude;

        // Follow start
        if (distance >= followDistance)
            isFollowing = true;

        // Stop follow
        if (distance <= stopDistance)
        {
            isFollowing = false;
            isRunning = false;

            controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);

            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveZ", 0f);
                animator.SetBool("isSprinting", false);
            }

            return;
        }

        if (!isFollowing)
            return;

        // ==================================
        // CATCH-UP SYSTEM
        // ==================================
        if (!isRunning && distance > sprintDistance)
        {
            isRunning = true;
        }
        else if (isRunning && distance < sprintStopDistance)
        {
            isRunning = false;
        }

        float moveSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 moveDir = direction.normalized;

        // ==================================
        // FIXED SMART WIDE OBSTACLE AVOIDANCE
        // ==================================
        Vector3 rayOrigin = transform.position + Vector3.up * (controller.height * 0.5f);
        RaycastHit hit;

        // 1. Broad volume scan using a SphereCast to capture the true width of walls
        bool obstacleAhead = Physics.SphereCast(
            rayOrigin,
            npcWidthRadius,
            moveDir,
            out hit,
            obstacleDistance,
            obstacleLayers
        );

        if (obstacleAhead)
        {
            // 2. Extract the surface orientation of the blocking wall
            Vector3 hitNormal = hit.normal;
            hitNormal.y = 0; // Keep movement locked to horizontal plane
            hitNormal.Normalize();

            // 3. Compute a vector that slides seamlessly parallel along the wall face
            Vector3 slideDir = Vector3.ProjectOnPlane(moveDir, hitNormal).normalized;

            // Prevent the NPC from turning backwards into an loop if it gets perfectly pinned
            if (Vector3.Dot(slideDir, direction.normalized) < -0.1f)
            {
                slideDir = Vector3.Cross(Vector3.up, hitNormal).normalized;
            }

            moveDir = slideDir;
            lastSlideDir = slideDir;
            wasBlockedLastFrame = true;
        }
        else if (wasBlockedLastFrame)
        {
            // 4. Corner-Clearing Guard: If just exiting a wall boundary, 
            // maintain the slide trajectory briefly to make sure we don't clip the outer edge.
            Vector3 clearCheckOrigin = rayOrigin - (moveDir * 0.2f);
            bool cornerStillNear = Physics.SphereCast(
                clearCheckOrigin,
                npcWidthRadius * 1.2f,
                lastSlideDir,
                out _,
                1.5f,
                obstacleLayers
            );

            if (cornerStillNear)
            {
                moveDir = lastSlideDir;
            }
            else
            {
                wasBlockedLastFrame = false;
            }
        }

        // Rotate
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // Move
        Vector3 motion = moveDir * moveSpeed;
        motion.y = verticalVelocity;

        controller.Move(motion * Time.deltaTime);

        // ==================================
        // ANIMATION
        // ==================================
        if (animator != null)
        {
            animator.SetFloat("Speed", 1f);
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveZ", 1f);
            animator.SetBool("isSprinting", isRunning);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (controller == null)
            return;

        // Visualizes the width footprint of the sweeping sensor path
        Gizmos.color = Color.red;
        Vector3 rayOrigin = transform.position + Vector3.up * (controller.height * 0.5f);

        Gizmos.DrawWireSphere(rayOrigin, npcWidthRadius);
        Gizmos.DrawRay(rayOrigin, transform.forward * obstacleDistance);
        Gizmos.DrawWireSphere(rayOrigin + transform.forward * obstacleDistance, npcWidthRadius);
    }
}
