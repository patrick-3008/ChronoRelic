using UnityEngine;

public class EnemyUIFollower : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject uiElement; // UI element to display above the NPC
    public float heightOffset = 2f; // How high above the NPC to position the UI
    public float displayRange = 10f; // Distance at which UI becomes visible
    public float uiScale = 0.01f; // Scale for world space UI (adjust if too big/small)

    [Header("Player Detection")]
    public string playerTag = "Player";

    [Header("Smoothing")]
    public bool smoothMovement = true;
    public float followSpeed = 5f; // Speed for smooth following
    public float rotationSpeed = 5f; // Speed for smooth rotation

    private Transform playerTransform;
    private Camera playerCamera;
    private bool isPlayerInRange = false;

    void Start()
    {
        // Find the player
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
        }
        else
        {
            Debug.LogWarning("Player not found! Make sure player has the correct tag: " + playerTag);
        }

        // Setup UI element for world space
        if (uiElement != null)
        {
            // Set initial position above the enemy
            uiElement.transform.position = transform.position + Vector3.up * heightOffset;

            // Check if it's a Canvas and set it to World Space
            Canvas canvas = uiElement.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = playerCamera;

                // Set initial scale for world space canvas
                uiElement.transform.localScale = Vector3.one * uiScale;
            }

            // Disable physics if there's a Rigidbody
            Rigidbody rb = uiElement.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Start with UI hidden
            uiElement.SetActive(false);
        }
        else
        {
            Debug.LogError("UI Element not assigned! Please assign a UI element in the Inspector.");
        }
    }

    void Update()
    {
        if (playerTransform == null || uiElement == null) return;

        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Check if player is in range
        bool shouldShowUI = distanceToPlayer <= displayRange;

        // Show/hide UI based on range
        if (shouldShowUI != isPlayerInRange)
        {
            isPlayerInRange = shouldShowUI;
            uiElement.SetActive(isPlayerInRange);
        }

        // If UI is active, update its position and rotation
        if (isPlayerInRange)
        {
            UpdateUIPosition();
            UpdateUIRotation();
        }
    }

    void UpdateUIPosition()
    {
        // Calculate target position above the NPC
        Vector3 targetPosition = transform.position + Vector3.up * heightOffset;

        if (smoothMovement)
        {
            // Smooth movement
            uiElement.transform.position = Vector3.Lerp(
                uiElement.transform.position,
                targetPosition,
                followSpeed * Time.deltaTime
            );
        }
        else
        {
            // Instant movement
            uiElement.transform.position = targetPosition;
        }
    }

    void UpdateUIRotation()
    {
        if (playerCamera == null) return;

        // Calculate direction to face the camera
        Vector3 directionToCamera = playerCamera.transform.position - uiElement.transform.position;
        directionToCamera.y = 0; // Keep UI upright by removing Y component

        if (directionToCamera != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);

            if (smoothMovement)
            {
                // Smooth rotation
                uiElement.transform.rotation = Quaternion.Slerp(
                    uiElement.transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
            else
            {
                // Instant rotation
                uiElement.transform.rotation = targetRotation;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw the display range in the Scene view for easy visualization
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, displayRange);

        // Draw the UI position
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.up * heightOffset, Vector3.one * 0.5f);
    }

    // Optional: Method to manually set a specific player target
    public void SetPlayer(Transform newPlayerTransform)
    {
        playerTransform = newPlayerTransform;
        if (newPlayerTransform != null)
        {
            // Try to get camera from player or use main camera
            Camera playerCam = newPlayerTransform.GetComponentInChildren<Camera>();
            if (playerCam != null)
            {
                playerCamera = playerCam;
            }
        }
    }

    // Optional: Method to force show/hide UI
    public void ForceShowUI(bool show)
    {
        if (uiElement != null)
        {
            uiElement.SetActive(show);
            isPlayerInRange = show;
        }
    }
}