using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PriestNPC : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public string npcName = "Priest"; // NEW: NPC name to display

    [Header("UI & References")]
    public TextMeshProUGUI dialogueTextBox;
    public GameObject dialogueUI;
    public TextMeshProUGUI nextIndicator; // Optional ">>" UI
    public GameObject npcModel;
    public GameObject playerCloak;

    private bool isPlayerNearby = false;
    private bool isInteracting = false;
    private bool dialogueActive = false; // NEW: Track if dialogue is currently active
    private bool hasSpawned = false;
    private bool hasGrantedCloak = false;

    private int currentDialogueIndex = 0;
    private List<string> currentDialogues = new List<string>();

    private Animator anim;
    private Transform playerTransform;
    public float rotationSpeed = 5f;

    private List<string> originalDialogues = new List<string>()
    {
        "You... you made it here?",
        "That is no small feat. You must have journeyed a long, long way.",
        "But before I can trust your purpose is noble, you must prove yourself.",
        "There is a tomb hidden deep within the Great Desert—once belonging to Pharaoh Setekhotep.",
        "Bandits now control the tomb. If you truly seek the Pharaoh's Ankh...",
        "...you must drive them out.",
        "Do this, and I shall give you the tools and the path to the relic you seek."
    };

    private List<string> mission5Dialogues = new List<string>()
    {
        "You have done well to rid the desert of those bandits.",
        "Now, I shall guide your path to the Pharaoh's Ankh..",
        "The Ankh resides deep within the Pharaoh's Temple, hidden in the heart of the capital city.",
        "But beware — the city guards have been alerted to your intentions and patrol every corner with heightened vigilance.",
        "To claim the Ankh, you must move carefully and stay unseen. Stealth will be your greatest ally.",
        "Avoid drawing attention, use shadows, and trust no one.",
        "Take this holy cloak, it will grant you access through the main gate.",
        "Go now, and may the gods guide your steps."
    };

    void Start()
    {
        dialogueUI.SetActive(false);
        if (nextIndicator != null)
            nextIndicator.gameObject.SetActive(false);

        npcModel.SetActive(false);

        // NEW: Configure text wrapping
        if (dialogueTextBox != null)
        {
            dialogueTextBox.enableWordWrapping = true;
            dialogueTextBox.overflowMode = TextOverflowModes.Overflow;
        }
    }

    void Update()
    {
        if (!hasSpawned && MissionManager.Instance.mission3_complete)
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
                // Advance dialogue
                AdvanceDialogue();
            }
        }

        if (isInteracting)
        {
            RotateTowardsPlayer();
        }
    }

    void SpawnNPC()
    {
        npcModel.SetActive(true);
        anim = npcModel.GetComponent<Animator>();
        hasSpawned = true;
    }

    private void StartDialogue()
    {
        dialogueUI.SetActive(true);
        dialogueActive = true; // NEW: Set dialogue active
        isInteracting = true;
        currentDialogueIndex = 0;

        currentDialogues.Clear();

        if (MissionManager.Instance.mission5_complete)
        {
            currentDialogues.AddRange(mission5Dialogues);
        }
        else if (MissionManager.Instance.mission3_complete)
        {
            currentDialogues.AddRange(originalDialogues);
        }

        anim?.SetTrigger("Talking");
        DisplayCurrentSentence();
    }

    private void AdvanceDialogue()
    {
        currentDialogueIndex++;

        if (currentDialogueIndex >= currentDialogues.Count)
        {
            EndDialogue();
        }
        else
        {
            DisplayCurrentSentence();
        }
    }

    // NEW: Enhanced display method with NPC name
    private void DisplayCurrentSentence()
    {
        string sentence = currentDialogues[currentDialogueIndex];

        // Format the text with yellow NPC name
        string formattedSentence = $"<color=yellow>{npcName}:</color> <color=white>{sentence}</color>";

        // Add ">>" to all but the last sentence
        if (currentDialogueIndex < currentDialogues.Count - 1)
        {
            formattedSentence += "  <color=yellow>>></color>";
        }

        dialogueTextBox.text = formattedSentence;
    }

    // NEW: Enhanced end dialogue method
    private void EndDialogue()
    {
        dialogueUI.SetActive(false);
        dialogueActive = false; // NEW: Set dialogue inactive
        isInteracting = false;
        dialogueTextBox.text = "";

        if (nextIndicator != null)
            nextIndicator.gameObject.SetActive(false);

        if (MissionManager.Instance.mission5_complete && !MissionManager.Instance.mission6_complete)
        {
            MissionManager.Instance.CompleteMission(6);
            Debug.Log("Mission 6 completed!");

            if (playerCloak != null && !hasGrantedCloak)
            {
                playerCloak.SetActive(true);
                hasGrantedCloak = true;
                Debug.Log("Cloak activated!");
            }
        }
    }

    private void RotateTowardsPlayer()
    {
        if (playerTransform == null) return;

        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            playerTransform = other.transform;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            playerTransform = null;

            // NEW: End dialogue when player leaves
            if (dialogueActive)
            {
                EndDialogue();
            }
        }
    }
}