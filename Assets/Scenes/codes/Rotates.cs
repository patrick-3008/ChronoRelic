using System.Collections;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    public Transform Player; // Assign the character's Transform in the Inspector.
    public Transform raycastOrigin; // Assign the specific point (e.g., a child object like "Head" or "Hand") in the Inspector.
    public float rotationSpeed = 10f; // Default rotation speed.
    private Animator animator;
    private Animator playerAnimator; // Player's Animator to check its state.

    // LayerMask to specify layers to ignore (excluding 'arrow' layer)
    public LayerMask layerToIgnore;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found!");
        }

        if (raycastOrigin == null)
        {
            Debug.LogError("Raycast origin Transform not assigned!");
        }

        if (Player != null)
        {
            playerAnimator = Player.GetComponent<Animator>();
            if (playerAnimator == null)
            {
                Debug.LogError("Player does not have an Animator component!");
            }
        }
    }

    void Update()
    {
        if (Player == null || raycastOrigin == null || animator == null || playerAnimator == null)
        {
            return;
        }

        // Check if the player is running
        bool isRunning = playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("run"); // Replace "Run" with the actual state name in your Animator.

        // Adjust rotation speed based on the player's state
        rotationSpeed = isRunning ? 20f : 10f;

        // Check if the animation is in the correct state to allow rotation
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (currentState.IsName("walk")) // Replace "walk" with the actual state name of your animation
        {
            // Direction to the character
            Vector3 directionToCharacter = Player.position - transform.position;
            directionToCharacter.y = 0; // Keep rotation on the horizontal plane.

            // Check if the direction is significant enough to rotate
            if (directionToCharacter.magnitude > 0.1f)
            {
                // Target rotation
                Quaternion targetRotation = Quaternion.LookRotation(directionToCharacter);

                // Smoothly rotate towards the target
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // Raycast from the specific point in the forward direction
        Ray ray = new Ray(raycastOrigin.position, raycastOrigin.forward);

        // Perform the raycast, ignoring the specified layer
        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, ~layerToIgnore))
        {
            if (hitInfo.collider.CompareTag("Player"))
            {
                // Draw the ray in green if it hits the Player
                Debug.DrawRay(raycastOrigin.position, raycastOrigin.forward * 20f, Color.green);
                Debug.Log("Raycast hit the Player!");
                animator.SetBool("is_walking", false);
            }
            else
            {
                // Draw the ray in red if it hits something else
                Debug.DrawRay(raycastOrigin.position, raycastOrigin.forward * hitInfo.distance, Color.red);
                Debug.Log("Raycast did not hit the Player!");
                animator.SetBool("is_walking", true);
            }
        }
        else
        {
            // Draw the ray in blue if it doesn't hit anything
            Debug.Log("Raycast did not hit a thing!");
            animator.SetBool("is_walking", true);
            Debug.DrawRay(raycastOrigin.position, raycastOrigin.forward * 20f, Color.blue);
        }
    }
}
