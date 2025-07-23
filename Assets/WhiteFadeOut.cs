using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SimpleWhiteFade : MonoBehaviour
{
    [SerializeField]
    public Image whiteFadeImage; // Drag your white UI Image here

    [SerializeField]
    private float fadeDuration = 2f; // How long the fade takes

    private void Start()
    {
        // Start the white fade out when scene begins
        StartCoroutine(FadeOutWhite());
    }

    private IEnumerator FadeOutWhite()
    {
        if (whiteFadeImage == null)
        {
            Debug.LogError("White fade image is not assigned!");
            yield break;
        }

        // Start with full white (alpha = 1)
        Color startColor = whiteFadeImage.color;
        startColor.a = 1f;
        whiteFadeImage.color = startColor;

        float elapsedTime = 0f;

        // Fade from white (alpha 1) to transparent (alpha 0)
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);

            Color newColor = whiteFadeImage.color;
            newColor.a = alpha;
            whiteFadeImage.color = newColor;

            yield return null;
        }

        // Ensure it's completely transparent at the end
        Color finalColor = whiteFadeImage.color;
        finalColor.a = 0f;
        whiteFadeImage.color = finalColor;

        // Optionally disable the image when fade is complete
        whiteFadeImage.gameObject.SetActive(false);
    }
}