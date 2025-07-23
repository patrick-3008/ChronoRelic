using UnityEngine;

public class CactusDamage : MonoBehaviour
{
    public int damageAmount = 20;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerCombatAndHealth playerCombat = other.GetComponent<PlayerCombatAndHealth>();

            if (playerCombat != null)
            {
                playerCombat.health -= damageAmount;

                // Optional: Clamp health to prevent going below zero
                playerCombat.health = Mathf.Max(playerCombat.health, 0);
            }
        }
    }
}
