using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponInventory : MonoBehaviour
{
    [System.Serializable]
    public class Weapon
    {
        public string name;       // Weapon name
        public int damage;        // Weapon damage
        public int price;         // Weapon price
        public Sprite icon;       // Weapon icon (for UI)
        public Sprite highlightBorder; // Highlight border sprite
        public GameObject prefab; // Weapon prefab
        public bool isPurchased;  // Flag to track if the weapon is purchased
    }

    public List<Weapon> weaponList = new List<Weapon>(); // Start empty
    public Transform inventoryPanel; // Parent object for all weapon panels
    public Sprite defaultBorderSprite; // Default border sprite for non-selected items
    public Weapon currentWeapon;
    private Animator anim;
    public Sprite defaultEmptySprite;

    private Color highlight = new Color(99.0f / 255.0f, 175.0f / 255.0f, 1.0f, 1.0f);

    // Default Weapons
    public Weapon fistsWeapon; // Fists (assigned in Inspector)
    public Weapon throwingKnivesWeapon; // Throwing Knives (assigned in Inspector)
    public Weapon potion; // Throwing Knives (assigned in Inspector)

    void Start()
    {
        AssignIconsToUI();
        anim = GetComponent<Animator>();

        EquipWeapon(0);
        //{
          //  AddWeapon(fistsWeapon);
           // Debug.Log("Fists added to the inventory.");
        //}
        //currentWeapon = fistsWeapon;

        // Ensure "Throwing Knives" weapon is in the inventory
        if (!weaponList.Exists(w => w.name == "Throwing Knives"))
        {
            AddWeapon(throwingKnivesWeapon);
            Debug.Log("Throwing Knives added to the inventory.");
        }

        if (!weaponList.Exists(w => w.name == "Potion"))
        {
            AddWeapon(potion);
            Debug.Log("Potion added to the inventory.");
        }


        // Equip fists by default
        EquipWeapon(0);
    }

    void Update()
    {
        // Check for key inputs to equip weapons
        if (Input.GetKeyDown(KeyCode.Q)) EquipWeapon(0);  // Fists
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipWeapon(1); // First weapon (e.g., sword)
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipWeapon(2); // Second weapon (e.g., spear)
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipWeapon(3); // Throwing Knives
    }

    public void AddWeapon(Weapon newWeapon)
    {
        newWeapon.isPurchased = true; // Mark the weapon as purchased
        weaponList.Add(newWeapon);
        AssignIconsToUI();
        Debug.Log($"Added {newWeapon.name} to inventory.");
    }

    public void AssignIconsToUI()
    {
        foreach (Weapon weapon in weaponList)
        {
            Transform weaponFullPanel = inventoryPanel.Find(weapon.name + "Full");
            if (weaponFullPanel != null)
            {
                Transform border = weaponFullPanel.Find("Border");
                if (border != null)
                {
                    Transform imageElement = border.Find(weapon.name);
                    if (imageElement != null)
                    {
                        Image iconImage = imageElement.GetComponent<Image>();
                        if (iconImage != null)
                        {
                            if (weapon.isPurchased && weapon.icon != null)
                            {
                                iconImage.sprite = weapon.icon;
                                Debug.Log($"Assigned icon for {weapon.name}");
                            }
                            else
                            {
                                iconImage.sprite = defaultEmptySprite; // Keep panel blank for unpurchased items
                                Debug.Log($"Set blank icon for {weapon.name} (not purchased)");
                            }
                        }
                    }
                }
            }
        }
    }

    public void EquipWeapon(int index)
    {
        if (index >= 0 && index < weaponList.Count)
        {
            Weapon weaponToEquip = weaponList[index];

            if (!weaponToEquip.isPurchased)
            {
                Debug.LogError($"You have not purchased {weaponToEquip.name}.");
                return;
            }

            // Deactivate the currently equipped weapon (if any)
            if (currentWeapon != null && currentWeapon.prefab != null)
            {
                currentWeapon.prefab.SetActive(false);
            }

            // Equip the new weapon
            currentWeapon = weaponToEquip;

            if (currentWeapon.prefab != null)
            {
                currentWeapon.prefab.SetActive(true);


                // Play the draw animation only if conditions are met
                if (!anim.GetBool("Walking") && anim.GetBool("IsGrounded"))
                {
                    anim.SetTrigger("drawWeapon");
                }
            }

            // Ensure the UI reflects the selected weapon correctly
            AssignIconsToUI();
            HighlightSelectedWeapon(index);

            Debug.Log($"Equipped: {currentWeapon.name}");
        }
        else
        {
            Debug.LogError("Invalid weapon index.");
        }
    }

    void HighlightSelectedWeapon(int selectedIndex)
    {
        for (int i = 0; i < weaponList.Count; i++)
        {
            Transform weaponFullPanel = inventoryPanel.Find(weaponList[i].name + "Full");
            if (weaponFullPanel != null)
            {
                Transform border = weaponFullPanel.Find("Border");
                if (border != null)
                {
                    Image borderImage = border.GetComponent<Image>();
                    if (borderImage != null)
                    {
                        borderImage.sprite = (i == selectedIndex && weaponList[i].highlightBorder != null)
                            ? weaponList[i].highlightBorder
                            : defaultBorderSprite;
                    }
                }

                Transform imageElement = border?.Find(weaponList[i].name);
                if (imageElement != null)
                {
                    Image iconImage = imageElement.GetComponent<Image>();
                    if (iconImage != null)
                    {
                        iconImage.color = (i == selectedIndex) ? highlight : Color.white;
                    }
                }
            }
        }
    }

    // Get currently equipped weapon
    public Weapon GetCurrentWeapon()
    {
        return currentWeapon;
    }
}
