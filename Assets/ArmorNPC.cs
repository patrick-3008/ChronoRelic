using UnityEngine;
using TMPro;

public class HealerMentorNPC : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public string npcName = "Armorsmith"; // NEW: NPC name to display
    public string npcDialogue = "I can make you tougher, upgrading your armor! It costs 75 gold. Do you accept?";
    public string successDialogue = "Your armor is now stronger!";
    public string failDialogue = "You lack the gold, traveler. The price is 75 gold.";
    public string alreadyUpgradedDialogue = "You've already received my blessing.";

    public TextMeshProUGUI dialogueTextBox;
    public GameObject dialogueUI;
    private Animator anim;

    private bool isPlayerNearby = false;
    private Transform playerTransform;
    private bool isInteracting = false;
    private bool dialogueActive = false; // NEW: Track if dialogue is currently active

    public float rotationSpeed = 5f;

    private PlayerGold playerGold;
    private PlayerCombatAndHealth playerCombat;
    private Renderer playerRenderer; // For material change

    private const int upgradeCost = 75;
    private const int newMaxHealth = 150;
    private bool hasUpgradedHealth = false; // Prevents multiple upgrades

    public AudioClip interactionSound;
    private AudioSource audioSource;

    void Start()
    {
        dialogueUI.SetActive(false);
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // NEW: Configure text wrapping
        if (dialogueTextBox != null)
        {
            dialogueTextBox.enableWordWrapping = true;
            dialogueTextBox.overflowMode = TextOverflowModes.Overflow;
        }

        GreetPlayer();
    }

    void Update()
    {
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
                // End dialogue
                EndDialogue();
            }
        }

        if (isInteracting)
        {
            RotateTowardsPlayer();

            // Upgrade controls only work when dialogue is active
            if (dialogueActive)
            {
                if (Input.GetKeyDown(KeyCode.Alpha6))
                {
                    UpgradeMaxHealth();
                }
            }
        }
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

        // Display dialogue with NPC name and upgrade instructions
        DisplayMainDialogue();

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

    // NEW: Display main dialogue with NPC name and upgrade instructions
    private void DisplayMainDialogue()
    {
        if (dialogueTextBox != null)
        {
            // Format the text with yellow NPC name and upgrade instructions
            string formattedText = $"<color=yellow>{npcName}:</color> {npcDialogue}";
            formattedText += "\n\n<color=yellow>Press 6 to upgrade your Armor.</color>";
            dialogueTextBox.text = formattedText;
        }
    }

    // NEW: Display dialogue with NPC name (for responses)
    private void DisplayDialogue(string dialogue)
    {
        if (dialogueTextBox != null)
        {
            // Format the text with yellow NPC name
            string formattedText = $"<color=yellow>{npcName}:</color> {dialogue}";
            dialogueTextBox.text = formattedText;
        }
    }

    // OLD METHOD - Keeping for reference but not used anymore
    private void ToggleDialogueAndRotation()
    {
        bool isActive = dialogueUI.activeSelf;
        dialogueUI.SetActive(!isActive);

        if (!isActive)
        {
            // ✅ Play interaction sound
            if (interactionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(interactionSound);
            }

            dialogueTextBox.text = npcDialogue;
            isInteracting = true;
        }
        else
        {
            isInteracting = false;
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

            playerGold = other.GetComponent<PlayerGold>();
            playerCombat = other.GetComponent<PlayerCombatAndHealth>();
            playerRenderer = other.GetComponentInChildren<Renderer>(); // Get player's Renderer

            if (playerGold == null)
            {
                Debug.LogError("PlayerGold script not found on the player object.");
            }

            if (playerCombat == null)
            {
                Debug.LogError("PlayerCombatAndHealth script not found on the player object.");
            }

            if (playerRenderer == null)
            {
                Debug.LogError("Renderer component not found on the player object.");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            playerTransform = null;
            playerGold = null;
            playerCombat = null;

            // NEW: End dialogue when player leaves
            if (dialogueActive)
            {
                EndDialogue();
            }
        }
    }

    void GreetPlayer()
    {
        Debug.Log("I can make you tougher, upgrading your armor! It costs 75 gold.");
    }

    private void UpgradeMaxHealth()
    {
        if (playerGold == null || playerCombat == null || playerRenderer == null) return;

        if (hasUpgradedHealth)
        {
            DisplayDialogue(alreadyUpgradedDialogue); // NEW: Use DisplayDialogue method
            return;
        }

        if (playerGold.gold >= upgradeCost)
        {
            playerGold.gold -= upgradeCost;
            playerCombat.maxHealth = newMaxHealth; // Set new max health
            playerCombat.health = newMaxHealth; // Fully heal the player
            hasUpgradedHealth = true; // Mark upgrade as done

            // Change player's material metallic value to 1
            Material playerMaterial = playerRenderer.material;
            playerMaterial.SetFloat("_Metallic", 1f);

            DisplayDialogue(successDialogue); // NEW: Use DisplayDialogue method
            Debug.Log("Max health upgraded to 150! Player material is now metallic.");
        }
        else
        {
            DisplayDialogue(failDialogue); // NEW: Use DisplayDialogue method
        }
    }
}