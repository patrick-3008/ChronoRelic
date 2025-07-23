using System.Collections.Generic;
using UnityEngine;

public class DistanceActivatorManager : MonoBehaviour
{
    [Header("Gate Reference")]
    public GameObject gate; // The gate object with CapitalGate script

    [Header("Objects To Manage (Manually Assign)")]
    public List<GameObject> objectsToActivate = new List<GameObject>();
    public List<GameObject> objectsToDeactivate = new List<GameObject>();

    [Header("Settings")]
    public float checkInterval = 0.5f;

    private CloakChecker capitalGateScript;
    private bool objectsActivated = false;

    private void Start()
    {
        // Get the CapitalGate script from the gate object
        if (gate != null)
        {
            capitalGateScript = gate.GetComponent<CloakChecker>();
            if (capitalGateScript == null)
            {
                Debug.LogError("CapitalGate script not found on the assigned gate object!");
            }
        }
        else
        {
            Debug.LogError("Gate object not assigned! Please assign the gate GameObject in the Inspector.");
        }

        // Start with activation objects disabled and deactivation objects enabled
        SetObjectsActive(objectsToActivate, false);
        SetObjectsActive(objectsToDeactivate, true);

        InvokeRepeating(nameof(CheckTeleportStatus), 0f, checkInterval);
    }

    private void CheckTeleportStatus()
    {
        if (capitalGateScript == null) return;

        // Check if isTeleporting is true and objects haven't been activated yet
        if (capitalGateScript.isTeleporting && !objectsActivated)
        {
            SetObjectsActive(objectsToActivate, true);
            SetObjectsActive(objectsToDeactivate, false);
            objectsActivated = true;
            Debug.Log("Objects updated due to teleporting status! Activated new objects and deactivated old ones.");
        }
    }

    private void SetObjectsActive(List<GameObject> objectList, bool active)
    {
        for (int i = 0; i < objectList.Count; i++)
        {
            GameObject obj = objectList[i];
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }
}