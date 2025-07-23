using UnityEngine;
using System.Collections;

public class PharaohMusic : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip ambientMusic;
    public AudioClip bossMusic;
    public AudioClip ankhReleaseMusic;

    public GameObject terrain;


    public GameObject pharaoh;
    public GameObject ankh; // ✅ Reference to the Ankh object
    public float fadeDuration = 1f;
    public string playerTag = "Player";

    private Coroutine fadeCoroutine;
    private bool isPlayerInside = false;
    private bool isBossMusicPlaying = false;
    private bool hasAnkhMusicPlayed = false;

    private void Update()
    {
        if (!isPlayerInside || pharaoh == null || ankh == null) return;

        PharaohBoss bossScript = pharaoh.GetComponent<PharaohBoss>();
        if (bossScript == null) return;

        bool isBossActive = bossScript.enabled;
        bool isAnkhHeldByPharaoh = ankh.transform.parent != null && pharaoh.tag == "Enemy";
        bool isAnkhHeldByPlayer = ankh.transform.parent != null && ankh.transform.parent.CompareTag("Player");
        bool isAnkhReleased = ankh.transform.parent == null;

        // 🎵 Ambient music if Ankh is held by the player
        if (isAnkhHeldByPlayer)
        {
            if (audioSource.clip != ambientMusic)
            {
                SwitchMusic(ambientMusic);
                isBossMusicPlaying = false;
            }
            return;
        }

        // 🎵 Play Ankh release music once
        if (isAnkhReleased && !hasAnkhMusicPlayed)
        {
            if (audioSource.clip != ankhReleaseMusic)
            {
                SwitchMusic(ankhReleaseMusic);
                hasAnkhMusicPlayed = true;
                isBossMusicPlaying = false;
            }
            return;
        }

        // 🎵 Boss music if Pharaoh is alive and holding Ankh
        if (isBossActive && isAnkhHeldByPharaoh && !isBossMusicPlaying)
        {
            SwitchMusic(bossMusic);
            isBossMusicPlaying = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        isPlayerInside = true;

        if (terrain != null)
            terrain.SetActive(false); // 👈 Deactivate terrain


        PharaohBoss bossScript = pharaoh.GetComponent<PharaohBoss>();
        if (bossScript == null || ankh == null) return;

        bool isBossActive = bossScript.enabled;
        bool isAnkhHeldByPharaoh = ankh.transform.parent != null && pharaoh.tag == "Enemy";
        bool isAnkhHeldByPlayer = ankh.transform.parent != null && ankh.transform.parent.CompareTag("Player");
        bool isAnkhReleased = ankh.transform.parent == null;

        if (isAnkhHeldByPlayer)
        {
            SwitchMusic(ambientMusic);
            isBossMusicPlaying = false;
        }
        else if (isAnkhReleased && !hasAnkhMusicPlayed)
        {
            SwitchMusic(ankhReleaseMusic);
            hasAnkhMusicPlayed = true;
            isBossMusicPlaying = false;
        }
        else if (isBossActive && isAnkhHeldByPharaoh)
        {
            SwitchMusic(bossMusic);
            isBossMusicPlaying = true;
        }
        else
        {
            SwitchMusic(ambientMusic);
            isBossMusicPlaying = false;
        }
    }




    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInside = false;

            if (terrain != null)
                terrain.SetActive(true); // 👈 Reactivate terrain

            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeAudio(0f));
        }

    }


    private void SwitchMusic(AudioClip newClip)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeToClip(newClip, 1f));
    }

    private IEnumerator FadeToClip(AudioClip newClip, float targetVolume)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.clip = newClip;

        if (newClip != null)
            audioSource.Play();

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;
        fadeCoroutine = null;
    }

    private IEnumerator FadeAudio(float targetVolume)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / fadeDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;

        if (targetVolume == 0f)
            audioSource.Stop();

        fadeCoroutine = null;
    }


}
