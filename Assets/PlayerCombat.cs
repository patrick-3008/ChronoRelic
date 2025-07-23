using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerCombatAndHealth : MonoBehaviour
{
    private Animator anim;
    private PlayerMovement playerMovement;
    private WeaponInventory weaponInventory;
    public GameObject Player;

    // Health & Damage
    public int health = 100;
    public int maxHealth = 100;
    public float hitTimer = 3f;
    private const float maxHitTimer = 3f;
    public TextMeshProUGUI healthText;
    public int potionCount = 0; // 🧪 Potions available to use
    public TextMeshProUGUI potionCountText; // NEW: Text object to display potion count
    public AudioClip potionDrinkClip; // Optional healing sound
    public ParticleSystem healingEffect;
    public bool isInvincible = false;


    // Spear Combat
    public GameObject spear;
    private bool isSpearAttacking = false;
    public int spearDamage;
    public bool spearUpgrade = false;


    // Punch Combat
    private bool isPunching = false;
    private bool isPunchLocked = false;
    private float punchTimer = 0f;
    private const float comboResetTime = 0.8f;
    private int punchComboStep = 0;
    public int fistsDamage;

    // Sword Combat
    public GameObject sword;
    private bool isSwordAttacking = false;
    private float swordComboTimer = 0f;
    private const float swordComboResetTime = 1.2f;
    private int swordComboStep = 0;
    public int khopeshDamage;
    public bool khopeshUpgrade = false;

    // Throwing Knives
    public GameObject knifePrefab;
    public Transform knifeSpawnPoint;
    public int knifeCount = 5;
    public TextMeshProUGUI knifeCountText; // NEW: Text object to display knife count
    public float knifeSpeed = 10f;
    public float knifeRange = 15f;
    private bool isThrowingKnife = false;

    // Combat Targeting
    private GameObject currentTargetEnemy;

    // Audio
    public AudioClip jump1Clip;
    public AudioClip jump2Clip;
    public AudioClip jump3Clip;
    public AudioClip swordSlashClip;
    public AudioClip punchClip;
    public AudioClip spearClip;
    public AudioClip sword1Clip;
    public AudioClip sword2Clip;
    public AudioClip throwKnifeClip;
    public AudioSource combatAudioSource; // Drag the second AudioSource here in the Inspector


    void Start()
    {
        anim = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        weaponInventory = GetComponent<WeaponInventory>();
        anim.SetBool("isDying", false);
        UpdateHealthText();
        UpdatePotionCountText(); // NEW: Initialize potion count display
        UpdateKnifeCountText(); // NEW: Initialize knife count display
        fistsDamage = 15;
        khopeshDamage = 25;
        spearDamage = 50;
    }

    void Update()
    {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Idle") || anim.GetCurrentAnimatorStateInfo(0).IsName("Combat Idle"))
        {
            anim.ResetTrigger("SecondSword");
        }

        // Press R to use a healing potion
        if (Input.GetKeyDown(KeyCode.R) && potionCount > 0 && health < maxHealth)
        {
            health = maxHealth;
            potionCount--;
            if (healingEffect != null)
            {
                healingEffect.transform.position = transform.position;
                healingEffect.Play();
            }
            UpdateHealthText();
            UpdatePotionCountText(); // NEW: Update potion count display after using a potion
            Debug.Log("🧪 Potion used. Health restored to max. Remaining potions: " + potionCount);

            if (potionDrinkClip != null && combatAudioSource != null)
            {
                combatAudioSource.PlayOneShot(potionDrinkClip);
            }
        }


        if (health <= 0)
        {
            if (anim != null && !anim.GetBool("isDying"))
            {
                anim.SetBool("isDying", true);
                gameObject.tag = "Untagged"; // Or "Default" if you created that tag manually
                if (combatAudioSource != null)
                    combatAudioSource.mute = true;

                Debug.Log("Player died. Tag changed to Untagged and audio muted.");
            }

            if (playerMovement != null && playerMovement.enabled)
            {
                playerMovement.enabled = false;
            }

            return;
        }




        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Second Punch") ||
            anim.GetCurrentAnimatorStateInfo(0).IsName("Sword 1") ||
            anim.GetCurrentAnimatorStateInfo(0).IsName("Spear Attack"))
        {
            isPunchLocked = true;
        }

        if (hitTimer < maxHitTimer)
            hitTimer += Time.deltaTime;

        // Auto-switch to fists if knives run out
        if (weaponInventory.currentWeapon != null &&
            weaponInventory.currentWeapon.name == "Throwing Knives" && knifeCount <= 0)
        {
            weaponInventory.EquipWeapon(0); // Ensure "Fists" is defined in inventory
            Debug.Log("Throwing Knives empty! Switching to Fists.");
        }

        if (Input.GetMouseButtonDown(0))
        {
            string equippedWeapon = weaponInventory.currentWeapon != null ? weaponInventory.currentWeapon.name : "Fists";

            if (equippedWeapon == "Spear" && !isSpearAttacking)
            {
                StartAttack(SpearAttack);
            }
            else if (equippedWeapon == "Khopesh")
            {
                StartAttack(SwordAttack);
            }
            else if (equippedWeapon == "Fists")
            {
                StartAttack(PunchAttack);
            }
            else if (equippedWeapon == "Throwing Knives")
            {
                if (knifeCount > 0)
                {
                    anim.SetTrigger("ThrowKnife");
                }
                else
                {
                    Debug.Log("No knives left!");
                }
            }
        }

        HandleComboTimers();
    }

    private void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = "Health: " + health;
        }
    }

    // NEW: Method to update potion count text
    public void UpdatePotionCountText()
    {
        if (potionCountText != null)
        {
            potionCountText.text = "" + potionCount;
        }
    }

    // NEW: Method to update knife count text
    public void UpdateKnifeCountText()
    {
        if (knifeCountText != null)
        {
            knifeCountText.text = "" + knifeCount;
        }
    }

    // Start an attack and rotate towards the nearest enemy if found
    private void StartAttack(System.Action attackAction)
    {
        GameObject closestEnemy = FindNearestEnemy(2f);
        if (closestEnemy != null)
        {
            StartCoroutine(RotateAndAttack(closestEnemy.transform, attackAction));
        }
        else
        {
            attackAction(); // No enemy nearby, attack normally
        }
    }

    // Finds the closest enemy within a certain range
    private GameObject FindNearestEnemy(float range)
    {
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, range);
        GameObject nearestEnemy = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestEnemy = enemy.gameObject;
                }
            }
        }
        return nearestEnemy;
    }

    // Smoothly rotates player to face the target before attacking
    private IEnumerator RotateAndAttack(Transform target, System.Action attackAction)
    {
        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        float rotationSpeed = 5f;
        float elapsedTime = 0f;
        while (elapsedTime < 0.3f) // Adjust rotation duration as needed
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, elapsedTime * rotationSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        attackAction();
    }

    private void PlayEnemyPunchDamageSound(GameObject enemy)
    {
        AudioSource audio = enemy.GetComponent<AudioSource>();
        if (audio == null)
        {
            Debug.LogWarning("Enemy has no AudioSource!");
            return;
        }

        AudioClip[] clips = null;

        var spear = enemy.GetComponent<SpearThrowingEnemy>();
        if (spear != null && spear.punchContactSounds != null && spear.punchContactSounds.Length > 0)
        {
            clips = spear.punchContactSounds;
        }

        var sword = enemy.GetComponent<EnemyFollowAndEvadeWithAnimation>();
        if (sword != null && sword.punchContactSounds != null && sword.punchContactSounds.Length > 0)
        {
            clips = sword.punchContactSounds;
        }

        var guard = enemy.GetComponent<TempleGuard>();
        if (guard != null && guard.punchContactSounds != null && guard.punchContactSounds.Length > 0)
        {
            clips = guard.punchContactSounds;
        }

        if (clips != null && clips.Length > 0)
        {
            AudioClip randomClip = clips[Random.Range(0, clips.Length)];
            audio.PlayOneShot(randomClip);
        }
        else
        {
            Debug.LogWarning("No punch contact sounds assigned to enemy.");
        }
    }

    private void PlayEnemyDamageSound(GameObject enemy)
    {
        AudioSource audio = enemy.GetComponent<AudioSource>();
        if (audio != null)
        {
            // Get the enemy's custom damage sound
            AudioClip clip = null;

            var spear = enemy.GetComponent<SpearThrowingEnemy>();
            if (spear != null && spear.damageSound != null)
            {
                clip = spear.damageSound;
            }

            var sword = enemy.GetComponent<EnemyFollowAndEvadeWithAnimation>();
            if (sword != null && sword.damageSound != null)
            {
                clip = sword.damageSound;
            }

            var guard = enemy.GetComponent<TempleGuard>();
            if (guard != null && guard.damageSound != null)
            {
                clip = guard.damageSound;
            }

            if (clip != null)
            {
                audio.PlayOneShot(clip);
            }
            else
            {
                Debug.LogWarning("No damage sound assigned to enemy.");
            }
        }
        else
        {
            Debug.LogWarning("Enemy has no AudioSource!");
        }
    }



    private void ApplyDamageToEnemy(int damage)
    {
        GameObject enemy = FindNearestEnemy(2f);
        if (enemy != null)
        {
            SpearThrowingEnemy spearEnemy = enemy.GetComponent<SpearThrowingEnemy>();
            EnemyFollowAndEvadeWithAnimation swordEnemy = enemy.GetComponent<EnemyFollowAndEvadeWithAnimation>();
            TempleGuard guardEnemy = enemy.GetComponent<TempleGuard>();
            PharaohBoss pharaoh = enemy.GetComponent<PharaohBoss>();

            if (spearEnemy != null)
            {
                spearEnemy.TakeDamage(damage);
                PlayEnemyPunchDamageSound(enemy);
                Debug.Log("✅ Damage applied to SpearThrowingEnemy: " + damage);
            }
            else if (swordEnemy != null)
            {
                swordEnemy.TakeDamage(damage);
                PlayEnemyPunchDamageSound(enemy);
                Debug.Log("✅ Damage applied to Melee Enemy: " + damage);
            }
            else if (guardEnemy != null)
            {
                guardEnemy.TakeDamage(damage);
                PlayEnemyPunchDamageSound(enemy);
                Debug.Log("✅ Damage applied to Guard Enemy: " + damage);
            }
            else if (pharaoh != null)
            {
                //pharaoh.TakeDamage(damage);
                //PlayEnemyPunchDamageSound(enemy);
                Debug.Log("Pharaoh is not affected by Punnches");
            }
            else
            {
                Debug.LogWarning("❌ Enemy found, but no TakeDamage function found!");
            }
        }
        else
        {
            Debug.LogWarning("❌ No enemy found in range!");
        }
    }

    private void ApplySwordDamageToEnemy(int damage)
    {
        if (khopeshUpgrade == true)
            damage += 15;

        GameObject enemy = FindNearestEnemy(2f);
        if (enemy != null)
        {
            SpearThrowingEnemy spearEnemy = enemy.GetComponent<SpearThrowingEnemy>();
            EnemyFollowAndEvadeWithAnimation swordEnemy = enemy.GetComponent<EnemyFollowAndEvadeWithAnimation>();
            PharaohBoss pharaoh = enemy.GetComponent<PharaohBoss>();

            if (spearEnemy != null)
            {
                spearEnemy.TakeDamage(damage);
                PlayEnemyDamageSound(enemy);
                Debug.Log("✅ Damage applied to SpearThrowingEnemy: " + damage);
            }
            else if (swordEnemy != null)
            {
                swordEnemy.TakeDamage(damage);
                PlayEnemyDamageSound(enemy);
                Debug.Log("✅ Damage applied to Melee Enemy: " + damage);
            }
            else if (pharaoh != null && !pharaoh.isInvincible)
            {
                pharaoh.TakeDamage(damage);
                PlayEnemyDamageSound(enemy);
                Debug.Log("✅ Damage applied to Pharaoh: " + damage);
            }
            else
            {
                Debug.LogWarning("❌ Enemy found, but no TakeDamage function found!");
            }
        }
        else
        {
            Debug.LogWarning("❌ No enemy found in range!");
        }
    }


    private void ApplySpearDamageToEnemy(int damage)
    {
        if (spearUpgrade == true)
            damage += 25;

        GameObject enemy = FindNearestEnemy(2f);
        if (enemy != null)
        {
            SpearThrowingEnemy spearEnemy = enemy.GetComponent<SpearThrowingEnemy>();
            EnemyFollowAndEvadeWithAnimation swordEnemy = enemy.GetComponent<EnemyFollowAndEvadeWithAnimation>();
            PharaohBoss pharaoh = enemy.GetComponent<PharaohBoss>();

            if (spearEnemy != null)
            {
                spearEnemy.TakeDamage(damage);
                PlayEnemyDamageSound(enemy);
                Debug.Log("✅ Damage applied to SpearThrowingEnemy: " + damage);
            }
            else if (swordEnemy != null)
            {
                swordEnemy.TakeDamage(damage);
                PlayEnemyDamageSound(enemy);
                Debug.Log("✅ Damage applied to Melee Enemy: " + damage);
            }
            else if (pharaoh != null && !pharaoh.isInvincible)
            {
                pharaoh.TakeDamage(damage);
                PlayEnemyDamageSound(enemy);
                Debug.Log("✅ Damage applied to Pharaoh: " + damage);
            }
            else
            {
                Debug.LogWarning("❌ Enemy found, but no TakeDamage function found!");
            }
        }
        else
        {
            Debug.LogWarning("❌ No enemy found in range!");
        }
    }



    private void SpearAttack()
    {
        anim.SetTrigger("SpearAttack");
        isSpearAttacking = true;
        StartCoroutine(ResetSpearRotationAfterDelay(1.5f));
    }

    private IEnumerator ResetSpearRotationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isSpearAttacking = false;
    }

    private void SwordAttack()
    {
        if (swordComboStep == 0 && !isSwordAttacking)
        {
            isSwordAttacking = true;
            swordComboStep = 1;
            anim.SetTrigger("FirstSword");
            swordComboTimer = swordComboResetTime;
        }
        else if (swordComboStep == 1 && swordComboTimer > 0f)
        {
            swordComboStep = 2;
            anim.SetTrigger("SecondSword");
            swordComboTimer = 0f;
        }
    }

    private void PunchAttack()
    {
        if (isPunchLocked) return;

        if (!isPunching)
        {
            isPunching = true;
            punchComboStep = 1;
            anim.SetTrigger("FirstPunch");
            punchTimer = comboResetTime;
        }
        else if (punchComboStep == 1 && punchTimer > 0f)
        {
            punchComboStep = 2;
            anim.SetTrigger("SecondPunch");
            punchTimer = 0f;
        }
        else if (anim.GetCurrentAnimatorStateInfo(0).IsName("First Punch"))
        {
            punchComboStep = 2;
            anim.SetTrigger("SecondPunch");
            punchTimer = 0f;
        }
    }

    private void ThrowKnife()
    {
        if (knifeCount <= 0)
        {
            Debug.Log("No knives left!");
            return;
        }

        if (knifePrefab == null || knifeSpawnPoint == null)
        {
            Debug.LogError("KnifePrefab or KnifeSpawnPoint is missing!");
            return;
        }

        knifeCount--;
        UpdateKnifeCountText(); // NEW: Update knife count display after throwing a knife

        if (knifeCount <= 0)
        {
            knifePrefab.SetActive(false);
            Debug.Log("Throwing Knives depleted! Disabling knifePrefab.");
        }

        GameObject targetEnemy = FindNearestEnemy(knifeRange);
        GameObject knife = Instantiate(knifePrefab, knifeSpawnPoint.position, Quaternion.identity);
        knife.transform.rotation = Quaternion.Euler(0, 0, 0);
        Debug.Log("Knife instantiated!");
        knife.transform.rotation = transform.rotation;

        if (targetEnemy != null)
        {
            StartCoroutine(ThrowKnifeAtTarget(knife, targetEnemy, Player.transform));
        }
        else
        {
            StartCoroutine(ThrowKnifeStraight(knife));
        }

        Debug.Log("Knife thrown!");
    }

    private IEnumerator ThrowKnifeStraight(GameObject knife)
    {
        float distanceTraveled = 0f;
        Debug.Log("Knife moving...");

        // Rotate the knife 90 degrees around its right (X) axis to face forward
        knife.transform.rotation *= Quaternion.Euler(0, 90, 0);

        while (distanceTraveled < knifeRange)
        {
            float moveStep = knifeSpeed * Time.deltaTime;
            knife.transform.Translate(Player.transform.forward * moveStep, Space.World);
            distanceTraveled += moveStep;
            yield return null;
        }
        Destroy(knife);
    }



    private IEnumerator ThrowKnifeAtTarget(GameObject knife, GameObject target, Transform player)
    {
        if (target != null && player != null)
        {
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.lockRotation = true;
                StartCoroutine(RotatePlayerTowardsTarget(player, target.transform.position, movement));
            }
        }

        Vector3 targetPosition = target.transform.position + new Vector3(0, 2f, 0); // Adjust for height

        while (knife != null && target != null)
        {
            Vector3 direction = (targetPosition - knife.transform.position).normalized;
            knife.transform.position += direction * knifeSpeed * Time.deltaTime;

            knife.transform.LookAt(targetPosition);
            knife.transform.rotation *= Quaternion.Euler(0, 90, 0); // Knife model offset

            if (Vector3.Distance(knife.transform.position, targetPosition) < 0.2f)
            {
                // Damage handling
                EnemyFollowAndEvadeWithAnimation swordEnemy = target.GetComponent<EnemyFollowAndEvadeWithAnimation>();
                SpearThrowingEnemy spearEnemy = target.GetComponent<SpearThrowingEnemy>();
                PharaohBoss pharaoh = target.GetComponent<PharaohBoss>();

                if (swordEnemy != null)
                {
                    swordEnemy.TakeDamage(40);
                    Debug.Log("✅ Sword enemy hit by knife! -40 HP");
                    PlayEnemyDamageSound(target);
                }
                else if (spearEnemy != null)
                {
                    spearEnemy.TakeDamage(40);
                    Debug.Log("✅ Spear enemy hit by knife! -40 HP");
                    PlayEnemyDamageSound(target);
                }
                else if (pharaoh != null)
                {
                    pharaoh.TakeDamage(40);
                    Debug.Log("✅ Pharaoh hit by knife! -40 HP");
                    PlayEnemyDamageSound(target);
                }
                else
                {
                    Debug.LogWarning("❌ Target hit, but no valid enemy script found!");
                }

                Destroy(knife);
                break;
            }

            yield return null;
        }
    }



    // Separate coroutine for smooth rotation
    private IEnumerator RotatePlayerTowardsTarget(Transform player, Vector3 targetPosition, PlayerMovement movement)
    {
        Vector3 direction = (targetPosition - player.position).normalized;
        direction.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        float elapsedTime = 0f;
        float duration = 0.3f;

        while (elapsedTime < duration)
        {
            player.rotation = Quaternion.Slerp(player.rotation, targetRotation, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        player.rotation = targetRotation;

        if (movement != null)
            movement.lockRotation = false;
    }


    private void HandleComboTimers()
    {
        if (punchTimer > 0f) punchTimer -= Time.deltaTime;
        if (swordComboTimer > 0f) swordComboTimer -= Time.deltaTime;

        if (punchTimer <= 0f)
        {
            isPunching = false;
            punchComboStep = 0;
            isPunchLocked = false;
        }

        if (swordComboTimer <= 0f)
        {
            isSwordAttacking = false;
            swordComboStep = 0;
        }
    }

    public void PlaySwordSlashSound()
    {
        if (swordSlashClip != null && combatAudioSource != null)
        {
            combatAudioSource.PlayOneShot(swordSlashClip);
        }
        else
        {
            Debug.LogWarning("❌ Sword sound clip or combatAudioSource is missing!");
        }
    }

    public void AttackSound()
    {
        if (throwKnifeClip != null && combatAudioSource != null && weaponInventory.currentWeapon.name == "Throwing Knives" && knifeCount > 0)
        {
            combatAudioSource.PlayOneShot(throwKnifeClip);
        }
        else if (punchClip != null && combatAudioSource != null && weaponInventory.currentWeapon.name == "Fists")
        {
            combatAudioSource.PlayOneShot(punchClip);
        }
        else if (punchClip != null && combatAudioSource != null && weaponInventory.currentWeapon.name == "Spear")
        {
            combatAudioSource.PlayOneShot(spearClip);
        }
        else if (sword1Clip != null && sword2Clip != null && combatAudioSource != null && weaponInventory.currentWeapon.name == "Khopesh")
        {
            AudioClip chosenClip = (Random.value < 0.5f) ? sword1Clip : sword2Clip;
            combatAudioSource.PlayOneShot(chosenClip);
        }

        else
        {
            Debug.LogWarning("❌ Sword sound clip or combatAudioSource is missing!");
        }
    }

    public void JumpSound()
    {
        if (jump1Clip != null && jump2Clip != null && jump3Clip != null && combatAudioSource != null)
        {
            int randomIndex = Random.Range(0, 3); // 0, 1, or 2
            AudioClip chosenClip = null;

            switch (randomIndex)
            {
                case 0:
                    chosenClip = jump1Clip;
                    break;
                case 1:
                    chosenClip = jump2Clip;
                    break;
                case 2:
                    chosenClip = jump3Clip;
                    break;
            }

            combatAudioSource.PlayOneShot(chosenClip);
        }
    }

}