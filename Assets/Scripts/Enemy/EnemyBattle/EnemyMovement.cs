using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Animation Clips")]
    public AnimationClip idleClip;
    public AnimationClip walkClip;
    public AnimationClip runClip;
    public AnimationClip searchClip;

    private Vector3 desiredMoveDir = Vector3.zero;
    private float desiredSpeed = 0f;


    [System.Serializable]
    public class AttackData
    {
        public string attackName = "Attack";
        public AnimationClip attackClip;
        public int damageToPlayer;
        public int enemyEnergyCost;

        public RangeType rangeType = RangeType.Close;
        public EffectType effectToPlayer;
        public MonoBehaviour customEffectScript;

        [Header("Cooldown Settings")]
        public float cooldown = 2f;
        [HideInInspector] public float lastUsedTime = -999f;

        public enum EffectType { Hit, Takedown, Freeze, Other }
        public enum RangeType { Close, Long }

        public bool IsReady() => Time.time - lastUsedTime >= cooldown;
    }

    [Header("Attacks")]
    public AttackData attack1;
    public AttackData attack2;
    public AttackData attack3;
    public AttackData specialAttack;

    [Header("Special Attack Settings")]
    public float specialAttackCooldown = 5f;
    private float lastSpecialAttackTime = -999f;

    [Header("Energy System")]
    public int enemyEnergy = 100;
    public int maxEnergy = 100;
    public int energyRegen = 5;
    public float energyRegenTime = 2f;
    private float regenTimer;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float enemySpeed;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    private Transform currentTarget;

    [Header("Player Detection")]
    public Transform player;
    public float detectionRange = 8f;
    public float attackRange = 2f;
    public float loseSightTime = 3f;
    private float loseSightTimer = 0f;

    [Header("Search Settings")]
    public float searchDuration = 20f;
    public float searchRadius = 3f;
    private float searchTimer = 0f;
    private Vector3 lastKnownPlayerPos;
    private Vector3 searchTarget;

    private Animator animator;
    private Rigidbody rb;

    private enum EnemyState { Idle, Patrol, Run, Attack, Search }
    private EnemyState currentState = EnemyState.Idle;

    private bool isAttacking = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        currentState = EnemyState.Idle;
        enemySpeed = 0f;
        if (patrolPoints.Length > 0)
            currentTarget = patrolPoints[0];
    }

    void Update()
    {
        HandleEnergyRegen();

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (isAttacking) return;

        switch (currentState)
        {
            case EnemyState.Idle:
                PlayAnim(idleClip);
                enemySpeed = 0f;
                if (Random.value < 0.01f && patrolPoints.Length > 0)
                    currentState = EnemyState.Patrol;
                break;

            case EnemyState.Patrol:
                enemySpeed = walkSpeed;
                Patrol();
                if (distToPlayer < detectionRange)
                {
                    currentState = EnemyState.Run;
                    loseSightTimer = 0f;
                }
                break;

            case EnemyState.Run:
                enemySpeed = runSpeed;
                RunTowardsPlayer();

                if (distToPlayer < attackRange)
                {
                    currentState = EnemyState.Attack;
                }
                else if (distToPlayer > detectionRange)
                {
                    loseSightTimer += Time.deltaTime;
                    if (loseSightTimer >= loseSightTime)
                    {
                        lastKnownPlayerPos = player.position;
                        searchTimer = 0f;
                        PickNewSearchTarget();
                        currentState = EnemyState.Search;
                    }
                }
                else
                {
                    loseSightTimer = 0f;
                }
                break;

            case EnemyState.Attack:
                enemySpeed = 0f;
                StartCoroutine(PerformSmartAttack(distToPlayer));
                break;

            case EnemyState.Search:
                enemySpeed = walkSpeed;
                PlayAnim(searchClip);

                searchTimer += Time.deltaTime;

                // Move around within search radius
                if (Vector3.Distance(transform.position, searchTarget) < 0.3f)
                {
                    PickNewSearchTarget();
                }
                else
                {
                    MoveTowards(searchTarget);
                }

                // If player reappears
                if (distToPlayer < detectionRange)
                {
                    currentState = EnemyState.Run;
                    loseSightTimer = 0f;
                }
                else if (searchTimer >= searchDuration)
                {
                    currentState = EnemyState.Patrol;
                }
                break;
        }
    }

    void HandleEnergyRegen()
    {
        regenTimer += Time.deltaTime;
        if (regenTimer >= energyRegenTime)
        {
            enemyEnergy = Mathf.Min(maxEnergy, enemyEnergy + energyRegen);
            regenTimer = 0f;
        }
    }

    void Patrol()
    {
        if (currentTarget == null) return;

        PlayAnim(walkClip);
        MoveTowards(currentTarget.position);

        if (Vector3.Distance(transform.position, currentTarget.position) < 0.3f)
        {
            if (Random.value < 0.5f)
            {
                currentState = EnemyState.Idle;
            }
            else
            {
                Transform nearest1 = null, nearest2 = null;
                float dist1 = float.MaxValue, dist2 = float.MaxValue;

                foreach (Transform p in patrolPoints)
                {
                    if (p == currentTarget) continue;
                    float d = Vector3.Distance(transform.position, p.position);
                    if (d < dist1)
                    {
                        dist2 = dist1; nearest2 = nearest1;
                        dist1 = d; nearest1 = p;
                    }
                    else if (d < dist2)
                    {
                        dist2 = d; nearest2 = p;
                    }
                }

                if (nearest1 != null && nearest2 != null)
                    currentTarget = (Random.value < 0.5f) ? nearest1 : nearest2;
                else if (nearest1 != null)
                    currentTarget = nearest1;
            }
        }
    }

    void RunTowardsPlayer()
    {
        PlayAnim(runClip);
        MoveTowards(player.position);
    }

    void MoveTowards(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        if (dir.sqrMagnitude < 0.001f) return;

        Vector3 move = dir * enemySpeed * Time.deltaTime;
        rb.MovePosition(rb.position + move);

        if (dir.magnitude > 0.05f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 10f * Time.deltaTime));
        }
    }

    void PickNewSearchTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * searchRadius;
        searchTarget = lastKnownPlayerPos + new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    IEnumerator PerformSmartAttack(float distToPlayer)
    {
        isAttacking = true;

        List<AttackData> possibleAttacks = new List<AttackData>();

        // Filter by range type and cooldown
        if (distToPlayer <= attackRange + 0.5f)
        {
            if (attack1.attackClip && attack1.rangeType == AttackData.RangeType.Close &&
                enemyEnergy >= attack1.enemyEnergyCost && attack1.IsReady()) possibleAttacks.Add(attack1);
            if (attack2.attackClip && attack2.rangeType == AttackData.RangeType.Close &&
                enemyEnergy >= attack2.enemyEnergyCost && attack2.IsReady()) possibleAttacks.Add(attack2);
            if (attack3.attackClip && attack3.rangeType == AttackData.RangeType.Close &&
                enemyEnergy >= attack3.enemyEnergyCost && attack3.IsReady()) possibleAttacks.Add(attack3);
        }
        else
        {
            if (attack1.attackClip && attack1.rangeType == AttackData.RangeType.Long &&
                enemyEnergy >= attack1.enemyEnergyCost && attack1.IsReady()) possibleAttacks.Add(attack1);
            if (attack2.attackClip && attack2.rangeType == AttackData.RangeType.Long &&
                enemyEnergy >= attack2.enemyEnergyCost && attack2.IsReady()) possibleAttacks.Add(attack2);
            if (attack3.attackClip && attack3.rangeType == AttackData.RangeType.Long &&
                enemyEnergy >= attack3.enemyEnergyCost && attack3.IsReady()) possibleAttacks.Add(attack3);
        }

        // Special attack
        if (specialAttack.attackClip && enemyEnergy >= specialAttack.enemyEnergyCost &&
            enemyEnergy > maxEnergy * 0.7f && Time.time - lastSpecialAttackTime >= specialAttack.cooldown)
        {
            possibleAttacks.Add(specialAttack);
        }

        if (possibleAttacks.Count == 0)
        {
            // No valid attack → fallback to chase
            currentState = EnemyState.Run;
            isAttacking = false;
            yield break;
        }

        // Pick and perform attack
        AttackData chosenAttack = possibleAttacks[Random.Range(0, possibleAttacks.Count)];
        PlayAnim(chosenAttack.attackClip);
        enemyEnergy -= chosenAttack.enemyEnergyCost;

        chosenAttack.lastUsedTime = Time.time;
        if (chosenAttack == specialAttack)
            lastSpecialAttackTime = Time.time;

        // Wait for animation to finish
        yield return new WaitForSeconds(chosenAttack.attackClip.length);

        isAttacking = false;
        currentState = EnemyState.Run;
    }

    void PlayAnim(AnimationClip clip)
    {
        if (clip != null && animator != null)
        {
            animator.Play(clip.name);
        }
    }

    // --- Visual Debug Gizmos ---
    void OnDrawGizmosSelected()
    {
        // Detection range (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Search radius (blue)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(lastKnownPlayerPos, searchRadius);
    }
}
