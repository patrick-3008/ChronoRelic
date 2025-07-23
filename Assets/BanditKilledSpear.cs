using UnityEngine;

public class BanditKilledSpear : MonoBehaviour
{
    private SpearThrowingEnemy enemyMainScript;
    private bool hasNotifiedDeath = false;

    void Start()
    {
        enemyMainScript = GetComponent<SpearThrowingEnemy>();
        if (enemyMainScript == null)
        {
            Debug.LogError("Enemy Spear script not found on this GameObject.");
        }
    }

    void Update()
    {
        if (enemyMainScript == null) return;

        // Check health only if not already notified
        if (!hasNotifiedDeath && enemyMainScript.health <= 0)
        {
            hasNotifiedDeath = true;

            // Notify mission system
            SideMissionBanditsTracker.Instance?.EnemyDefeated();

            Debug.Log($"{gameObject.name} died and notified mission tracker.");
        }
    }
}
