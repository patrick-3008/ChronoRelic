using UnityEngine;
using TMPro;

public class newMedic : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public string npcName = "Temple Medic"; // NEW: NPC name to display
    public string emptyDialogue = " ";
    public string sideMissionLine1 = "Hello traveler, I'm in need of a troublesome favor.";
    public string sideMissionLine2 = "Help me defeat the bandits camping near this sacred temple, and I will grant you a reward praised by the Gods.";
    public string healedDialogue = "Your wounds are healed. Take this mystical potion with you, it will greatly aid you on your quest.";
    private bool hasSeenLine1 = false;

    public TextMeshProUGUI dialogueTextBox;
    public GameObject dialogueUI;
    public GameObject npcModel; // drag your child model here in Inspector
    public ParticleSystem healingEffect;

    private Animator anim;
    private bool isPlayerNearby = false;
    private Transform playerTransform;
    private bool isInteracting = false;
    private bool dialogueActive = false; // NEW: Track if dialogue is currently active
    private PlayerCombatAndHealth playerCombat;
    public float rotationSpeed = 5f;

    private bool hasSpawned = false;

    public AudioClip interactionSound;
    private AudioSource audioSource;

    void Start()
    {
        dialogueUI.SetActive(false);
        npcModel.SetActive(false); // model starts hidden
        audioSource = GetComponent<AudioSource>();

        // NEW: Enable text wrapping
        if (dialogueTextBox != null)
        {
            dialogueTextBox.enableWordWrapping = true;
            dialogueTextBox.overflowMode = TextOverflowModes.Overflow;
        }
    }

    void Update()
    {
        if (!hasSpawned && MissionManager.Instance.mission1_complete)
        {
            SpawnNPC();
        }

        // NEW: Modified input handling for dialogue system
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (!dialogueActive)
            {
                // Start dialogue
                StartDialogue();
            }
            else
            {
                // Advance or end dialogue
                AdvanceDialogue();
            }
        }

        if (isInteracting)
        {
            RotateTowardsPlayer();
        }
    }

    private void SpawnNPC()
    {
        npcModel.SetActive(true);
        anim = npcModel.GetComponent<Animator>();
        hasSpawned = true;
    }

    // NEW: Start dialogue method
    private void StartDialogue()
    {
        dialogueUI.SetActive(true);
        dialogueActive = true;
        isInteracting = true;

        // Trigger talking animation
        if (anim != null)
        {
            anim.SetTrigger("Talking");
        }

        // Check mission state and display appropriate dialogue
        if (MissionManager.Instance.missionside_complete)
        {
            // Mission complete - heal player and give reward
            DisplayDialogue(healedDialogue);
            HealPlayer();
        }
        else
        {
            // Show first line of side mission
            DisplayDialogue(sideMissionLine1);
            hasSeenLine1 = true;
        }

        // Play interaction sound
        if (interactionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(interactionSound);
        }
    }

    // NEW: Advance dialogue method
    private void AdvanceDialogue()
    {
        if (MissionManager.Instance.missionside_complete)
        {
            // Mission complete - close dialogue
            EndDialogue();
            return;
        }

        if (hasSeenLine1)
        {
            // Show second line of side mission
            DisplayDialogue(sideMissionLine2);
            hasSeenLine1 = false; // Reset for next interaction
        }
        else
        {
            // Close dialogue
            EndDialogue();
        }

        // Play interaction sound
        if (interactionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(interactionSound);
        }
    }

    // NEW: End dialogue method
    private void EndDialogue()
    {
        dialogueUI.SetActive(false);
        dialogueActive = false;
        isInteracting = false;
        dialogueTextBox.text = "";
    }

    // NEW: Display dialogue with NPC name
    private void DisplayDialogue(string dialogue)
    {
        if (dialogueTextBox != null)
        {
            // Format the text with yellow NPC name
            string formattedText = $"<color=yellow>{npcName}:</color> {dialogue}";
            dialogueTextBox.text = formattedText;
        }
    }

    // NEW: Heal player method
    private void HealPlayer()
    {
        if (playerCombat != null)
        {
            playerCombat.health = playerCombat.maxHealth;
            Debug.Log("Player fully healed by New Medic!");
            playerCombat.potionCount += 1;
            playerCombat.UpdatePotionCountText(); // Update UI display

            if (healingEffect != null)
            {
                healingEffect.transform.position = playerCombat.transform.position;
                healingEffect.Play();
            }
        }
    }

    // OLD METHOD - Keeping for reference but not used anymore
    private void ToggleDialogueAndRotation()
    {
        bool isActive = dialogueUI.activeSelf;
        dialogueUI.SetActive(true); // Always enable UI if interacting

        if (!hasSeenLine1)
        {
            if (MissionManager.Instance.missionside_complete)
            {
                dialogueTextBox.text = healedDialogue;

                if (playerCombat != null)
                {
                    playerCombat.health = playerCombat.maxHealth;
                    Debug.Log("Player fully healed by SideMissionMedicNPC!");
                    playerCombat.potionCount += 1;

                    if (healingEffect != null)
                    {
                        healingEffect.transform.position = playerCombat.transform.position;
                        healingEffect.Play();
                    }
                }

                isInteracting = true;
            }
            else
            {
                dialogueTextBox.text = sideMissionLine1;
                hasSeenLine1 = true;
                isInteracting = true;
            }
        }
        else
        {
            if (!MissionManager.Instance.missionside_complete)
            {
                dialogueTextBox.text = sideMissionLine2;
            }
            else
            {
                dialogueTextBox.text = healedDialogue; // Redundant, but fallback-safe
            }

            hasSeenLine1 = false;
            isInteracting = false;
            dialogueUI.SetActive(false); // Close dialogue on second press
        }

        anim?.SetTrigger("Talking");

        if (interactionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(interactionSound);
        }
    }

    // OLD METHOD - Keeping for reference but not used anymore
    private void StartInteraction()
    {
        dialogueUI.SetActive(true);
        isInteracting = true;
        anim?.SetTrigger("Talking");

        if (MissionManager.Instance.missionside_complete)
        {
            dialogueTextBox.text = healedDialogue;

            if (playerCombat != null)
            {
                playerCombat.health = playerCombat.maxHealth;
                Debug.Log("Player fully healed by SideMissionMedicNPC!");
                playerCombat.potionCount += 1;

                if (healingEffect != null)
                {
                    healingEffect.transform.position = playerCombat.transform.position;
                    healingEffect.Play();
                }
            }
        }
        else
        {
            dialogueTextBox.text = sideMissionLine1;
            hasSeenLine1 = true;
        }

        if (interactionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(interactionSound);
        }
    }

    private void RotateTowardsPlayer()
    {
        if (playerTransform == null) return;

        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            playerTransform = other.transform;

            playerCombat = other.GetComponent<PlayerCombatAndHealth>();

            if (playerCombat == null)
            {
                Debug.LogError("PlayerCombatAndHealth script not found on the player object.");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            playerTransform = null;
            playerCombat = null;

            // NEW: End dialogue when player leaves
            if (dialogueActive)
            {
                EndDialogue();
            }
        }
    }
}