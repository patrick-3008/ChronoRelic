using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class endGame : MonoBehaviour
{
    [SerializeField]
    private float _sceneFadeDuration;
    private SceneFade _sceneFade;
    private bool _hasStarted = false;
    public AudioSource Music;

    [Header("White Fade Settings")]
    public GameObject whiteFadeImage; // NEW: White image UI element to fade out
    public float whiteFadeDuration = 2.0f; // NEW: Duration for white fade out

    [Header("Space Key Effects")]
    public AudioClip spaceKeySound; // Audio to play when space is pressed
    public TextMeshProUGUI textToFlash; // Text UI element to flash
    public float textFlashDuration = 0.5f; // How long the text flash takes

    [Header("Text Fade Settings")]
    public float textFadeDuration = 2.0f; // How long each fade in/out cycle takes

    private Color originalTextColor; // Store original text color
    private float originalTextSize;
    private Coroutine textFadeCoroutine; // Track the fading coroutine

    private void Awake()
    {
        _sceneFade = GetComponentInChildren<SceneFade>();
        // Lock the cursor to the center of the screen and make it invisible
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Store the original text color
        if (textToFlash != null)
        {
            originalTextColor = textToFlash.color;
            originalTextSize = textToFlash.fontSize;
        }
    }

    private void Start()
    {
        // NEW: Start with white fade out when scene loads
        StartCoroutine(InitialWhiteFadeOut());

        // Start the text fading loop
        if (textToFlash != null)
        {
            textFadeCoroutine = StartCoroutine(TextFadeLoop());
        }
    }

    // NEW: White fade out coroutine that runs when scene starts
    private IEnumerator InitialWhiteFadeOut()
    {
        if (whiteFadeImage != null)
        {
            whiteFadeImage.SetActive(true);
            yield return StartCoroutine(PerformImageFade(whiteFadeImage, 1f, 0f, whiteFadeDuration));
            whiteFadeImage.SetActive(false);
        }
    }

    // NEW: Generic fade method for UI images
    private IEnumerator PerformImageFade(GameObject imageObject, float startAlpha, float endAlpha, float duration)
    {
        UnityEngine.UI.Image image = imageObject.GetComponent<UnityEngine.UI.Image>();
        if (image == null) yield break;

        Color startColor = image.color;
        Color endColor = startColor;
        startColor.a = startAlpha;
        endColor.a = endAlpha;

        image.color = startColor;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            image.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        image.color = endColor;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !_hasStarted)
        {
            _hasStarted = true;

            // Stop the fading loop
            if (textFadeCoroutine != null)
            {
                StopCoroutine(textFadeCoroutine);
            }

            // Play the space key sound
            if (spaceKeySound != null && Music != null)
            {
                Music.PlayOneShot(spaceKeySound);
            }

            // Flash the text color
            if (textToFlash != null)
            {
                StartCoroutine(FlashTextColor());
            }

            // NEW: Exit the game instead of loading scene 1
            ExitGame();
        }
    }

    // NEW: Method to exit the game
    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // Continuous text fade in/out loop
    private IEnumerator TextFadeLoop()
    {
        while (!_hasStarted) // Keep fading until space is pressed
        {
            // Fade out
            yield return StartCoroutine(FadeText(originalTextColor, new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f)));

            // Fade in
            yield return StartCoroutine(FadeText(new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f), originalTextColor));
        }
    }

    // Helper method to fade text between two colors
    private IEnumerator FadeText(Color fromColor, Color toColor)
    {
        float elapsedTime = 0f;

        while (elapsedTime < textFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / textFadeDuration;
            textToFlash.color = Color.Lerp(fromColor, toColor, t);
            yield return null;
        }

        textToFlash.color = toColor;
    }

    // Coroutine to flash text color to white and back
    private IEnumerator FlashTextColor()
    {
        if (textToFlash == null) yield break;

        float elapsedTime = 0f;
        float halfDuration = textFlashDuration / 2f;

        // Flash to white (first half)
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfDuration;
            textToFlash.color = Color.Lerp(originalTextColor, Color.white, t);
            textToFlash.fontSize = Mathf.Lerp(originalTextSize, originalTextSize + 2, t / 2);
            textToFlash.fontSize = Mathf.Lerp(originalTextSize + 2, originalTextSize, t / 2);
            yield return null;
        }

        // Flash back to original color (second half)
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfDuration;
            textToFlash.color = Color.Lerp(Color.white, originalTextColor, t);
            yield return null;
        }

        // Ensure it's back to original color
        textToFlash.color = originalTextColor;
    }

    public void LoadScene(int sceneNumber)
    {
        StartCoroutine(LoadSceneCoroutine(sceneNumber));
    }

    private IEnumerator LoadSceneCoroutine(int sceneNumber)
    {
        float initialVolume = Music.volume;
        float elapsedTime = 0f;
        // Start the fade out coroutine
        Coroutine fadeCoroutine = StartCoroutine(_sceneFade.FadeOutCoroutine(_sceneFadeDuration));
        // Decrease music volume over the same duration
        while (elapsedTime < _sceneFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            Music.volume = Mathf.Lerp(initialVolume, 0f, elapsedTime / _sceneFadeDuration);
            yield return null;
        }
        // Ensure volume is exactly 0
        Music.volume = 0f;
        // Wait for fade to complete
        yield return fadeCoroutine;
        yield return SceneManager.LoadSceneAsync(sceneNumber);
    }
}