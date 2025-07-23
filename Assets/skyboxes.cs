using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
public class DayNightCycle : MonoBehaviour
{
    public Material daySkybox;
    public Material nightSkybox;
    public Light sunlight;
    public Light moonlight;
    public float cycleDuration = 60f;
    public float fadeDuration = 5f;

    private Color dayAmbient = new(226f / 255f, 139f / 255f, 118f / 255f, 1);
    private Color nightAmbient = new(44f / 255f, 60f / 255f, 198f / 255f, 1);

    private Color dayFog = new(94f / 255f, 75f / 255f, 51f / 255f, 1);
    private Color nightFog = new(58f / 255f, 58f / 255f, 90f / 255f, 1);

    private bool transitioning = false;
    private bool isDay = true;
    private float timer = 0f;

    private bool isPlayerInZone = false;

    void Start()
    {
        RenderSettings.skybox = daySkybox;
        sunlight.intensity = 1f;
        moonlight.intensity = 0f;
        RenderSettings.skybox.SetFloat("_Exposure", 1f);

        // Ensure BoxCollider is trigger
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
            col.isTrigger = true;
    }

    void Update()
    {
        if (isPlayerInZone)
        {
            ForceNight();
            return; // Skip timer update
        }

        timer += Time.deltaTime;

        if (timer >= cycleDuration && !transitioning)
        {
            timer = 0f;
            StartCoroutine(TransitionSkybox());
        }
    }

    private void ForceNight()
    {
        if (!transitioning && isDay)
        {
            StopAllCoroutines(); // Stop ongoing transitions
            SetNightInstantly();
        }
    }

    private void SetNightInstantly()
    {
        isDay = false;
        RenderSettings.skybox = nightSkybox;
        RenderSettings.skybox.SetFloat("_Exposure", 0.5f);
        sunlight.intensity = 0f;
        moonlight.intensity = 0.5f;
        RenderSettings.ambientLight = nightAmbient;
        RenderSettings.fogColor = nightFog;
    }

    private IEnumerator TransitionSkybox()
    {
        transitioning = true;

        Material startSkybox = isDay ? daySkybox : nightSkybox;
        Material endSkybox = isDay ? nightSkybox : daySkybox;

        float fadeStartTime = Time.time;

        if (isDay)
        {
            // Day to night
            while (Time.time - fadeStartTime < fadeDuration)
            {
                float t = (Time.time - fadeStartTime) / fadeDuration;
                RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(1f, 0f, t));
                sunlight.intensity = Mathf.Lerp(1f, 0f, t);
                moonlight.intensity = Mathf.Lerp(0f, 0.5f, t);
                yield return null;
            }

            RenderSettings.skybox = endSkybox;
            fadeStartTime = Time.time;

            while (Time.time - fadeStartTime < fadeDuration)
            {
                float t = (Time.time - fadeStartTime) / fadeDuration;
                RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(0f, 0.5f, t));
                RenderSettings.ambientLight = nightAmbient;
                RenderSettings.fogColor = nightFog;
                yield return null;
            }
        }
        else
        {
            // Night to day
            while (Time.time - fadeStartTime < fadeDuration)
            {
                float t = (Time.time - fadeStartTime) / fadeDuration;
                RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(1f, 0f, t));
                sunlight.intensity = Mathf.Lerp(0f, 1f, t);
                moonlight.intensity = Mathf.Lerp(0.5f, 0f, t);
                yield return null;
            }

            RenderSettings.skybox = endSkybox;
            fadeStartTime = Time.time;

            while (Time.time - fadeStartTime < fadeDuration)
            {
                float t = (Time.time - fadeStartTime) / fadeDuration;
                RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(0f, 1f, t));
                RenderSettings.ambientLight = dayAmbient;
                RenderSettings.fogColor = dayFog;
                yield return null;
            }
        }

        isDay = !isDay;
        transitioning = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            Debug.Log("Player entered night zone.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            Debug.Log("Player exited night zone.");
            timer = 0f; // Reset cycle timer when leaving
        }
    }
}
