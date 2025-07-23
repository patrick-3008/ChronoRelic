using UnityEngine;

public class SideMissionBanditsTracker : MonoBehaviour
{
    public static SideMissionBanditsTracker Instance { get; private set; }

    private int banditsDefeated = 0;
    public int banditsToDefeat = 4;

    void Awake()
    {
        // Singleton pattern: ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Call this whenever a bandit dies
    public void EnemyDefeated()
    {
        banditsDefeated++;
        Debug.Log($"Bandits defeated: {banditsDefeated}/{banditsToDefeat}");

        if (banditsDefeated >= banditsToDefeat)
        {
            CompleteSideMission();
        }
    }

    private void CompleteSideMission()
    {
        Debug.Log("Side mission complete!");
        MissionManager.Instance.CompleteMission(7); // Side mission is case 7
    }

}
