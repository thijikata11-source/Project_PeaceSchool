using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterActs : MonoBehaviour
{
    [Header("Character Settings")]
    public Transform characterRoot;        // assign the child GameObject to rotate
    public Animator characterAnimator;

    [Header("Animator State Names")]
    public string idleAStateName = "IdleA";   // type the exact state name in Animator
    public string idleBStateName = "IdleB";
    public string walkingStateName = "Walking";

    [Header("Movement Settings")]
    public float moveRange = 5f;           // radius around starting point
    public float moveSpeed = 2f;
    public float rotationSpeed = 5f;
    public float idleWaitTime = 3f;        // how long to wait in idle before moving again

    public enum MovementStyle { Round, Linear }
    [Header("Movement Style")]
    public MovementStyle movementStyle = MovementStyle.Round;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool movingForward = true;

    private CharacterController controller;
    private float idleTimer = 0f;
    private bool isIdle = false;


    // ✅ Add CharacterSystem reference
    [Header("Dependencies")]
    public CharacterSystem characterSystem;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (characterRoot == null) characterRoot = transform;

        startPosition = characterRoot.position;
        PickNewTarget();
        EnterIdleState();
    }

    void Update()
    {
        if (isIdle)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                isIdle = false;
                PickNewTarget();
            }
            return;
        }

        if (movementStyle == MovementStyle.Round)
        {
            MoveRound();
        }
        else if (movementStyle == MovementStyle.Linear)
        {
            MoveLinear();
        }
    }

    void PickNewTarget()
    {
        Vector3 randomPoint = Random.insideUnitSphere * moveRange;
        randomPoint.y = 0;
        targetPosition = startPosition + randomPoint;
    }

    void MoveRound()
    {
        float distance = Vector3.Distance(characterRoot.position, targetPosition);

        if (distance < 0.5f)
        {
            EnterIdleState();
        }
        else
        {
            EnterWalkingState();
            MoveTowardsTarget();
        }
    }

    void MoveLinear()
    {
        Vector3 pointA = startPosition + Vector3.forward * moveRange;
        Vector3 pointB = startPosition - Vector3.forward * moveRange;

        targetPosition = movingForward ? pointA : pointB;

        float distance = Vector3.Distance(characterRoot.position, targetPosition);

        if (distance < 0.5f)
        {
            EnterIdleState();
            movingForward = !movingForward;
        }
        else
        {
            EnterWalkingState();
            MoveTowardsTarget();
        }
    }

    void MoveTowardsTarget()
    {
        Vector3 direction = (targetPosition - characterRoot.position).normalized;
        controller.Move(direction * moveSpeed * Time.deltaTime);

        // ✅ Always rotate while walking
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        characterRoot.rotation = Quaternion.Slerp(characterRoot.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    }

    // ✅ Animator parameter control
    void EnterIdleState()
    {
        isIdle = true;
        idleTimer = idleWaitTime;

        if (characterAnimator != null)
        {
            characterAnimator.SetBool("isWalking", false);

            // Randomly trigger IdleA or IdleB
            if (Random.value > 0.5f)
                characterAnimator.Play(idleAStateName);
            else
                characterAnimator.Play(idleBStateName);
        }
    }

    void EnterWalkingState()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetBool("isWalking", true);
            characterAnimator.Play(walkingStateName);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = (characterRoot != null) ? characterRoot.position : transform.position;
        Gizmos.DrawWireSphere(center, moveRange);
    }
}
