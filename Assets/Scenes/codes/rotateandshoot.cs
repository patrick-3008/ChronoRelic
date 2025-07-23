using System.Collections;
using UnityEngine;

public class RotateAndShoot : MonoBehaviour
{
    public Transform character; // The player's Transform.
    public Transform raycastOrigin; // Origin for the raycast (e.g., hand or head).
    public Transform arrowSpawnPoint; // Point where the arrow is spawned.
    public GameObject arrowPrefab; // The arrow prefab.
    public float rayLength = 20f; // Length of the raycast.
    public float rotationSpeed = 5f; // Rotation speed.
    public float shootInterval = 0.5f; // Interval between shots.
    public float arrowSpeed = 15f; // Speed of the arrow.
    public LayerMask raycastLayerMask; // Layer mask to specify which layers the raycast should detect

    private Animator animator; // Reference to the Animator component.
    private bool isAiming = false;
    private bool canShoot = true;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found!");
        }

        if (raycastOrigin == null || arrowSpawnPoint == null)
        {
            Debug.LogError("Raycast origin or arrow spawn point not assigned!");
        }
    }

    void Update()
    {
        if (character == null || raycastOrigin == null)
        {
            return;
        }

        // Direction to the character
        Vector3 directionToCharacter = character.position - transform.position;
        directionToCharacter.y = 0; // Keep rotation on the horizontal plane.

        if (directionToCharacter.magnitude > 0.1f)
        {
            // Rotate towards the player
            Quaternion targetRotation = Quaternion.LookRotation(directionToCharacter);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Perform raycast with LayerMask to ignore arrows
            Ray ray = new Ray(raycastOrigin.position, raycastOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, rayLength, ~raycastLayerMask)) // ~raycastLayerMask excludes the arrow layer
            {
                if (hitInfo.collider.CompareTag("Player"))
                {
                    // Player detected
                    Debug.DrawRay(raycastOrigin.position, raycastOrigin.forward * hitInfo.distance, Color.green);

                    if (!isAiming)
                    {
                        isAiming = true;
                        animator.SetBool("is_aiming", true); // Start aiming animation
                    }

                    // Shoot if allowed
                    if (canShoot)
                    {
                        StartCoroutine(Shoot(hitInfo.point)); // Pass target position to shoot
                    }
                }
                else
                {
                    // Target lost
                    StopAiming();
                }
            }
            else
            {
                // No hit
                StopAiming();
            }
        }
        else
        {
            // No significant movement, stop aiming
            StopAiming();
        }
    }

    IEnumerator Shoot(Vector3 targetPosition)
    {
        canShoot = false;

        // Instantiate the arrow
        GameObject arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, Quaternion.identity);
        Rigidbody arrowRb = arrow.GetComponent<Rigidbody>();

        if (arrowRb != null)
        {
            // Calculate direction to the target
            Vector3 direction = (targetPosition - arrowSpawnPoint.position).normalized;

            // Apply velocity to the arrow
            arrowRb.linearVelocity = direction * arrowSpeed;

            // Orient the arrow to face its direction of travel
            arrow.transform.rotation = Quaternion.LookRotation(direction);

            // Debug visualization
            Debug.DrawRay(arrowSpawnPoint.position, direction * 5f, Color.red, 2f); // Visualize the direction
        }
        else
        {
            Debug.LogWarning("Arrow prefab missing Rigidbody component!");
        }

        Debug.Log("Arrow launched!");

        yield return new WaitForSeconds(shootInterval);
        canShoot = true;
    }

    void StopAiming()
    {
        if (isAiming)
        {
            isAiming = false;
            animator.SetBool("is_aiming", false); // Stop aiming animation
        }
    }
}
