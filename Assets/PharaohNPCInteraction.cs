using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class PharaohNPCInteraction : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public string npcName = "Pharaoh";
    public string[] dialogueLines;
    public TextMeshProUGUI dialogueTextBox;
    public GameObject dialogueUI;
    public GameObject fightCollider;

    [Header("Victory Dialogue Settings")]
    public TextMeshProUGUI victoryDialogueTextBox; // Separate dialogue panel for victory
    public GameObject victoryDialogueUI;

    [Header("Defeat Sequence")]
    public GameObject blackFadeImage;
    public GameObject whiteFadeImage;
    public AudioClip portalAudio;
    public float fadeDuration = 2f;
    public GameObject ankhObject; // Public Ankh GameObject with AnkhPickup script

    private bool isPlayerNearby = false;
    private bool isInteracting = false;
    private bool dialogueActive = false;
    private bool isDefeated = false;
    private bool defeatDialogueStarted = false;
    private int currentLine = 0;
    private Transform playerTransform;

    // Defeat dialogue arrays
    private string[] defeatDialogueWithName = {
        "No....",
        "This cannot be happening....",
        "How could YOU defeat me...."
    };

    private string[] victoryDialogue = {
        "Finally...",
        "I did it...",
        "I can save my future..."
    };

    private bool isDefeatDialogue = false;
    private bool isVictoryDialogue = false;

    public AudioClip interactionSound;
    private AudioSource audioSource;
    private Animator animator;
    private PharaohBoss pharaohBoss;
    private AnkhPickup ankhPickupScript; // Reference to AnkhPickup script on the ankh object

    void Start()
    {
        dialogueUI.SetActive(false);
        if (victoryDialogueUI != null)
            victoryDialogueUI.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        pharaohBoss = GetComponent<PharaohBoss>();

        // Get AnkhPickup script from the assigned ankh object
        if (ankhObject != null)
        {
            ankhPickupScript = ankhObject.GetComponent<AnkhPickup>();
            if (ankhPickupScript == null)
            {
                Debug.LogError("AnkhPickup script not found on the assigned Ankh object!");
            }
        }
        else
        {
            Debug.LogError("Ankh object not assigned! Please assign the Ankh GameObject in the Inspector.");
        }

        if (dialogueTextBox != null)
        {
            dialogueTextBox.enableWordWrapping = true;
            dialogueTextBox.overflowMode = TextOverflowModes.Overflow;
        }

        if (victoryDialogueTextBox != null)
        {
            victoryDialogueTextBox.enableWordWrapping = true;
            victoryDialogueTextBox.overflowMode = TextOverflowModes.Overflow;
        }

        if (blackFadeImage != null)
            blackFadeImage.SetActive(false);
        if (whiteFadeImage != null)
            whiteFadeImage.SetActive(false);

        if (fightCollider != null)
            fightCollider.SetActive(false);
    }

    void Update()
    {
        // Check if pharaoh is defeated AND ankh is picked up
        if (pharaohBoss != null && pharaohBoss.health <= 0 && !isDefeated && !defeatDialogueStarted &&
            ankhPickupScript != null && ankhPickupScript.isPickedUp)
        {
            isDefeated = true;
            StartCoroutine(HandleDefeatSequence());
        }

        // Handle input
        if (Input.GetKeyDown(KeyCode.E))
        {
            // For defeat/victory dialogue, allow progression regardless of proximity
            if (dialogueActive && (isDefeatDialogue || isVictoryDialogue))
            {
                ProgressDefeatDialogue();
            }
            // For initial dialogue, require proximity and pharaoh to be alive
            else if (isPlayerNearby && !dialogueActive && !isDefeated && pharaohBoss != null && pharaohBoss.health > 0)
            {
                BeginInitialDialogue();
            }
            else if (isPlayerNearby && dialogueActive && !isDefeatDialogue && !isVictoryDialogue)
            {
                ProgressInitialDialogue();
            }
        }

        if (isInteracting)
        {
            RotateTowardsPlayer();
        }
    }

    // Handle defeat sequence
    private IEnumerator HandleDefeatSequence()
    {
        defeatDialogueStarted = true;
        yield return new WaitForSeconds(2f);
        InitiateDefeatDialogue();
    }

    // Start initial dialogue
    private void BeginInitialDialogue()
    {
        dialogueActive = true;
        isInteracting = true;
        currentLine = 0;
        dialogueUI.SetActive(true);

        ShowCurrentDialogueLine();

        animator.SetTrigger("Talking");

        if (interactionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(interactionSound);
        }
    }

    // Show current dialogue line
    private void ShowCurrentDialogueLine()
    {
        if (currentLine < dialogueLines.Length)
        {
            string dialogue = dialogueLines[currentLine];
            string formattedText = $"<color=yellow>{npcName}:</color> <color=white>{dialogue}</color>";

            dialogueTextBox.text = formattedText;
        }
    }

    // Progress through initial dialogue
    private void ProgressInitialDialogue()
    {
        currentLine++;

        if (currentLine < dialogueLines.Length)
        {
            ShowCurrentDialogueLine();
        }
        else
        {
            EndDialogueAndStartFight();
        }
    }

    // End dialogue and start fight
    private void EndDialogueAndStartFight()
    {
        dialogueUI.SetActive(false);
        dialogueActive = false;
        isInteracting = false;

        PharaohBoss bossScript = GetComponent<PharaohBoss>();
        if (bossScript != null)
            bossScript.enabled = true;

        if (fightCollider != null)
            fightCollider.SetActive(true);

        animator.ResetTrigger("Talking");
    }

    // Start defeat dialogue
    private void InitiateDefeatDialogue()
    {
        dialogueActive = true;
        isInteracting = true;
        isDefeatDialogue = true;
        currentLine = 0;
        dialogueUI.SetActive(true);

        ShowDefeatDialogueLine();
    }

    // Show defeat dialogue line
    private void ShowDefeatDialogueLine()
    {
        if (isDefeatDialogue && currentLine < defeatDialogueWithName.Length)
        {
            string dialogue = defeatDialogueWithName[currentLine];
            string formattedText = $"<color=yellow>{npcName}:</color> <color=white>{dialogue}</color>";

            dialogueTextBox.text = formattedText;
        }
        else if (isVictoryDialogue && currentLine < victoryDialogue.Length)
        {
            string dialogue = victoryDialogue[currentLine];
            string formattedText = $"<color=white>{dialogue}</color>";

            // Use the separate victory dialogue panel
            if (victoryDialogueTextBox != null)
                victoryDialogueTextBox.text = formattedText;
        }
    }

    // Progress defeat dialogue
    private void ProgressDefeatDialogue()
    {
        currentLine++;

        if (isDefeatDialogue && currentLine < defeatDialogueWithName.Length)
        {
            ShowDefeatDialogueLine();
        }
        else if (isDefeatDialogue && currentLine >= defeatDialogueWithName.Length)
        {
            // End defeat dialogue and start black fade
            StartCoroutine(ExecuteBlackFadeAndVictoryDialogue());
        }
        else if (isVictoryDialogue && currentLine < victoryDialogue.Length)
        {
            ShowDefeatDialogueLine();
        }
        else if (isVictoryDialogue && currentLine >= victoryDialogue.Length)
        {
            // End victory dialogue and start white fade then scene transition
            StartCoroutine(ExecuteWhiteFadeAndSceneTransition());
        }
    }

    // Black fade sequence followed by victory dialogue
    private IEnumerator ExecuteBlackFadeAndVictoryDialogue()
    {
        dialogueUI.SetActive(false);
        dialogueActive = false;
        isInteracting = false;

        // Start black fade and keep it permanent
        if (blackFadeImage != null)
        {
            blackFadeImage.SetActive(true);
            yield return StartCoroutine(PerformImageFade(blackFadeImage, 0f, 1f, fadeDuration));
        }

        // Wait a moment in black
        yield return new WaitForSeconds(3f);

        // Start victory dialogue with separate panel (appears over the black fade)
        InitiateVictoryDialogue();
    }

    // Start victory dialogue
    private void InitiateVictoryDialogue()
    {
        dialogueActive = true;
        isInteracting = true;
        isDefeatDialogue = false;
        isVictoryDialogue = true;
        currentLine = 0;

        if (victoryDialogueUI != null)
            victoryDialogueUI.SetActive(true);

        ShowDefeatDialogueLine();
    }

    // White fade sequence with portal audio and scene transition
    private IEnumerator ExecuteWhiteFadeAndSceneTransition()
    {
        if (victoryDialogueUI != null)
            victoryDialogueUI.SetActive(false);

        dialogueActive = false;
        isInteracting = false;

        if (whiteFadeImage != null)
        {
            whiteFadeImage.SetActive(true);

            // Play portal audio at the start of white fade
            if (portalAudio != null && audioSource != null)
            {
                audioSource.PlayOneShot(portalAudio);
            }

            yield return StartCoroutine(PerformImageFade(whiteFadeImage, 0f, 1f, fadeDuration));
        }

        // Wait a moment before scene transition
        yield return new WaitForSeconds(0.5f);

        // Switch to scene 3
        SceneManager.LoadScene(3);
    }

    // Generic fade method
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

    private void RotateTowardsPlayer()
    {
        if (playerTransform == null) return;

        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            playerTransform = other.transform;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            playerTransform = null;

            if (dialogueActive && !isDefeated)
            {
                dialogueUI.SetActive(false);
                dialogueActive = false;
                isInteracting = false;
            }
        }
    }
}