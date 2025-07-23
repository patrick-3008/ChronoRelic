using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class SceneController : MonoBehaviour
{
    [SerializeField]
    private float _sceneFadeDuration;
    private SceneFade _sceneFade;
    private bool _hasStarted = false;
    public AudioSource Music;

    [Header("Space Key Effects")]
    public AudioClip spaceKeySound; // NEW: Audio to play when space is pressed
    public TextMeshProUGUI textToFlash; // NEW: Text UI element to flash
    public float textFlashDuration = 0.5f; // NEW: How long the text flash takes

    [Header("Text Fade Settings")]
    public float textFadeDuration = 2.0f; // NEW: How long each fade in/out cycle takes

    private Color originalTextColor; // NEW: Store original text color
    private float originalTextSize;
    private Coroutine textFadeCoroutine; // NEW: Track the fading coroutine

    private void Awake()
    {
        _sceneFade = GetComponentInChildren<SceneFade>();
        // Lock the cursor to the center of the screen and make it invisible
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // NEW: Store the original text color
        if (textToFlash != null)
        {
            originalTextColor = textToFlash.color;
            originalTextSize = textToFlash.fontSize;
        }
    }

    private void Start()
    {
        // NEW: Start the text fading loop
        if (textToFlash != null)
        {
            textFadeCoroutine = StartCoroutine(TextFadeLoop());
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !_hasStarted)
        {
            _hasStarted = true;

            // NEW: Stop the fading loop
            if (textFadeCoroutine != null)
            {
                StopCoroutine(textFadeCoroutine);
            }

            // NEW: Play the space key sound
            if (spaceKeySound != null && Music != null)
            {
                Music.PlayOneShot(spaceKeySound);
            }
            // NEW: Flash the text color
            if (textToFlash != null)
            {
                StartCoroutine(FlashTextColor());
            }
            LoadScene(1);
        }
    }

    // NEW: Continuous text fade in/out loop
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

    // NEW: Helper method to fade text between two colors
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

    // NEW: Coroutine to flash text color to white and back
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