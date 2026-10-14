using UnityEngine;

public class IdleManager : MonoBehaviour
{
    [Header("Idle Settings")]
    public float idleThreshold = 50f; // seconds before triggering idle animation
    public AnimationClip[] idleAnimations; // assign clips in inspector

    [Header("References")]
    public Animator animator;
    public PlayerMovement3D playerMovement; // reference to your movement script

    private float idleTimer;
    private bool isIdlePlaying;

    void Update()
    {
        // Use the public property instead of private field
        bool isMoving = playerMovement != null && playerMovement.Controller.velocity.magnitude > 0.1f;

        if (isMoving)
        {
            idleTimer = 0f;
            if (isIdlePlaying)
            {
                animator.SetBool("IdleSpecial", false);
                isIdlePlaying = false;
            }
        }
        else
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleThreshold && !isIdlePlaying)
            {
                PlayRandomIdle();
            }
        }
    }

    void PlayRandomIdle()
    {
        if (idleAnimations.Length == 0) return;

        int index = Random.Range(0, idleAnimations.Length);
        animator.Play(idleAnimations[index].name);
        isIdlePlaying = true;
    }
}
