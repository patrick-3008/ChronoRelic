using UnityEngine;
using TMPro;

public class inputDialogue : MonoBehaviour
{
    [Header("Dialogue")]
    public string[] npcDialogue = {
        "Pharaoh: Hello, traveler! Welcome to our village.",
        "Pharaoh: We have been expecting someone like you.",
        "Pharaoh: The ancient treasures lie beyond the desert.",
        "Pharaoh: Be careful on your journey, brave adventurer."
    };

    [Header("UI")]
    public TextMeshProUGUI dialogueTextBox;
    public GameObject dialogueUI;

    [Header("Scene Reference")]
    public introSceneManager sceneController; // Drag your SceneController here

    private bool isInDialogue = false;
    private int currentLineIndex = 0;

    void Start()
    {
        dialogueUI.SetActive(false);

        // Configure text wrapping
        if (dialogueTextBox != null)
        {
            dialogueTextBox.enableWordWrapping = true;
            dialogueTextBox.overflowMode = TextOverflowModes.Overflow;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isInDialogue)
            {
                dialogueUI.SetActive(true);
                StartDialogue();
            }
            else
            {
                NextLine();
            }
        }
    }

    private void StartDialogue()
    {
        isInDialogue = true;
        currentLineIndex = 0;
        dialogueUI.SetActive(true);
        ShowCurrentLine();
    }

    private void NextLine()
    {
        currentLineIndex++;

        if (currentLineIndex < npcDialogue.Length)
        {
            ShowCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    private void ShowCurrentLine()
    {
        dialogueTextBox.text = npcDialogue[currentLineIndex];
    }

    private void EndDialogue()
    {
        isInDialogue = false;
        dialogueUI.SetActive(false);
        currentLineIndex = 0;

        // Trigger fade to white
        if (sceneController != null)
        {
            sceneController.FadeToWhite();
        }
    }
}