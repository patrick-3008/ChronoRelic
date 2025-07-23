using UnityEngine;
using System.Collections;

public class MainAreaAudio : MonoBehaviour
{
    public AudioSource audioSource; // Reference to the AudioSource component
    public string playerTag = "Player"; // Tag used to identify the player
    public float fadeDuration = 1f; // Duration of the fade-in and fade-out

    private Coroutine fadeCoroutine; // To ensure only one fade coroutine runs at a time

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeAudio(1f)); // Fade in to full volume
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeAudio(0f)); // Fade out to silence
        }
    }

    private IEnumerator FadeAudio(float targetVolume)
    {
        float startVolume = audioSource.volume;

        if (targetVolume > 0 && !audioSource.isPlaying)
        {
            audioSource.Play();
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / fadeDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;

        if (targetVolume == 0)
        {
            audioSource.Stop();
        }

        fadeCoroutine = null;
    }
}
