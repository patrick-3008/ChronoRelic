using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PharaohBoss : MonoBehaviour
{
    public enum BossPhase { Melee, Flying, Vulnerable }
    private BossPhase currentPhase = BossPhase.Melee;

    public Transform player;
    public float flyingHeight = 10f;
    public float meleeDuration = 10f;
    public float flyingDuration = 6f;
    public float rockThrowInterval = 1.5f;
    public float rockSpeed = 3f;
    public float rockTravelDuration = 0.05f;
    public int rockDamage = 25;
    public int health = 200;

    public float boxingRange = 4f;

    public GameObject rockProjectilePrefab;
    public Transform rockSpawnPoint;

    public List<Transform> rockSummonPoints;
    public Transform rockCircleCenter;
    public float summonRiseDuration = 4f;
    public float circleRadius = 2f;

    private List<GameObject> summonedRocks = new List<GameObject>();
    private float phaseTimer;
    private float earthquakeCooldown;
    private bool isActionInProgress = false;
    public bool isInvincible = true;
    private bool isDying = false;
    private bool hasEnteredFlyingPhase = false;

    [Header("Ankh Effects")]
    public Light ankhLight;
    public ParticleSystem ankhParticles;
    public GameObject ankhTrail;
    public float lightFadeDuration = 1.5f;
    public GameObject shockwaveVFXPrefab;
    public Transform ankh;
    public Transform ankhTargetLocation;
    public float ankhMoveSpeed = 0.2f; // slow movement
    private bool isAnkhReleasing = false;
    
    private Animator animator;
    private AudioSource audioSource;

    private PlayerCombatAndHealth playerCombatScript;
    public Animator playerAnimator;

    public AudioClip playerHitSound;
    public AudioClip punchSound;
    public AudioClip damageSound;
    public AudioClip earthquakeSound;
    public AudioClip flySound;
    public AudioClip summonRockSound;
    public AudioClip rockThrowSound;
    public AudioClip fireRockSound;


    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (player != null)
        {
            playerCombatScript = player.GetComponent<PlayerCombatAndHealth>();
            playerAnimator = player.GetComponent<Animator>();
        }

        currentPhase = BossPhase.Melee;
        isInvincible = true;
        phaseTimer = meleeDuration;
        ResetEarthquakeCooldown();
    }

    void Update()
    {

        if (isAnkhReleasing && ankh != null)
        {
            float distance = Vector3.Distance(ankh.position, ankhTargetLocation.position);
            float angleDifference = Quaternion.Angle(ankh.rotation, ankhTargetLocation.rotation);

            if (distance > 0.01f || angleDifference > 1f)
            {
                Debug.Log("ankh moving");
                ankh.position = Vector3.MoveTowards(ankh.position, ankhTargetLocation.position, ankhMoveSpeed * Time.deltaTime);
                ankh.rotation = Quaternion.Slerp(ankh.rotation, ankhTargetLocation.rotation, ankhMoveSpeed * Time.deltaTime);
            }
            else
            {
                Debug.Log("ankh reached destination");
                isAnkhReleasing = false;
            }
        }


        if (player == null || animator == null || isDying) return;

        FacePlayer();

        if (currentPhase == BossPhase.Melee)
        {
            phaseTimer -= Time.deltaTime;

            if (phaseTimer <= 0f && !hasEnteredFlyingPhase)
            {
                hasEnteredFlyingPhase = true;
                animator.SetBool("isFlying", true);
                animator.SetTrigger("fly");

                return;
            }


            HandleJumpAttackCooldown();
            HandleBoxingAttack();
        }
    }

    void PlayFlySound()
    {
        audioSource.PlayOneShot(flySound);
    }

    void FacePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction.magnitude > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    void HandleJumpAttackCooldown()
    {
        if (isActionInProgress) return;

        earthquakeCooldown -= Time.deltaTime;
        if (earthquakeCooldown <= 0f && !animator.GetBool("isFlying"))
        {
            audioSource.PlayOneShot(earthquakeSound);
            animator.SetTrigger("jumpAttack");
            isActionInProgress = true;
            StartCoroutine(EndActionAfterDelay(2f));
            ResetEarthquakeCooldown();
        }
    }

    void HandleBoxingAttack()
    {
        if (isActionInProgress) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= boxingRange)
        {
            int randomAttack = Random.Range(1, 3);
            animator.SetTrigger(randomAttack == 1 ? "boxAttack" : "boxAttack2");
            audioSource.PlayOneShot(punchSound);
            isActionInProgress = true;
            StartCoroutine(EndActionAfterDelay(1.5f));
        }
    }

    void ResetEarthquakeCooldown()
    {
        earthquakeCooldown = Random.Range(3f, 5f);
    }

    IEnumerator EndActionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isActionInProgress = false;
    }

    public void EarthquakeDamage()
    {
        if (player == null || playerCombatScript == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (IsPlayerGrounded() || distance <= boxingRange)
        {
            playerCombatScript.health -= 30;
            if (playerHitSound != null)
                audioSource.PlayOneShot(playerHitSound);

            if (playerAnimator != null && !playerAnimator.IsInTransition(0))
            {
                AnimatorStateInfo info = playerAnimator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName("Idle"))
                    playerAnimator.SetTrigger("HitReaction");
            }
        }
    }

    public void BoxingDamage()
    {
        if (player == null || playerCombatScript == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= boxingRange + 1f)
        {
            playerCombatScript.health -= 30;

            if (playerHitSound != null)
                audioSource.PlayOneShot(playerHitSound);

            if (playerAnimator != null && !playerAnimator.IsInTransition(0))
            {
                playerAnimator.SetTrigger("knockedDown");
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                // 🔁 Direction from Pharaoh to Player (so knockback goes *away* from Pharaoh)
                Vector3 directionToPlayer = (player.position - transform.position);
                directionToPlayer.y = 0f; // Prevent vertical knockback
                Vector3 knockbackDirection = directionToPlayer.normalized;

                StartCoroutine(ApplyKnockback(controller, knockbackDirection, 10f, 0.5f));
            }
        }
    }


    IEnumerator ApplyKnockback(CharacterController controller, Vector3 direction, float distance, float duration)
    {
        float elapsed = 0f;
        Vector3 totalDisplacement = direction * distance;

        while (elapsed < duration)
        {
            float delta = Time.deltaTime / duration;
            Vector3 move = totalDisplacement * delta;

            controller.Move(move);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }




    bool IsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.2f);
    }


    bool IsPlayerGrounded()
    {
        return Physics.Raycast(player.position + Vector3.up * 0.1f, Vector3.down, out _, 0.2f);
    }

    public void EnterFlyingPhase()
    {
        if (currentPhase == BossPhase.Flying) return;

        Debug.Log("Entering Flying Phase");

        currentPhase = BossPhase.Flying;
        isInvincible = true;
        animator.SetBool("isFlying", true);
        StartCoroutine(FlyingRoutine());
    }

    IEnumerator FlyingRoutine()
    {
        float duration = rockTravelDuration;

        // Ascend
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * flyingHeight;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;

        // Wait before summoning
        yield return new WaitForSeconds(1f);

        // Summon rocks
        audioSource.PlayOneShot(summonRockSound);
        animator.SetTrigger("Summon");
        yield return StartCoroutine(SummonRocksAbovePharaoh());

        // Wait before throwing
        yield return new WaitForSeconds(1f);

        // Start throwing
        animator.SetBool("isThrowingRocks", true);
        yield return StartCoroutine(FireSummonedRocksRoutine());
        animator.SetBool("isThrowingRocks", false);


        // Descend
        startPos = transform.position;
        Vector3 landPos = startPos - Vector3.up * flyingHeight;
        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, landPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = landPos;

        animator.SetBool("isFlying", false);
        currentPhase = BossPhase.Vulnerable;
        animator.SetBool("vulnerable", true);
        isInvincible = false;

        if (ankhParticles != null)
            ankhParticles.Stop();

        if (ankhLight != null)
            StartCoroutine(FadeAnkhLight(ankhLight, 0f, lightFadeDuration));

        if (ankhTrail!= null)
        {
            ankhTrail.SetActive(false);
        }

        yield return new WaitForSeconds(7f); // Vulnerability duration

        if (ankhParticles != null)
            ankhParticles.Play();

        if (ankhLight != null)
            StartCoroutine(FadeAnkhLight(ankhLight, 3f, lightFadeDuration));

        if (ankhTrail != null)
        {
            ankhTrail.SetActive(true);
        }

        animator.SetBool("vulnerable", false);
        currentPhase = BossPhase.Melee;
        phaseTimer = meleeDuration;
        isInvincible = true;
        hasEnteredFlyingPhase = false;
        ResetEarthquakeCooldown();
    }

    IEnumerator FadeAnkhLight(Light lightSource, float targetIntensity, float duration)
    {
        float startIntensity = lightSource.intensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            lightSource.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            yield return null;
        }

        lightSource.intensity = targetIntensity;
    }


    IEnumerator SummonRocksAbovePharaoh()
    {
        summonedRocks.Clear();

        for (int i = 0; i < rockSummonPoints.Count; i++)
        {
            GameObject rock = Instantiate(rockProjectilePrefab, rockSummonPoints[i].position, Quaternion.identity);

            Rigidbody rb = rock.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            summonedRocks.Add(rock);
        }

        Vector3[] targetPositions = new Vector3[summonedRocks.Count];
        for (int i = 0; i < summonedRocks.Count; i++)
        {
            float angle = (360f / summonedRocks.Count) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * circleRadius;
            targetPositions[i] = rockCircleCenter.position + offset;
        }

        float elapsed = 0f;
        while (elapsed < summonRiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / summonRiseDuration;

            for (int i = 0; i < summonedRocks.Count; i++)
            {
                if (summonedRocks[i])
                {
                    Vector3 start = rockSummonPoints[i].position;
                    summonedRocks[i].transform.position = Vector3.Lerp(start, targetPositions[i], t);
                }
            }

            yield return null;
        }

        for (int i = 0; i < summonedRocks.Count; i++)
        {
            if (summonedRocks[i])
                summonedRocks[i].transform.position = targetPositions[i];
        }
    }

    IEnumerator FireSummonedRocksRoutine()
    {
        audioSource.PlayOneShot(fireRockSound);
        yield return new WaitForSeconds(2f); // ⏳ wait before throwing

        for (int i = 0; i < summonedRocks.Count; i++)
        {
            GameObject rock = summonedRocks[i];
            if (rock)
            {
                // Re-enable physics before firing
                Rigidbody rb = rock.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = true;
                    rb.isKinematic = false;
                }

                // 🎵 Play throw sound
                if (rockThrowSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(rockThrowSound);
                    Debug.Log("rockSound played");
                }

                Vector3 targetPosition = player.position + new Vector3(
                    Random.Range(-1f, 1f),
                    1.5f + Random.Range(-0.5f, 0.5f),
                    Random.Range(-1f, 1f)
                );

                StartCoroutine(MoveRock(rock, targetPosition));
                yield return new WaitForSeconds(rockThrowInterval);
            }
        }

        summonedRocks.Clear();
    }


    private IEnumerator MoveRock(GameObject rock, Vector3 target)
    {
        float duration = rockTravelDuration;
        float elapsed = 0f;
        Vector3 start = rock.transform.position;

        while (elapsed < duration)
        {
            if (rock == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rock.transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        if (Vector3.Distance(player.position, target) <= 1.5f)
        {
            PlayerCombatAndHealth playerHealth = player.GetComponent<PlayerCombatAndHealth>();
            CharacterController controller = player.GetComponent<CharacterController>();
            Animator playerAnimator = player.GetComponent<Animator>();
        }



        Destroy(rock);
    }

    IEnumerator ApplyKnockbackToPlayer()
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        PlayerCombatAndHealth playerHealth = player.GetComponent<PlayerCombatAndHealth>();
        Animator playerAnimator = player.GetComponent<Animator>();

        if (controller == null || playerHealth == null) yield break;

        // Set invincibility
        playerHealth.isInvincible = true;

        // Knockback direction (away from Pharaoh)
        Vector3 direction = (player.position - transform.position);
        direction.y = 0f;
        Vector3 knockbackDirection = direction.normalized;

        float knockbackDistance = 10f;
        float knockbackDuration = 0.5f;
        float elapsed = 0f;

        Vector3 totalDisplacement = knockbackDirection * knockbackDistance;

        // Trigger animation (only if playerAnimator exists)
        if (playerAnimator != null && !playerAnimator.IsInTransition(0))
        {
            playerAnimator.SetTrigger("knockedDown");
        }

        // Apply knockback over time
        while (elapsed < knockbackDuration)
        {
            float delta = Time.deltaTime / knockbackDuration;
            Vector3 move = totalDisplacement * delta;

            controller.Move(move);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Clear invincibility
        playerHealth.isInvincible = false;
    }

    public void SpawnShockwave()
    {
        Vector3 spawnPosition = transform.position + new Vector3(0, -2.5f, 0); // Slightly below

        GameObject shockwave = Instantiate(shockwaveVFXPrefab, spawnPosition, Quaternion.Euler(90, 0, 0));
        Destroy(shockwave, 1f);
    }



    public void TakeDamage(int damage)
    {
        if (isDying || isInvincible) return;

        health -= damage;
        audioSource.PlayOneShot(damageSound);
        animator.SetTrigger("HitReaction");

        if (health <= 0)
        {
            isDying = true;
            animator.SetBool("isDying", true);
            isActionInProgress = false;
            gameObject.tag = "Untagged";
        }
    }

    public void ReleaseAnkh()
    {
        isAnkhReleasing = true;
        AnkhPickup pickup = ankh.GetComponent<AnkhPickup>();
        pickup.enabled = true;
        ankh.SetParent(null);
    }

}
