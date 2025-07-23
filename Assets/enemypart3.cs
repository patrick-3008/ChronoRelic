using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator), typeof(AudioSource))]
public class EnemyFollowAndEvadeWithAnimation : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator playerAnimator;
    private PlayerCombatAndHealth playerCombatScript;
    private Animator animator;
    private AudioSource audioSource;

    [Header("Settings")]
    public float followDistance = 30f;
    public float stopDistance = 5f;
    public float rotationSpeed = 5f;
    public int health = 100;

    [Header("Audio")]
    public AudioClip playerHitSound;
    public AudioClip damageSound;
    public AudioClip[] punchContactSounds;
    public AudioClip punchClip;
    public AudioClip swordClip;

    [Header("Drops")]
    public GameObject bandagePrefab;

    private bool isDying = false;
    private bool isActionInProgress = false;
    private bool followDistanceDoubled = false;
    private float baseFollowDistance;

    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        animator.applyRootMotion = true;

        baseFollowDistance = followDistance;

        if (player != null)
        {
            playerCombatScript = player.GetComponent<PlayerCombatAndHealth>();
            if (playerCombatScript != null)
                playerAnimator = playerCombatScript.GetComponent<Animator>();
        }
    }

    void Update()
    {
        if (player == null || animator == null || isDying) return;
        if (isDying || (playerAnimator != null && playerAnimator.GetBool("isDying")))
        {
            GoToIdleState();
            return;
        }

        if (health < 100 && !followDistanceDoubled)
        {
            followDistance *= 2f;
            followDistanceDoubled = true;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (isActionInProgress)
        {
            FacePlayer();
        }
        else if (distanceToPlayer <= followDistance && distanceToPlayer > stopDistance)
        {
            // Run toward the player (movement via animation)
            animator.SetBool("is_running", true);
            animator.SetBool("is_walking", true);
            animator.SetBool("is_boxing", false);

            FacePlayer();
        }
        else if (distanceToPlayer <= stopDistance + 0.5f)
        {
            PerformAction();
        }
        else
        {
            GoToIdleState();
        }
    }

    void PerformAction()
    {
        if (isActionInProgress) return;

        FacePlayer();
        animator.SetBool("is_running", false);
        animator.SetBool("is_walking", false);
        animator.SetBool("is_boxing", true);

        isActionInProgress = true;
        StartCoroutine(EndActionAfterAnimation());
    }

    void FacePlayer()
    {
        if (player == null || isDying) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f && !isDying)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }



    IEnumerator EndActionAfterAnimation()
    {
        yield return new WaitForSeconds(1.5f);
        EndAction();
    }

    void EndAction()
    {
        if (isDying) return; // ← prevent further logic if dead

        animator.SetBool("is_boxing", false);

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > stopDistance)
        {
            animator.SetBool("is_running", true);
            animator.SetBool("is_walking", true);
        }

        StartCoroutine(WaitRandomIdleTime());
    }


    IEnumerator WaitRandomIdleTime()
    {
        float idleTime = Random.Range(1f, 2.5f);
        yield return new WaitForSeconds(idleTime);

        if (isDying) yield break; // cancel coroutine if dead

        isActionInProgress = false;
    }


    public void ApplyDamageToPlayer(int damage)
    {
        if (player == null || playerCombatScript == null) return;

        float distX = Mathf.Abs(transform.position.x - player.position.x);
        float distY = Mathf.Abs(transform.position.y - player.position.y);

        if (distX <= 2.5f && distY <= 2.5f)
        {
            playerCombatScript.health -= damage;

            if (playerHitSound != null)
                audioSource.PlayOneShot(playerHitSound);

            if (playerAnimator != null)
            {
                AnimatorStateInfo state = playerAnimator.GetCurrentAnimatorStateInfo(0);
                if (!playerAnimator.IsInTransition(0) && state.IsName("Idle"))
                {
                    playerAnimator.SetTrigger("HitReaction");
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDying) return;

        health -= damage;
        animator.SetTrigger("HitReaction");

        if (health <= 0)
        {
            health = 0;
            isDying = true;
            animator.SetBool("isDying", true);
            gameObject.tag = "Untagged";
            GoToIdleState();
            StartCoroutine(DeactivateAfterDelay());
        }
    }

    IEnumerator DeactivateAfterDelay()
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
        animator.SetBool("is_running", false);
        animator.SetBool("is_walking", false);
        animator.SetBool("is_boxing", false);
        isActionInProgress = false;
    }

    public void AttackSound()
    {
        audioSource.PlayOneShot(punchClip);
    }

    public void swordAttackSound()
    {
        audioSource.PlayOneShot(swordClip);
    }

    // Optional: external control from other scripts
    public void SetWalkAnimation(bool state) => animator.SetBool("is_walking", state);
    public void SetRunAnimation(bool state) => animator.SetBool("is_running", state);
}
