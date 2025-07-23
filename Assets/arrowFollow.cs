using UnityEngine;

public class arrowFollow : MonoBehaviour
{
    [Header("Player Reference")]
    public GameObject player;

    [Header("Arrow Settings")]
    public bool followRotation = true;
    public Vector3 rotationOffset = Vector3.zero;

    private RectTransform arrowTransform;

    void Start()
    {
        // Get the RectTransform component for UI manipulation
        arrowTransform = GetComponent<RectTransform>();

        // If no player is assigned, try to find it by tag
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        // Warn if player is still not found
        if (player == null)
        {
            Debug.LogWarning("Arrow Follow: No player GameObject assigned or found!");
        }
    }

    void Update()
    {
        // Only update if we have a valid player reference
        if (player != null && followRotation)
        {
            FollowPlayerRotation();
        }
    }

    void FollowPlayerRotation()
    {
        // Get the player's Y rotation (typically the one we want for minimap)
        float playerYRotation = player.transform.eulerAngles.y;

        // Apply the rotation to the arrow with any offset
        Vector3 newRotation = new Vector3(
            rotationOffset.x,
            rotationOffset.y,
            -playerYRotation + rotationOffset.z  // Negative because UI rotation is opposite
        );

        arrowTransform.rotation = Quaternion.Euler(newRotation);
    }

    // Optional: Method to set player reference at runtime
    public void SetPlayer(GameObject newPlayer)
    {
        player = newPlayer;
    }
}