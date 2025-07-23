using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class introSceneManager : MonoBehaviour
{
    [SerializeField]
    private float _sceneFadeDuration;
    private SceneFadeBlack _sceneFade;
    private SceneFadeBlack _sceneFadeWhite; // Reference to the white fade object
    private bool _hasStarted = false;
    public AudioSource Music;

    [Header("White Fade Audio")]
    public AudioClip whiteFadeAudio; // NEW: Audio to play before white fade

    [Header("Light Flickering")]
    public Light flickeringLight; // NEW: Light to flicker randomly
    public float minFlickerInterval = 0.1f; // NEW: Minimum time between flickers
    public float maxFlickerInterval = 2.0f; // NEW: Maximum time between flickers
    public float flickerDuration = 0.1f; // NEW: How long each flicker lasts

    private float originalLightIntensity; // NEW: Store original light intensity
    private bool isFlickering = false; // NEW: Track if currently flickering

    private void Awake()
    {
        _sceneFade = GetComponentInChildren<SceneFadeBlack>();
        // Find the "Screen Fade White" GameObject
        GameObject whiteFadeObject = GameObject.Find("Screen Fade White");
        if (whiteFadeObject != null)
        {
            _sceneFadeWhite = whiteFadeObject.GetComponent<SceneFadeBlack>();
        }
        // Lock the cursor to the center of the screen and make it invisible
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // NEW: Store original light intensity and start flickering
        if (flickeringLight != null)
        {
            originalLightIntensity = flickeringLight.intensity;
            StartCoroutine(LightFlickerLoop());
        }
    }

    private void Start()
    {
        // Fade in from black when scene starts
        StartCoroutine(FadeInFromBlack());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !_hasStarted)
        {
            _hasStarted = true;
            LoadScene(1);
        }
    }

    // NEW: Continuous light flickering loop
    private IEnumerator LightFlickerLoop()
    {
        while (true) // Keep flickering throughout the scene
        {
            // Wait for a random interval between flickers
            float waitTime = Random.Range(minFlickerInterval, maxFlickerInterval);
            yield return new WaitForSeconds(waitTime);

            // Perform the flicker if not already flickering
            if (!isFlickering && flickeringLight != null)
            {
                StartCoroutine(FlickerLight());
            }
        }
    }

    // NEW: Individual flicker effect
    private IEnumerator FlickerLight()
    {
        isFlickering = true;

        // Turn light off/dim
        flickeringLight.intensity = 0f;

        // Wait for flicker duration
        yield return new WaitForSeconds(flickerDuration);

        // Turn light back to original intensity
        flickeringLight.intensity = originalLightIntensity;

        isFlickering = false;
    }

    private IEnumerator FadeInFromBlack()
    {
        // Start with black screen and fade to clear
        yield return StartCoroutine(_sceneFade.FadeInCoroutine(_sceneFadeDuration));
    }

    public void FadeToWhite()
    {
        StartCoroutine(FadeToWhiteCoroutine());
    }

    private IEnumerator FadeToWhiteCoroutine()
    {
        // NEW: Play the white fade audio at the same time as the fade starts
        if (whiteFadeAudio != null && Music != null)
        {
            Music.PlayOneShot(whiteFadeAudio);
        }

        float initialVolume = Music.volume;
        float elapsedTime = 0f;
        // Start the fade to white coroutine using the white fade object
        Coroutine fadeCoroutine = null;
        if (_sceneFadeWhite != null)
        {
            fadeCoroutine = StartCoroutine(_sceneFadeWhite.FadeToWhiteCoroutine(_sceneFadeDuration));
        }
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
        if (fadeCoroutine != null)
        {
            yield return fadeCoroutine;
        }
        // Load scene 2
        yield return SceneManager.LoadSceneAsync(2);
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