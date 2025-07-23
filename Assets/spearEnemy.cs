using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class SpearThrowingEnemy : MonoBehaviour
{
    public Transform player;
    public GameObject spearPrefab;
    public Transform throwPoint;
    public float followDistance = 15f;
    public float attackRange = 13f;
    public float meleeRange = 3f;
    public float evadeDistance = 10f;
    public float throwCooldown = 2f;
    public float spearSpeed = 15f;
    private float baseFollowDistance;
    private bool followDistanceDoubled = false;
    private float baseAttackRange;

    public GameObject bandagePrefab;

    private NavMeshAgent agent;
    private Animator animator;
    private bool isActionInProgress = false;
    private bool canThrow = true;
    private bool isSpearAttacking = false;

    private PlayerCombatAndHealth playerCombatScript;
    private Animator playerAnimator;

    public int health = 100;
    private bool isDying = false;
    public int spearThrowDamage = 30;
    public int meleeDamage = 20;

    public AudioClip playerHitSound;
    private AudioSource audioSource;
    public AudioClip damageSound;
    public AudioClip spearAttackClip;
    public AudioClip spearThrowClip;
    public AudioClip[] punchContactSounds; // Assign 4 clips in the Inspector



    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (player != null)
        {
            playerCombatScript = player.GetComponent<PlayerCombatAndHealth>();
            playerAnimator = player.GetComponent<Animator>();
        }

        animator.applyRootMotion = false;
        agent.updateRotation = false;

        baseFollowDistance = followDistance;
        baseAttackRange = attackRange;
        followDistanceDoubled = false;
    }


    void Update()
    {
        if (player == null || animator == null || isDying) return;

        // Double follow distance if health drops below 100
        if (health <= 50 && !followDistanceDoubled)
        {
            followDistance *= 2f;
            attackRange *= 2f;
            followDistanceDoubled = true;
        }


        if (playerAnimator != null && playerAnimator.GetBool("isDying"))
        {
            GoToIdleState();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (isActionInProgress)
        {
            FacePlayer(); // 🛠 Ensure enemy rotates even during attack animations
            return;
        }

        if (distanceToPlayer > meleeRange && distanceToPlayer <= attackRange)
        {
            GoToIdleState();
            if (canThrow)
            {
                FacePlayer(); // 🛠 Make sure enemy faces player before throwing
                animator.SetTrigger("throw_spear");
                canThrow = false;
                StartCoroutine(ResetThrowCooldown());
            }
        }
        else if (distanceToPlayer > meleeRange && distanceToPlayer <= evadeDistance)
        {
            StartEvading();
        }
        else if (distanceToPlayer <= meleeRange)
        {
            PerformMeleeAttack();
        }
        else
        {
            GoToIdleState();
        }

        FacePlayer(); // 🛠 Always rotate toward the player when moving
    }

    // ✅ Now called by an animation event
    public void ThrowSpear()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > attackRange)
        {
            Debug.Log("❌ Player is out of throw range, canceling spear throw.");
            return;
        }

        FacePlayer(); // Ensure the enemy faces the player before throwing

        // Instantiate spear
        GameObject spear = Instantiate(spearPrefab, throwPoint.position, throwPoint.rotation);

        // Rotate it by 180 degrees to face the player
        spear.transform.Rotate(0, 180, 0);

        StartCoroutine(ThrowSpearAtTarget(spear, player.gameObject));
    }





    private IEnumerator ThrowSpearAtTarget(GameObject spear, GameObject target)
    {
        Vector3 targetPosition = target.transform.position + new Vector3(0, 1.5f, 0); // Adjust for height

        while (spear != null && target != null)
        {
            Vector3 direction = (targetPosition - spear.transform.position).normalized;
            spear.transform.position += direction * spearSpeed * Time.deltaTime;

            spear.transform.LookAt(targetPosition); // Ensure it faces the adjusted target position
            spear.transform.rotation *= Quaternion.Euler(0, 90, 0); // Apply 90-degree rotation

            if (Vector3.Distance(spear.transform.position, targetPosition) < 0.2f)
            {
                PlayerCombatAndHealth playerScript = target.GetComponent<PlayerCombatAndHealth>();

                if (playerScript != null)
                {
                    if (playerHitSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(playerHitSound);
                    }
                    playerScript.health -= spearThrowDamage;
                    Debug.Log("❌ Player hit by spear! -" + spearThrowDamage + " HP");
                }
                Destroy(spear);
                break;
            }

            yield return null;
        }
    }

    IEnumerator ResetThrowCooldown()
    {
        yield return new WaitForSeconds(throwCooldown);
        canThrow = true;
    }

    void StartEvading()
    {
        agent.isStopped = false;
        agent.speed = 3f;
        animator.SetBool("is_walking", true);
        animator.SetBool("is_running", false);
        animator.SetBool("is_attacking", false);

        StartCoroutine(EvadePlayer());
    }

    IEnumerator EvadePlayer()
    {
        while (Vector3.Distance(transform.position, player.position) < evadeDistance)
        {
            Vector3 direction = (transform.position - player.position).normalized;
            Vector3 newPos = transform.position + direction * 3f;

            agent.SetDestination(newPos);
            yield return null;
        }

        GoToIdleState();
    }

    void PerformMeleeAttack()
    {
        if (isActionInProgress) return;

        agent.isStopped = true;
        SpearAttack();

        StartCoroutine(ResetMeleeAttackState());
    }

    private void SpearAttack()
    {
        animator.SetTrigger("melee_attack");
        isSpearAttacking = true;
        StartCoroutine(ResetSpearRotationAfterDelay(1.5f));
    }

    private IEnumerator ResetSpearRotationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isSpearAttacking = false;
    }

    IEnumerator ResetMeleeAttackState()
    {
        yield return new WaitForSeconds(1.5f);
        isActionInProgress = false;
    }

    // Called via Animation Event
    public void ApplyDamageToPlayer()
    {
        if (player != null && playerCombatScript != null)
        {
            float distanceX = Mathf.Abs(transform.position.x - player.position.x);
            float distanceY = Mathf.Abs(transform.position.y - player.position.y);

            if (distanceX <= 2.5f && distanceY <= 2.5f)
            {
                playerCombatScript.health -= meleeDamage;
                Debug.Log("Player's health reduced. Current health: " + playerCombatScript.health);

                if (playerHitSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(playerHitSound);
                }

                // Trigger the hit reaction animation
                Animator playerAnimator = player.GetComponent<Animator>();
                if (playerAnimator != null)
                {
                    playerAnimator.SetTrigger("HitReaction");
                }
                else
                {
                    Debug.LogWarning("Player Animator not found!");
                }
            }


        }
    }

    void FacePlayer()
    {
        if (player == null || !CompareTag("Enemy")) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Prevent tilting

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f); // Increased speed
    }

    public void TakeDamage(int damage)
    {
        if (isDying) return; // Prevent reaction after death

        health -= damage;
        Debug.Log("Enemy health reduced by " + damage + ". Current health: " + health);

        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0); // Assuming layer 0

            if (stateInfo.IsName("idle")) // Change this to match the exact name of your idle state
            {
                animator.SetTrigger("HitReaction");
            }
        }

        if (health <= 0)
        {
            health = 0;
            isDying = true;
            animator.SetBool("isDying", true);
            GoToIdleState();
            StartCoroutine(DeactivateAfterDelay());
            // Change tag to Untagged when enemy dies
            gameObject.tag = "Untagged";
            Debug.Log("Enemy tag changed to Untagged on death.");
        }
    }



    private IEnumerator DeactivateAfterDelay()
    {
        if (bandagePrefab != null && Random.value < 0.5f)
        {
            Instantiate(bandagePrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
            Debug.Log("Bandage dropped!");
        }

        yield return new WaitForSeconds(15f);
        Destroy(gameObject);
    }

    void GoToIdleState()
    {
        agent.isStopped = true;
        animator.SetBool("is_running", false);
        animator.SetBool("is_walking", false);
        animator.SetBool("is_attacking", false);
        isActionInProgress = false;
    }

    public void spearAttackSound()
    {
        audioSource.PlayOneShot(spearAttackClip);
    }

    public void spearThrowSound()
    {
        audioSource.PlayOneShot(spearThrowClip);
    }
}
