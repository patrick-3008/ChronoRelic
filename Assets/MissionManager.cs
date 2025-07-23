using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("Mission States")]
    public bool mission1_complete = false;
    public bool mission2_complete = false;
    public bool mission3_complete = false;
    public bool mission4_complete = false;
    public bool mission5_complete = false;
    public bool mission6_complete = false;
    public bool missionside_complete = false;  // Side mission

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Only one MissionManager allowed
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes
    }

    public void CompleteMission(int missionNumber)
    {
        switch (missionNumber)
        {
            case 1:
                mission1_complete = true;
                Debug.Log("Mission 1 completed!");
                break;
            case 2:
                mission2_complete = true;
                Debug.Log("Mission 2 completed!");
                break;
            case 3:
                mission3_complete = true;
                Debug.Log("Mission 3 completed!");
                break;
            case 4:
                mission4_complete = true;
                Debug.Log("Mission 4 completed!");
                break;
            case 5:
                mission5_complete = true;
                Debug.Log("Mission 5 completed!");
                break;
            case 6:
                mission6_complete = true;
                Debug.Log("Mission 6 completed!");
                break;
            case 7:  // Let's assign side mission to 8 for clarity
                missionside_complete = true;
                Debug.Log("Side mission completed!");
                break;
            default:
                Debug.LogWarning("Mission number not recognized!");
                break;
        }
    }

    public bool IsMissionComplete(int missionNumber)
    {
        switch (missionNumber)
        {
            case 1: return mission1_complete;
            case 2: return mission2_complete;
            case 3: return mission3_complete;
            case 4: return mission4_complete;
            case 5: return mission5_complete;
            case 6: return mission6_complete;
            case 7: return missionside_complete;
            default: return false;
        }
    }
}
