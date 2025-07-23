using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class SubZeroMove : MonoBehaviour
{
    public CharacterController charController;
    Animator anim;
    public new Camera camera;

    public GameObject spear; // Reference to the spear object

    private float gravityY = 0.0f;
    public float mass = 1.0f;
    bool isJumping;
    bool isGrounded;
    //public float timer = 360.0f;
    public int coins = 0;
    bool isRunning = false;
    bool isCrouching = false;
    float lerpRun = 0.0f;
    float lerpCrouch = 0.0f;
    public bool isSpearAttacking = false;
    private bool isPunching = false;
    private float punchTimer = 0f;
    private const float comboResetTime = 1f; // Time window for triggering the second punch
    private int punchComboStep = 0; // 0: No punch, 1: First punch, 2: Second punch
    public float jumpCooldownTimer = 0f; // Timer to track cooldown
    public float jumpCooldown = 1f; // Cooldown duration (2 seconds)

    public GameObject sword; // Reference to the sword object
    private bool isSwordAttacking = false;
    public float swordComboTimer = 0f; // Timer for sword combo
    private const float swordComboResetTime = 1f; // Time window for triggering the second sword attack
    public int swordComboStep = 0; // 0: No attack, 1: First attack, 2: Second attack
    public int health = 100;
    public float hitTimer = 3;

    public int gold = 100;
    public TextMeshProUGUI goldText; // Drag the TextMeshPro object here

    public AudioSource footstepSource; // Reference to the AudioSource for footsteps
    public AudioClip[] footstepClips; // Array of footstep sounds
    public float walkFootstepInterval = 0.4f; // Time interval between walking footsteps
    public float runFootstepInterval = 0.25f;
    private float footstepTimer = 0f; // Timer to track footstep intervals

    private Vector3 jumpMomentum = Vector3.one; // Store momentum for jumping

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }
        else
            UnityEngine.Cursor.lockState = CursorLockMode.None;
    }

    void Start()
    {
        charController = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        isGrounded = true;
        //timer = 360.0f;
        anim.SetBool("Lost", false);
        anim.SetBool("Win", false);
        anim.SetBool("isDying", false);

        // Initialize the footstep audio source
        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.loop = false; // Play one footstep at a time
            footstepSource.playOnAwake = false;
        }
    }

    void Update()
    {
        UpdateGoldText(); // Initialize the text display

        if (health <= 0)
        {
            anim.SetBool("isDying", true);
            return;
        }

        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }

        HandleFootsteps();

        float dX = Input.GetAxis("Horizontal");
        float dY = Input.GetAxis("Vertical");

        Vector3 movementVector = new Vector3(dX, 0, dY);
        movementVector = Quaternion.AngleAxis(camera.transform.eulerAngles.y, Vector3.up) * movementVector;
        movementVector.Normalize();

        gravityY += Physics.gravity.y * mass * Time.deltaTime;

        if (charController.isGrounded)
        {
            gravityY = -0.5f;
            isJumping = false;
            anim.SetBool("IsJumping", false);
            isGrounded = true;
            anim.SetBool("IsGrounded", true);
            anim.SetBool("IsFalling", false);

            // Jump logic with cooldown
            if (Input.GetKeyDown(KeyCode.Space) && jumpCooldownTimer <= 0f)
            {
                isJumping = true;
                anim.SetBool("IsJumping", true);
                if (anim.GetFloat("Walk_Run") < 0.6)
                    gravityY = 3.0f; // Initial jump force

                // Store current movement as jump momentum
                jumpMomentum = movementVector * (isRunning ? 4.0f : 2.0f); // Scale based on running or walking

                // Reset jump cooldown timer
                jumpCooldownTimer = jumpCooldown;
            }
        }
        else
        {
            isGrounded = false;
            anim.SetBool("IsGrounded", false);

            if (isJumping && gravityY < -3 || gravityY < -6)
                anim.SetBool("IsFalling", true);
        }

        // Use momentum in the air
        Vector3 newMoveVector = isGrounded ? movementVector : jumpMomentum;
        newMoveVector.y = gravityY;

        Physics.SyncTransforms();

        // Crouch Logic
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = true;
            anim.SetBool("IsCrouching", true);
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isCrouching = false;
            anim.SetBool("IsCrouching", false);
        }

        if (isCrouching)
        {
            // Smoothly transition crouch-walk blend value
            float crouchSpeed = movementVector.magnitude > 0 ? 1.0f : 0.0f;
            lerpCrouch = Mathf.Lerp(lerpCrouch, crouchSpeed, 2.0f * Time.deltaTime);
            anim.SetFloat("Crouch_Walk", lerpCrouch);
        }

        // Walk/Run Logic
        if (!isCrouching)
        {
            if (isRunning)
            {
                float delta = Mathf.Lerp(0, 1, lerpRun);
                if (lerpRun < 1.0f)
                    lerpRun += 2.0f * Time.deltaTime;
                anim.SetFloat("Walk_Run", delta);
            }
            else
            {
                float targetWalkValue = 0.5f;
                float delta = Mathf.Lerp(anim.GetFloat("Walk_Run"), targetWalkValue, 1.0f * Time.deltaTime);
                anim.SetFloat("Walk_Run", delta);
            }
        }

        // Character Rotation
        if (movementVector != Vector3.zero)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                isRunning = true;
                lerpRun = 0.0f;
            }
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                isRunning = false;
                lerpRun = 0.0f;
            }
            Quaternion rotationDirection = Quaternion.LookRotation(movementVector, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationDirection, 360 * Time.deltaTime);

            anim.SetBool("Walking", true);
        }
        else
        {
            anim.SetBool("Walking", false);
        }

        charController.Move(newMoveVector * Time.deltaTime);

        if (Input.GetMouseButtonDown(0)) // Left click
        {
            // Spear attack logic
            if (spear != null && spear.activeSelf && !isSpearAttacking)
            {
                anim.SetTrigger("SpearAttack");
                isSpearAttacking = true;

                // Rotate the spear's Y-axis
                Quaternion spearRotation = spear.transform.rotation;
                spear.transform.localRotation = Quaternion.Euler(0, 180, 30);
                spear.transform.localPosition = new Vector3(-1.231f, 0.851f, -0.024f);

                // Start coroutine to reset spear rotation
                StartCoroutine(ResetSpearRotationAfterDelay(1.5f));
            }
            // Sword attack logic
            else if (sword != null && sword.activeSelf)
            {
                if (swordComboStep == 0 && !isSwordAttacking)
                {
                    // Start first attack
                    isSwordAttacking = true;
                    swordComboStep = 1;
                    anim.SetTrigger("FirstSword");
                    swordComboTimer = swordComboResetTime; // Start combo timer
                }
                else if (swordComboStep == 1 && swordComboTimer > 0f)
                {
                    // Trigger second attack within combo window
                    swordComboStep = 2;
                    anim.SetTrigger("SecondSword");
                    swordComboTimer = 0f; // Reset the timer as the combo is complete
                }
            }
            // Punch attack logic
            else if (!sword.activeSelf && !spear.activeSelf)
            {
                if (!isPunching)
                {
                    isPunching = true;
                    punchComboStep = 1;
                    anim.SetTrigger("FirstPunch");
                    punchTimer = comboResetTime; // Start combo timer
                }
                else if (punchComboStep == 1 && punchTimer > 0f)
                {
                    punchComboStep = 2;
                    anim.SetTrigger("SecondPunch");
                    punchTimer = 0f;
                }
            }
        }
        if (punchComboStep == 2)
        {
            punchComboStep = 0;
            isPunching = false;
        }
        if (swordComboStep == 2)
        {
            swordComboStep = 0;
            isSwordAttacking = false;
        }
        if (punchTimer > 0f)
        {
            punchTimer -= Time.deltaTime;

            if (punchTimer <= 0f)
            {
                isPunching = false;
                punchComboStep = 0;
            }
        }
        if (swordComboTimer > 0f)
        {
            swordComboTimer -= Time.deltaTime;

            if (swordComboTimer <= 0f)
            {
                isSwordAttacking = false;
                swordComboStep = 0; // Reset combo if timer expires
            }
        }

        if (isPunching)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.IsName("FirstPunch") && stateInfo.normalizedTime >= 1f ||
                stateInfo.IsName("SecondPunch") && stateInfo.normalizedTime >= 1f)
            {
                isPunching = false;
                punchComboStep = 0;
            }
        }

        //Health System
        if (health < 0)
        {
            health = 0;
        }
        else if (health > 100)
        {
            health = 100;
        }

        //hitTimer increase
        if (hitTimer < 3)
            hitTimer += Time.deltaTime * 1;
    }

    private IEnumerator ResetSpearRotationAfterDelay(float delay)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);

        isSpearAttacking = false;

        // Target values for rotation and position
        Quaternion targetRotation = Quaternion.Euler(0, 180, 0);
        Vector3 targetPosition = new Vector3(-1.424997f, 0.1230005f, 0f);

        // Store initial values
        Quaternion initialRotation = spear.transform.localRotation;
        Vector3 initialPosition = spear.transform.localPosition;

        // Duration of the Lerp
        float duration = 1.0f; // 1 second
        float elapsedTime = 0f;

        // Lerp over time
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Lerp rotation and position
            spear.transform.localRotation = Quaternion.Lerp(initialRotation, targetRotation, t);
            spear.transform.localPosition = Vector3.Lerp(initialPosition, targetPosition, t);

            yield return null; // Wait for the next frame
        }

        // Ensure the final values are set (in case of slight precision errors)
        spear.transform.localRotation = targetRotation;
        spear.transform.localPosition = targetPosition;
    }



    private void OnAnimatorMove()
    {
        Vector3 _move = anim.deltaPosition;
        _move.y = gravityY * Time.deltaTime;
        charController.Move(_move);
    }

    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log("Gold added! Current gold: " + gold);
        UpdateGoldText();
    }

    private void UpdateGoldText()
    {
        if (goldText != null)
        {
            goldText.text = "Gold: " + gold;
        }
    }

    private void HandleFootsteps()
    {
        // Check if the player is moving and grounded
        if (charController.isGrounded && anim.GetBool("Walking"))
        {
            float walkRunValue = anim.GetFloat("Walk_Run");

            // Determine the interval based on walking or running
            float currentInterval = walkRunValue > 0.6f ? runFootstepInterval : walkFootstepInterval;

            footstepTimer -= Time.deltaTime;

            // Play a random footstep sound at regular intervals
            if (footstepTimer <= 0f)
            {
                PlayRandomFootstep();
                footstepTimer = currentInterval; // Reset the timer
            }
        }
        else
        {
            footstepTimer = 0f; // Reset timer when not moving
        }
    }

    private void PlayRandomFootstep()
    {
        if (footstepClips.Length > 0)
        {
            // Select a random clip from the array
            int randomIndex = Random.Range(0, footstepClips.Length);
            footstepSource.clip = footstepClips[randomIndex];
            footstepSource.Play();
        }
    }


    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (health > 0)
        {
            if (hit.gameObject.tag == "Hurt")
            {
                if (hitTimer >= 3)
                {
                    health -= 10;
                    hitTimer = 0;
                }
            }
        }
    }
}