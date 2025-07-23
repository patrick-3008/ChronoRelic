using UnityEngine;
using System;

public class Mission5EnemyTracker : MonoBehaviour
{
    public static Mission5EnemyTracker Instance { get; private set; }

    public static event Action OnMission5Complete;

    private int enemiesDefeated = 0;
    private int enemiesRequired = 4;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    // Call this method each time an enemy relevant to Mission 5 is defeated
    public void EnemyDefeated()
    {
        enemiesDefeated++;
        Debug.Log($"[Tracker] Enemy defeated. Total: {enemiesDefeated}/{enemiesRequired}");

        if (enemiesDefeated >= enemiesRequired)
        {
            Debug.Log("[Tracker] Mission 5 completed. Triggering event.");
            OnMission5Complete?.Invoke();
        }
    }

}
