using UnityEngine;

public class Mission5CompletionListener : MonoBehaviour
{
    void OnEnable()
    {
        Mission5EnemyTracker.OnMission5Complete += HandleMission5Complete;
    }

    void OnDisable()
    {
        Mission5EnemyTracker.OnMission5Complete -= HandleMission5Complete;
    }

    private void HandleMission5Complete()
    {
        if (!MissionManager.Instance.IsMissionComplete(5))
        {
            MissionManager.Instance.CompleteMission(5);
            Debug.Log("✅ Mission 5 marked complete by Mission5CompletionListener.");
        }
    }
}
