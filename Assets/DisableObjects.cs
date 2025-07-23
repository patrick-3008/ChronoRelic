using UnityEngine;

public class DisableObjects : MonoBehaviour
{
    public Transform player;           // Reference to the player
    public float activationDistance = 50f;

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(player.position, transform.position);

        // Activate/deactivate based on distance
        bool shouldBeActive = distance < activationDistance;
        if (gameObject.activeSelf != shouldBeActive)
        {
            gameObject.SetActive(shouldBeActive);
        }
    }
}
