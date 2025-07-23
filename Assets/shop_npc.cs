using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopNPC : MonoBehaviour
{
    public string npcDialogue = "Welcome, traveler! I have weapons for sale: a khopesh and a spear.";
    public TextMeshProUGUI dialogueTextBox; // Drag the TextMeshPro object here
    public GameObject dialogueUI;           // A panel to show/hide the text box
    Animator anim;

    private bool isPlayerNearby = false;
    private Transform playerTransform;      // Reference to the player's transform
    private bool isInteracting = false;     // Tracks if the NPC is currently interacting

    public float rotationSpeed = 5f;        // Speed of the NPC's rotation

    public int khopeshPrice = 100;
    public int spearPrice = 150;

    private WeaponInventory playerInventory;
    private PlayerMovement playerMovement;
    private PlayerCombatAndHealth playerCombat; // Reference to combat script for knives/potions
    public GameObject player;      // Reference to your SubZeroMove script
    private PlayerGold playerGold;

    public Camera mainCam;
    public Camera shopCam;
    public GameObject gameUI;
    public GameObject shopUI;

    public Button exitButton;
    public Button khopeshButton;
    public Button spearButton;
    public Button knivesButton;   // Button for purchasing knives
    public Button potionButton;   // Button for purchasing potions

    // NEW: Variables to prevent multiple clicks
    private bool isPurchasing = false;
    private float purchaseCooldown = 0.5f; // Half second cooldown between purchases
    private float lastPurchaseTime = 0f;

    // NEW: Cursor state management
    private bool isInShopMode = false;

    void Start()
    {
        // Ensure dialogue UI is hidden at start
        dialogueUI.SetActive(false);
        anim = GetComponent<Animator>();
        GreetPlayer();

        mainCam.enabled = true;
        shopCam.enabled = false;
        gameUI.SetActive(true);
        shopUI.SetActive(false);
        playerGold = player.GetComponent<PlayerGold>();
        playerCombat = player.GetComponent<PlayerCombatAndHealth>(); // Get combat script reference

        // Ensure cursor starts locked for gameplay
        SetCursorForGameplay();
    }

    void Update()
    {
        // If player is near and presses E while the dialogue is hidden, open the shop
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E) && !dialogueUI.activeSelf)
        {
            ToggleDialogueAndRotation();
            anim.SetTrigger("Talking");

            // Switch to shop camera/UI
            mainCam.enabled = false;
            shopCam.enabled = true;
            gameUI.SetActive(false);
            shopUI.SetActive(true);

            // Freeze the player's movement (but keep the script enabled for gold access)
            if (playerMovement != null)
                playerMovement.canMove = false;

            // NEW: Set cursor for shop mode
            SetCursorForShop();

            // Add listener to the exit button
            exitButton.onClick.AddListener(exitMenu);
        }

        if (isInteracting)
        {
            RotateTowardsPlayer();

            // Button listeners for purchasing - only add if not already added
            if (!khopeshButton.onClick.GetPersistentEventCount().Equals(1))
            {
                khopeshButton.onClick.AddListener(khopeshBuy);
                spearButton.onClick.AddListener(spearBuy);
                knivesButton.onClick.AddListener(knivesBuy);   // Knives button listener
                potionButton.onClick.AddListener(potionsBuy); // Potion button listener
            }
        }
    }

    // NEW: Method to set cursor for shop mode
    private void SetCursorForShop()
    {
        isInShopMode = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // NEW: Method to set cursor for gameplay mode
    private void SetCursorForGameplay()
    {
        isInShopMode = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // NEW: Method to ensure cursor stays visible during purchases
    private void EnsureCursorVisibleForPurchase()
    {
        if (isInShopMode)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void ToggleDialogueAndRotation()
    {
        bool isActive = dialogueUI.activeSelf;
        dialogueUI.SetActive(!isActive);

        if (!isActive)
        {
            // Start interacting
            dialogueTextBox.text = npcDialogue;
            isInteracting = true;
        }
        else
        {
            // Stop interacting
            isInteracting = false;
        }
    }

    private void RotateTowardsPlayer()
    {
        if (playerTransform == null) return;

        // Calculate direction to player
        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0; // Keep the rotation only in the horizontal plane

        // Smoothly rotate towards the player
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Make sure your Player is tagged "Player"
        {
            isPlayerNearby = true;
            playerTransform = other.transform;

            // Grab the SubZeroMove script
            playerMovement = other.GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogError("SubZeroMove script not found on Player.");
            }

            // Grab the WeaponInventory if needed
            playerInventory = other.GetComponent<WeaponInventory>();
            if (playerInventory == null)
            {
                Debug.LogError("WeaponInventory script not found on Player.");
            }

            // Grab the PlayerCombatAndHealth script
            playerCombat = other.GetComponent<PlayerCombatAndHealth>();
            if (playerCombat == null)
            {
                Debug.LogError("PlayerCombatAndHealth script not found on Player.");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            playerTransform = null;
            playerMovement = null;
            playerInventory = null;
            playerCombat = null;
            isInteracting = false;

            // Hide dialogue if player steps away
            dialogueUI.SetActive(false);
        }
    }

    void GreetPlayer()
    {
        Debug.Log("Welcome, traveler! I have weapons for sale.");
    }

    // NEW: Method to check if purchase is allowed
    private bool CanPurchase()
    {
        if (isPurchasing)
        {
            return false;
        }

        if (Time.time - lastPurchaseTime < purchaseCooldown)
        {
            return false;
        }

        return true;
    }

    // NEW: Method to start purchase process
    private void StartPurchase()
    {
        isPurchasing = true;
        lastPurchaseTime = Time.time;
        // NEW: Ensure cursor stays visible during purchase
        EnsureCursorVisibleForPurchase();
    }

    // NEW: Method to end purchase process
    private void EndPurchase()
    {
        isPurchasing = false;
        // NEW: Ensure cursor stays visible after purchase completes
        EnsureCursorVisibleForPurchase();
    }

    public void SellWeapon(string weaponName)
    {
        if (playerMovement == null)
        {
            Debug.LogError("SubZeroMove script not found. Cannot access player's gold.");
            return;
        }

        if (playerInventory == null)
        {
            Debug.LogError("WeaponInventory script not found. Cannot sell weapon.");
            return;
        }

        // Find the weapon in the player's inventory list
        WeaponInventory.Weapon weaponToPurchase =
            playerInventory.weaponList.Find(w => w.name == weaponName);

        if (weaponToPurchase != null)
        {
            AttemptPurchase(weaponToPurchase);
        }
        else
        {
            Debug.LogError($"Weapon '{weaponName}' not found in the inventory list.");
        }
    }

    void AttemptPurchase(WeaponInventory.Weapon weapon)
    {
        // NEW: Check if purchase is allowed for knives and potions
        if ((weapon.name == "Throwing Knives" || weapon.name == "Potion") && !CanPurchase())
        {
            return; // Ignore the click if too soon or already purchasing
        }

        // 1) Has the player already purchased it?
        if (weapon.isPurchased)
        {
            if (weapon.name == "Throwing Knives")
            {
                StartPurchase(); // NEW: Start purchase process

                if (playerGold.gold < 50)
                {
                    dialogueTextBox.text =
                    $"Not enough gold! {weapon.name} costs 50 coins.";
                    EndPurchase(); // NEW: End purchase process
                    return;
                }
                else
                {
                    playerGold.RemoveGold(50);
                    // Add 10 knives to player's knife count
                    if (playerCombat != null)
                    {
                        playerCombat.knifeCount += 10;
                        playerCombat.UpdateKnifeCountText();
                    }
                    dialogueTextBox.text = "You purchased 10 throwing knives for 50 coins!";
                    EndPurchase(); // NEW: End purchase process
                    return;
                }
            }
            else if (weapon.name == "Potion")
            {
                StartPurchase(); // NEW: Start purchase process

                if (playerGold.gold < 40)
                {
                    dialogueTextBox.text =
                    $"Not enough gold! {weapon.name} costs 40 coins.";
                    EndPurchase(); // NEW: End purchase process
                    return;
                }
                else
                {
                    playerGold.RemoveGold(40);
                    // Add 1 potion to player's potion count
                    if (playerCombat != null)
                    {
                        playerCombat.potionCount += 1;
                        playerCombat.UpdatePotionCountText();
                    }
                    dialogueTextBox.text = "You purchased 1 healing potion for 40 coins!";
                    EndPurchase(); // NEW: End purchase process
                    return;
                }
            }
            else
            {
                return;
            }
        }

        // NEW: Start purchase process for one-time purchases too
        StartPurchase();

        if (weapon.name == "Khopesh")
        {
            if (playerGold.gold < 100)
            {
                dialogueTextBox.text =
                $"Not enough gold! {weapon.name} costs 100 coins.";
                EndPurchase(); // NEW: End purchase process
                return;
            }
            else
            {
                playerGold.RemoveGold(100);
            }
        }

        if (weapon.name == "Spear")
        {
            if (playerGold.gold < 150)
            {
                dialogueTextBox.text =
                $"Not enough gold! {weapon.name} costs 150 coins.";
                EndPurchase(); // NEW: End purchase process
                return;
            }
            else
            {
                playerGold.RemoveGold(150);
            }
        }

        weapon.isPurchased = true;

        dialogueTextBox.text =
            $"You purchased {weapon.name}!";
        Debug.Log($"{weapon.name} is now purchased.");

        // Update the inventory UI if you have it
        playerInventory.AssignIconsToUI();

        EndPurchase(); // NEW: End purchase process
    }

    void exitMenu()
    {
        // Switch back to main gameplay camera/UI
        mainCam.enabled = true;
        shopCam.enabled = false;
        gameUI.SetActive(true);
        shopUI.SetActive(false);

        // Re-allow movement
        if (playerMovement != null)
            playerMovement.canMove = true;

        // NEW: Set cursor back to gameplay mode
        SetCursorForGameplay();

        // Reset references/states
        isPlayerNearby = false;
        playerTransform = null;
        dialogueUI.SetActive(false);
        isInteracting = false;

        // NEW: Reset purchase state when exiting
        isPurchasing = false;
    }

    void khopeshBuy()
    {
        SellWeapon("Khopesh");
    }

    void spearBuy()
    {
        SellWeapon("Spear");
    }

    void knivesBuy()
    {
        // NEW: Check if purchase is allowed
        if (!CanPurchase())
        {
            return;
        }
        SellWeapon("Throwing Knives");
    }

    void potionsBuy()
    {
        // NEW: Check if purchase is allowed
        if (!CanPurchase())
        {
            return;
        }
        SellWeapon("Potion");
    }
}