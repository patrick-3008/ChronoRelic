using UnityEngine;
using TMPro;

public class SwordTeacherNPC : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public string npcName = "Blacksmith"; // NEW: NPC name to display
    public string npcDialogue = "Warriors must sharpen their skills! I can enhance your Khopesh or Spear for 50 gold each.";
    public string successKhopeshDialogue = "Your Khopesh is sharper and stronger!";
    public string successSpearDialogue = "Your Spear is now more deadly!";
    public string failDialogue = "Not enough gold! Each upgrade costs 50 gold.";
    public string noWeaponDialogue = "You don't have this weapon!";
    public string alreadyUpgradedDialogue = "You've already mastered this weapon's power.";

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
    private WeaponInventory playerInventory;

    private const int upgradeCost = 50;
    private int khopeshDamageIncrease = 20;
    private int spearDamageIncrease = 30;

    public Material khopeshGoldMaterial;
    public Material spearGoldMaterial;

    public GameObject khopesh;
    public GameObject spear;

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
                if (Input.GetKeyDown(KeyCode.Alpha7))
                {
                    UpgradeWeapon("Khopesh");
                }
                else if (Input.GetKeyDown(KeyCode.Alpha8))
                {
                    UpgradeWeapon("Spear");
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
            formattedText += "\n\n<color=yellow>Press 7 to upgrade Khopesh. Press 8 to upgrade Spear.</color>";
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
            playerInventory = other.GetComponent<WeaponInventory>();

            if (playerGold == null)
            {
                Debug.LogError("PlayerGold script not found on the player object.");
            }

            if (playerCombat == null)
            {
                Debug.LogError("PlayerCombatAndHealth script not found on the player object.");
            }

            if (playerInventory == null)
            {
                Debug.LogError("WeaponInventory script not found on the player object.");
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
            playerInventory = null;

            // NEW: End dialogue when player leaves
            if (dialogueActive)
            {
                EndDialogue();
            }
        }
    }

    void GreetPlayer()
    {
        Debug.Log("Warriors must sharpen their skills! I can enhance your Khopesh or Spear for 50 gold each.");
    }

    private void UpgradeWeapon(string weaponName)
    {
        if (playerGold == null || playerCombat == null || playerInventory == null) return;

        // Check if the player owns the weapon
        WeaponInventory.Weapon ownedWeapon = playerInventory.weaponList.Find(w => w.name == weaponName && w.isPurchased);

        if (ownedWeapon == null)
        {
            DisplayDialogue(noWeaponDialogue); // NEW: Use DisplayDialogue method
            return;
        }

        // Check if the weapon has already been upgraded
        if ((weaponName == "Khopesh" && playerCombat.khopeshUpgrade) || (weaponName == "Spear" && playerCombat.spearUpgrade))
        {
            DisplayDialogue(alreadyUpgradedDialogue); // NEW: Use DisplayDialogue method
            return;
        }

        // Check if the player has enough gold
        if (playerGold.gold >= upgradeCost)
        {
            playerGold.gold -= upgradeCost;

            if (weaponName == "Khopesh")
            {
                playerCombat.khopeshDamage += khopeshDamageIncrease;
                playerCombat.khopeshUpgrade = true; // Mark upgrade in PlayerCombatAndHealth
                DisplayDialogue(successKhopeshDialogue); // NEW: Use DisplayDialogue method
                ChangeWeaponMaterial(khopesh, khopeshGoldMaterial);
                Debug.Log("Khopesh upgraded and material changed!");
            }
            else if (weaponName == "Spear")
            {
                playerCombat.spearDamage += spearDamageIncrease;
                playerCombat.spearUpgrade = true; // Mark upgrade in PlayerCombatAndHealth
                DisplayDialogue(successSpearDialogue); // NEW: Use DisplayDialogue method
                ChangeWeaponMaterial(spear, spearGoldMaterial);
                Debug.Log("Spear upgraded and material changed!");
            }
        }
        else
        {
            DisplayDialogue(failDialogue); // NEW: Use DisplayDialogue method
        }
    }

    private void ChangeWeaponMaterial(GameObject weaponPrefab, Material newMaterial)
    {
        if (weaponPrefab == null)
        {
            Debug.LogError("Weapon prefab is null, cannot change material.");
            return;
        }

        MeshRenderer meshRenderer = weaponPrefab.GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            meshRenderer.material = newMaterial;
            Debug.Log("Weapon material changed to gold!");
        }
        else
        {
            Debug.LogError("No MeshRenderer found on the weapon prefab.");
        }
    }
}