using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TeleportWithFade : MonoBehaviour
{
    public Transform teleportDestination;       // Where to teleport the player
    public Image fadeImage;                     // UI Image used for fading (black screen)
    public float fadeDuration = 1f;             // Duration of fade in/out
    public string playerTag = "Player";

    private bool isTeleporting = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !isTeleporting)
        {
            StartCoroutine(FadeAndTeleport(other.gameObject));
        }
    }

    private IEnumerator FadeAndTeleport(GameObject player)
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
