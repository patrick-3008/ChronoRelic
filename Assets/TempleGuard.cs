using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Animator), typeof(AudioSource))]
public class TempleGuard : MonoBehaviour
{
    [Header("Hostility")]
    public bool hostile = false;

    [Header("References")]
    public Transform player;
    public TextMeshProUGUI dialogueTextBox;
    public GameObject dialogueUI;

    private PlayerCombatAndHealth playerCombatScript;
    private Animator animator;
    private AudioSource audioSource;
    private Animator playerAnimator;

    [Header("Combat & Movement")]
    public float followDistance = 10f;
    public float stopDistance = 2f;
    public float rotationSpeed = 5f;
    public int health = 100;

    [Header("Audio")]
    public AudioClip playerHitSound;
    public AudioClip damageSound;
    public AudioClip[] punchContactSounds;
    public AudioClip punchClip;

    [Header("Interaction")]
    private bool isPlayerNearby = false;
    private bool isInteracting = false;

    private bool isDying = false;
    private bool isActionInProgress = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        animator.applyRootMotion = true;

        if (player == null)
            player = GameObject.FindWithTag("Player")?.transform;

        if (player != null)
        {
            playerCombatScript = player.GetComponent<PlayerCombatAndHealth>();
            playerAnimator = player.GetComponent<Animator>();
        }

        if (dialogueUI != null)
            dialogueUI.SetActive(false);
    }

    void Update()
    {
        if (isDying || player == null) return;

        if (playerAnimator != null && playerAnimator.GetBool("isDying"))
        {
            GoToIdleState();
            return;
        }

        if (hostile)
        {
            HandleHostileBehavior();
        }
        else
        {
            HandlePeacefulBehavior();
        }
    }

    void HandleHostileBehavior()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (isActionInProgress)
        {
            FacePlayer();
        }
        else if (distance <= followDistance && distance > stopDistance)
        {
            animator.SetBool("is_running", true);
            animator.SetBool("is_walking", true);
            animator.SetBool("is_boxing", false);

            FacePlayer();
        }
        else if (distance <= stopDistance)
        {
            PerformAttack();
        }
        else
        {
            GoToIdleState();
        }
    }

    void HandlePeacefulBehavior()
    {
        if (isPlayerNearby)
        {
            FacePlayer();

            if (Input.GetKeyDown(KeyCode.E) && dialogueUI != null && !isInteracting)
            {
                dialogueUI.SetActive(true);
                dialogueTextBox.text = "The temple is not open for visitors.";
                animator.SetTrigger("Talking");
                isInteracting = true;
            }
        }
    }

    void PerformAttack()
    {
        if (isActionInProgress) return;

        FacePlayer();
        animator.SetBool("is_running", false);
        animator.SetBool("is_walking", false);
        animator.SetBool("is_boxing", true);

        isActionInProgress = true;
        StartCoroutine(EndActionAfterAnimation());
    }

    IEnumerator EndActionAfterAnimation()
    {
        yield return new WaitForSeconds(1.5f);
        animator.SetBool("is_boxing", false);
        yield return new WaitForSeconds(Random.Range(1f, 2.5f));
        isActionInProgress = false;
    }

    public void TakeDamage(int damage)
    {
        if (isDying) return;

        hostile = true;
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

    void GoToIdleState()
    {
        animator.SetBool("is_running", false);
        animator.SetBool("is_walking", false);
        animator.SetBool("is_boxing", false);
        isActionInProgress = false;
    }

    IEnumerator DeactivateAfterDelay()
    {
        yield return new WaitForSeconds(15f);
        Destroy(gameObject);
    }

    void FacePlayer()
    {
        if (player == null || !CompareTag("Enemy")) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hostile && other.CompareTag("Player"))
        {
            isPlayerNearby = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;

            if (dialogueUI != null)
                dialogueUI.SetActive(false);

            isInteracting = false;
        }
    }

    // Optional: Audio trigger from Animation Event
    public void AttackSound()
    {
        audioSource.PlayOneShot(punchClip);
    }
}
