using UnityEngine;

public class BandagePickup : MonoBehaviour
{
    public float rotationSpeed = 50f; // Speed of rotation
    public int healAmount = 50;       // How much health to give

    void Update()
    {
        // Rotate the bandage around its Y-axis
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerCombatAndHealth playerCombat = other.GetComponent<PlayerCombatAndHealth>();

            if (playerCombat != null && playerCombat.health < playerCombat.maxHealth)
            {
                playerCombat.health += healAmount;

                // Clamp health if needed (optional, assuming max health is 100)
                playerCombat.health = Mathf.Min(playerCombat.health, playerCombat.maxHealth);

                // Destroy the bandage object
                Destroy(gameObject);
            }
        }
    }
}
