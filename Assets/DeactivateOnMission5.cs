using UnityEngine;

public class DeactivateOnMission5 : MonoBehaviour
{
    void Start()
    {
        // Check immediately on start
        if (MissionManager.Instance != null && MissionManager.Instance.mission5_complete)
        {
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Check once after mission5 becomes complete
        if (MissionManager.Instance != null && MissionManager.Instance.mission5_complete && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            Debug.Log($"{gameObject.name} deactivated after Mission 5 completion.");
        }
    }
}
