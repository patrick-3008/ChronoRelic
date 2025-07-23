using UnityEngine;
using TMPro;

public class NPCInteraction : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public string npcName = "Pharaoh"; // NEW: NPC name to display
    public string npcDialogue = "Hello, traveler! Welcome to our village.";
    public TextMeshProUGUI dialogueTextBox; // Drag the TextMeshPro object here
    public GameObject dialogueUI;           // A panel to show/hide the text box

    Animator anim;
    private bool isPlayerNearby = false;
    private Transform playerTransform;      // Reference to the player's transform
    private bool isInteracting = false;     // Tracks if the NPC is currently interacting
    private bool dialogueActive = false;    // NEW: Track if dialogue is currently active
    public float rotationSpeed = 5f;        // Speed of the NPC's rotation

    public AudioClip interactionSound;
    private AudioSource audioSource;

    void Start()
    {
        // Ensure dialogue UI is hidden at start
        dialogueUI.SetActive(false);
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // NEW: Configure text wrapping and enable word wrapping
        if (dialogueTextBox != null)
        {
            dialogueTextBox.enableWordWrapping = true;
            dialogueTextBox.overflowMode = TextOverflowModes.Overflow;
        }
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
            RotateTowardsPlayer(); // Rotate NPC to face the player while interacting
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

        // Display dialogue with NPC name
        DisplayDialogue(npcDialogue);

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

    // NEW: Display dialogue with NPC name in yellow
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

        // Calculate the direction to the player
        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0; // Keep the rotation only in the horizontal plane

        // Smoothly rotate towards the player
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Ensure the player has a tag "Player"
        {
            isPlayerNearby = true;
            playerTransform = other.transform; // Store the player's transform
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            playerTransform = null; // Clear the player's transform reference

            // NEW: End dialogue when player leaves
            if (dialogueActive)
            {
                EndDialogue();
            }
        }
    }
}