using UnityEngine;

public class gateCross : MonoBehaviour
{
    public Transform teleportDestination; // Assign the empty GameObject's transform in the Inspector

    void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object is the player
        if (other.CompareTag("Player"))
        {
            // Teleport the player to the destination
            if (teleportDestination != null)
            {
                other.transform.position = teleportDestination.position;
                Debug.Log("Player teleported to: " + teleportDestination.position);
            }
            else
            {
                Debug.LogWarning("Teleport destination not assigned!");
            }
        }
    }
}