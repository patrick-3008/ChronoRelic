using System.Collections;
using UnityEngine;

public class ArrowShooter : MonoBehaviour
{
    public Transform raycastOrigin; // Assign the raycast origin point in the Inspector.
    public Transform Bow; // Assign the Bow transform in the Inspector.
    public float rayLength = 20f;   // Length of the raycast.
    public string playerTag = "Player"; // Tag for the player.
    public GameObject arrowPrefab; // The arrow prefab to instantiate.
    public Transform arrowSpawnPoint; // Point where the arrow is spawned.
    public float arrowSpeed = 10f; // Speed of the arrow.

    private Animator animator; // Reference to the Animator component.
    private bool isAiming = false;
    private Transform playerTransform; // Reference to the player's transform.

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on this GameObject!");
        }
    }

    void Update()
    {
        // Perform raycast
        Ray ray = new Ray(raycastOrigin.position, raycastOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, rayLength))
        {
            // Check if the ray hit the player
            if (hitInfo.collider.CompareTag(playerTag))
            {
                // Cache the player's transform
                if (playerTransform == null)
                {
                    playerTransform = hitInfo.collider.transform;
                }

                // Activate aiming
                if (!isAiming)
                {
                    isAiming = true;
                    animator.SetBool("is_aiming", true); // Activate aiming animation
                }

                // Draw ray in green when hitting the player
                Debug.DrawRay(raycastOrigin.position, raycastOrigin.forward * hitInfo.distance, Color.green);
            }
            else
            {
                // Ray hit something else
                StopAiming();
                Debug.DrawRay(raycastOrigin.position, raycastOrigin.forward * hitInfo.distance, Color.red);
            }
        }
        else
        {
            // Ray didn't hit anything
            StopAiming();
            Debug.DrawRay(raycastOrigin.position, raycastOrigin.forward * rayLength, Color.blue);
        }
    }

    public void TriggerShoot()
    {
        if (isAiming && playerTransform != null)
        {
            Shoot(playerTransform.position);
        }
        else
        {
            Debug.LogWarning("Cannot shoot: Not aiming or no target.");
        }
    }

    private void Shoot(Vector3 targetPosition)
    {
        // Instantiate the arrow prefab
        Vector3 arrowPos = arrowSpawnPoint.position + new Vector3(0, 1.5f, 0);

        Vector3 directionToTarget = (targetPosition - arrowPos).normalized;

        GameObject arrow = Instantiate(arrowPrefab, arrowPos, Quaternion.LookRotation(directionToTarget));
        arrow.transform.eulerAngles = new Vector3(90, arrow.transform.eulerAngles.y, arrow.transform.eulerAngles.z);

        // Add a Rigidbody and Collider to the arrow for collision detection
        Rigidbody rb = arrow.AddComponent<Rigidbody>();
        rb.useGravity = false; // Disable gravity to keep the arrow flying straight
        Collider arrowCollider = arrow.AddComponent<BoxCollider>();

        ArrowCollision arrowCollision = arrow.AddComponent<ArrowCollision>();
        arrowCollision.damage = 20; // Set damage value
        arrowCollision.playerTag = playerTag;

        // Set the velocity of the arrow's Rigidbody
        rb.linearVelocity = directionToTarget * arrowSpeed;

        // Destroy the arrow after 5 seconds to prevent clutter
        Destroy(arrow, 5f);
    }

    private void StopAiming()
    {
        if (isAiming)
        {
            isAiming = false;
            animator.SetBool("is_aiming", false); // Deactivate aiming animation
        }

        // Clear cached player transform
        playerTransform = null;
    }
}
