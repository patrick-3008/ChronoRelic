using UnityEngine;

public class SpearProjectile : MonoBehaviour
{
    private int damage;
    private PlayerCombatAndHealth playerScript;

    public void Initialize(int damageAmount, PlayerCombatAndHealth player)
    {
        damage = damageAmount;
        playerScript = player;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerScript != null)
            {
                playerScript.health -= damage;
                Debug.Log("💥 Player hit by spear! -" + damage + " HP");
            }
            Destroy(gameObject);
        }
        else if (!other.CompareTag("Enemy")) // Prevents spears from hitting other enemies
        {
            Destroy(gameObject); // Destroy if it hits environment
        }
    }
}
