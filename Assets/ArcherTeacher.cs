using UnityEngine;
using TMPro;

public class ArcherTeacherNPC : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public string npcName = "Archer"; // NEW: NPC name to display
    public string npcDialogue = "A skilled archer can hit targets from afar. Want to improve your range for 50 gold?";
    public string successDialogue = "Your range has been increased to 20! Use it wisely.";
    public string failDialogue = "Not enough gold! Training costs 50 gold.";
    public string alreadyUpgradedDialogue = "You've already mastered this technique.";

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

    private const int trainingCost = 50;
    private const float newKnifeRange = 20f;
    private bool hasUpgradedRange = false; // Prevents multiple upgrades

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
                    UpgradeThrowingRange();
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
            formattedText += "\n\n<color=yellow>Press 6 to upgrade your throwing range.</color>";
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

            if (playerGold == null)
            {
                Debug.LogError("PlayerGold script not found on the player object.");
            }

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
        Debug.Log("A skilled archer can hit targets from afar. Want to improve your range?");
    }

    private void UpgradeThrowingRange()
    {
        if (playerGold == null || playerCombat == null) return;

        if (hasUpgradedRange)
        {
            DisplayDialogue(alreadyUpgradedDialogue); // NEW: Use DisplayDialogue method
            return;
        }

        if (playerGold.gold >= trainingCost)
        {
            playerGold.gold -= trainingCost;
            playerCombat.knifeRange = newKnifeRange;
            hasUpgradedRange = true; // Mark upgrade as done

            DisplayDialogue(successDialogue); // NEW: Use DisplayDialogue method
            Debug.Log("Throwing knife range upgraded to 20!");
        }
        else
        {
            DisplayDialogue(failDialogue); // NEW: Use DisplayDialogue method
        }
    }
}