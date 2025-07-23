using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CloakChecker : MonoBehaviour
{
    public GameObject cloak; // Assign the cloak GameObject in the Inspector
    public GameObject player; // Assign the player GameObject in the Inspector
    public Transform teleportDestination; // Where to teleport the player
    public Image fadeImage; // UI Image used for fading (black screen)
    public float fadeDuration = 1f;
    public bool isTeleporting = false;
    private bool hasUsedTeleporter = false; // Prevent using this teleporter again

    void Update()
    {
        // No need for Update method anymore since we're using OnTriggerEnter
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player entered the trigger and hasn't used this teleporter before
        if (other.gameObject == player && !hasUsedTeleporter && !isTeleporting)
        {
            Debug.Log("Player entered teleport trigger zone");

            // Check if player has the cloak
            if (cloak != null && cloak.activeInHierarchy)
            {
                Debug.Log("Player has cloak - starting teleport");
                hasUsedTeleporter = true; // Mark as used to prevent future teleports

                if (teleportDestination != null)
                {
                    StartCoroutine(FadeAndTeleport());
                }
                else
                {
                    Debug.LogError("Teleport destination is null!");
                }
            }
            else
            {
                Debug.Log("Player doesn't have cloak - no teleport");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Optional: Keep this for debugging
        if (other.gameObject == player)
        {
            Debug.Log("Player exited teleport trigger zone");
        }
    }

    private IEnumerator FadeAndTeleport()
    {
        isTeleporting = true;

        // Fade to black
        yield return StartCoroutine(Fade(0f, 1f));

        // Teleport player
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        player.transform.position = teleportDestination.position;
        Debug.Log($"Player teleported to: {teleportDestination.position}");

        if (controller != null)
        {
            controller.enabled = true;
        }

        // Fade back from black
        yield return StartCoroutine(Fade(1f, 0f));

        isTeleporting = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            fadeImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(color.r, color.g, color.b, to);
    }
}