using UnityEngine;

public class BanditKilled : MonoBehaviour
{
    private EnemyFollowAndEvadeWithAnimation enemyMainScript;
    private bool hasNotifiedDeath = false;

    void Start()
    {
        enemyMainScript = GetComponent<EnemyFollowAndEvadeWithAnimation>();
        if (enemyMainScript == null)
        {
            Debug.LogError("EnemyFollowAndEvadeWithAnimation script not found on this GameObject.");
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
