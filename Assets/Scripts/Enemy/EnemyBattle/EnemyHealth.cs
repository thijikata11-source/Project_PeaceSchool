using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("References")]
    public Animator animator;
    public Rigidbody rb; // optional, for knockdown physics

    private bool isDead;
    private bool isFrozen;

    void Start()
    {
        currentHealth = maxHealth;
        if (animator == null) animator = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    public void TakeDamage(int amount)
    {
        if (isDead || isFrozen) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        // Play a generic hit reaction
        if (animator != null)
            animator.SetTrigger("Hit");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ApplyKnockdown()
    {
        if (isDead) return;

        if (animator != null)
            animator.SetTrigger("Knockdown");

        // Optional: add physics force
        if (rb != null)
            rb.AddForce(-transform.forward * 5f, ForceMode.Impulse);
    }

    public void ApplyFreeze()
    {
        if (isDead) return;

        isFrozen = true;
        if (animator != null)
            animator.SetBool("Frozen", true);

        // Example: stop movement
        if (rb != null)
            rb.velocity = Vector3.zero;

        // You can unfreeze after a delay
        Invoke(nameof(Unfreeze), 3f);
    }

    private void Unfreeze()
    {
        isFrozen = false;
        if (animator != null)
            animator.SetBool("Frozen", false);
    }

    private void Die()
    {
        isDead = true;
        if (animator != null)
            animator.SetTrigger("Die");

        // Disable collider or AI script
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Optional: destroy after delay
        Destroy(gameObject, 5f);
    }
}
