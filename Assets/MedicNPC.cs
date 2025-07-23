using UnityEngine;
using TMPro;

public class MedicNPC : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public string npcName = "Medic"; // NEW: NPC name to display
    public string emptyDialogue = " ";
    public string injuredDialogue = "Oh, someone is injured? Near the trees by the river you say? I will send my apprentice to heal him immediately!";
    public string healedDialogue = "Here you go, I took care of your wounds.";

    public TextMeshProUGUI dialogueTextBox;
    public GameObject dialogueUI;
    public GameObject npcModel; // drag your child model here in Inspector
    public ParticleSystem healingEffect; // ✅ Add this

    private Animator anim;
    private bool isPlayerNearby = false;
    private Transform playerTransform;
    private bool isInteracting = false;
    private bool dialogueActive = false; // NEW: Track if dialogue is currently active
    private PlayerCombatAndHealth playerCombat;
    public float rotationSpeed = 5f;

    public GameObject potionReward;
    private bool hasSpawned = false;
    private bool hasTriggeredMission2 = false;

    public AudioClip interactionSound;
    private AudioSource audioSource;

    void Start()
    {
        dialogueUI.SetActive(false);
        npcModel.SetActive(false); // model starts hidden
        audioSource = GetComponent<AudioSource>();

        // NEW: Enable text wrapping if dialogueTextBox exists
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
                // End dialogue
                EndDialogue();
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
        anim?.SetTrigger("Talking");

        string dialogue = GetCurrentDialogue();
        DisplayDialogue(dialogue);

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

    // NEW: Get the appropriate dialogue based on mission state
    private string GetCurrentDialogue()
    {
        if (MissionManager.Instance.mission3_complete)
        {
            // Heal the player
            if (playerCombat != null)
            {
                playerCombat.health = playerCombat.maxHealth;
                Debug.Log("Player fully healed!");

                // ✅ Play healing effect at player location
                if (healingEffect != null)
                {
                    healingEffect.transform.position = playerCombat.transform.position;
                    healingEffect.Play();
                }
            }

            return healedDialogue;
        }
        else if (MissionManager.Instance.mission1_complete)
        {
            // Trigger mission 2
            if (!hasTriggeredMission2)
            {
                MissionManager.Instance.mission2_complete = true;
                hasTriggeredMission2 = true;
                Debug.Log("Mission 2 Completed!");

                // ✅ Activate the potion reward
                if (potionReward != null)
                {
                    potionReward.SetActive(true);
                    Debug.Log("Potion reward activated.");
                }
            }

            return injuredDialogue;
        }
        else
        {
            return emptyDialogue;
        }
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
            if (MissionManager.Instance.mission3_complete)
            {
                if (playerCombat != null)
                {
                    playerCombat.health = playerCombat.maxHealth;
                    Debug.Log("Player fully healed!");

                    // ✅ Play healing effect at player location
                    if (healingEffect != null)
                    {
                        healingEffect.transform.position = playerCombat.transform.position;
                        healingEffect.Play();
                    }
                }

                dialogueTextBox.text = healedDialogue;
            }
            else if (MissionManager.Instance.mission1_complete)
            {
                dialogueTextBox.text = injuredDialogue;

                if (!hasTriggeredMission2)
                {
                    MissionManager.Instance.mission2_complete = true;
                    hasTriggeredMission2 = true;
                    Debug.Log("Mission 2 Completed!");

                    // ✅ Activate the potion reward
                    if (potionReward != null)
                    {
                        potionReward.SetActive(true);
                        Debug.Log("Potion reward activated.");
                    }
                }
            }
            else
            {
                dialogueTextBox.text = emptyDialogue;
            }

            if (interactionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(interactionSound);
            }

            isInteracting = true;
        }
        else
        {
            dialogueTextBox.text = emptyDialogue;
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