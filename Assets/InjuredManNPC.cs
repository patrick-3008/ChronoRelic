using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Collections;

public class InjuredManNPC : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public List<string> injuredDialogues = new List<string>()
    {
        "Please help me, a large piece of wood fell on my leg and I can't return back to my family.",
        "Please call the town medic by the oasis in Ramses Village."
    };

    public TextMeshProUGUI dialogueTextBox;
    public GameObject dialogueUI;
    public TextMeshProUGUI nextIndicator; // small ">>" text UI
    public GameObject potionItem; // NEW: Public potion item to turn off when healed

    [Header("AI Conversation Settings")]
    public GameObject recorder; // The Recorder gameobject with RecordAudioUsingPython component
    public AudioSource audioSource;
    public TextMeshProUGUI recordingStatus; // Optional UI to show recording status

    private Animator anim;
    private bool isPlayerNearby = false;
    private Transform playerTransform;
    private bool isInteracting = false;
    private bool isHealed = false;
    private bool firstInteractionDone = false;
    private bool healedInteractionDone = false;
    private bool conversationCompleted = false;
    private bool isPlayerAlly = false;
    private bool isInAIConversation = false;
    private bool isRecording = false;
    private bool isProcessingAudio = false;
    private bool npcTurn = true; // Track whose turn it is to speak
    private bool waitingForUserInput = false;
    private bool isWaitingForNPCResponse = false;
    private bool isProcessingNPCResponse = false;
    private bool canBeHealed = false; // NEW: Track if ready to be healed

    private List<string> currentDialogues = new List<string>();
    private int currentDialogueIndex = 0;
    private float rotationSpeed = 5f;

    public AudioClip interactionSound;
    private RecordAudioUsingPython audioRecorder;
    private string aiDirectoryPath = @"D:\ChronoRelic\Assets\ai\agent";
    private string npcOutputPath = "D:/ChronoRelic/Assets/ai/agent/npc_output.txt";
    private string transcriptionPath = "D:/ChronoRelic/Assets/ai/agent/transcription.txt";
    private string outputAudioPath = "D:/ChronoRelic/Assets/ai/agent/output.wav";
    private string userInputPath = "D:/ChronoRelic/Assets/ai/agent/user_input.txt";
    private string userInputReadyPath = "D:/ChronoRelic/Assets/ai/agent/user_input_ready.txt"; // Signal file
    private string npcMemoryPath = "D:/ChronoRelic/Assets/ai/agent/Neferkare_memory.json";
    private string npcMemoryCleanPath = "D:/ChronoRelic/Assets/ai/agent/Neferkare_memory_clean.json";
    private string lastProcessedInputPath = "D:/ChronoRelic/Assets/ai/agent/last_processed_input.txt";

    // NPC4 process reference
    private Process npc4Process;
    private string lastSentInput = "";
    private int inputCounter = 0;
    private int npcResponseCount = 0;

    void Start()
    {
        dialogueUI.SetActive(false);
        if (nextIndicator != null)
            nextIndicator.gameObject.SetActive(false);

        anim = GetComponent<Animator>(); // No child model, npc is one object

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Get the audio recorder component
        if (recorder != null)
        {
            audioRecorder = recorder.GetComponent<RecordAudioUsingPython>();
        }

        if (recordingStatus != null)
            recordingStatus.gameObject.SetActive(false);

        // NEW: Enable text wrapping and auto-sizing for dialogue box
        if (dialogueTextBox != null)
        {
            dialogueTextBox.enableWordWrapping = true;
            dialogueTextBox.overflowMode = TextOverflowModes.Overflow;
            dialogueTextBox.enableAutoSizing = true;
            dialogueTextBox.fontSizeMin = 8f;
            dialogueTextBox.fontSizeMax = 25f;
        }

        // Reset Neferkare's memory to clean state on game launch
        ResetNeferkareMoryToClean();
    }

    private void ResetNeferkareMoryToClean()
    {
        try
        {
            if (File.Exists(npcMemoryCleanPath))
            {
                string cleanMemory = File.ReadAllText(npcMemoryCleanPath);
                File.WriteAllText(npcMemoryPath, cleanMemory);
                UnityEngine.Debug.Log("Neferkare's memory reset to clean state");
            }
            else
            {
                UnityEngine.Debug.LogWarning("Neferkare_Memory_Clean.txt not found - cannot reset memory");
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Error resetting Neferkare's memory: {e.Message}");
        }
    }

    void Update()
    {
        // NEW: Check if NPC can be healed (mission 2 complete but not yet healed)
        if (!isHealed && MissionManager.Instance.mission2_complete)
        {
            canBeHealed = true;
        }

        // Handle AI conversation input
        if (isInAIConversation)
        {
            HandleAIConversationInput();

            // Check for new NPC responses
            if (npcTurn && !waitingForUserInput && !isWaitingForNPCResponse && !isProcessingNPCResponse)
            {
                CheckForNPCResponse();
            }
        }
        // Normal dialogue input
        else if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (!isInteracting)
            {
                if (isHealed)
                {
                    if (conversationCompleted)
                    {
                        // Show final message and end interaction
                        ShowFinalMessage();
                    }
                    else
                    {
                        // Start AI conversation for healed NPC
                        StartCoroutine(InitializeAIConversation());
                    }
                }
                else if (canBeHealed)
                {
                    // NEW: Heal the NPC when E is pressed and mission 2 is complete
                    HealInjuredMan();
                }
                else
                {
                    // Start normal dialogue for injured NPC
                    StartDialogue();
                }
            }
            else
            {
                if (conversationCompleted)
                {
                    // End the final message display
                    EndFinalMessage();
                }
                else
                {
                    AdvanceDialogue();  // On subsequent 'E' presses, advance the dialogue
                }
            }
        }

        if (isInteracting && isHealed)
        {
            RotateTowardsPlayer();
        }
    }

    private void HandleAIConversationInput()
    {
        // If conversation is completed, don't allow any recording input
        if (conversationCompleted) return;

        // Only allow user input when it's their turn
        if (!waitingForUserInput) return;

        // Numpad7 - Start recording
        if (Input.GetKeyDown(KeyCode.Keypad7) && !isRecording && !isProcessingAudio)
        {
            StartRecording();
        }
        // Numpad8 - Stop recording
        else if (Input.GetKeyDown(KeyCode.Keypad8) && isRecording)
        {
            StopRecording();
        }
        // Numpad9 - Send recorded audio to Vosk
        else if (Input.GetKeyDown(KeyCode.Keypad9) && !isRecording && !isProcessingAudio)
        {
            ProcessRecordedAudio();
        }
        // Escape - Exit AI conversation
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitAIConversation();
        }
    }

    private void HealInjuredMan()
    {
        isHealed = true;
        canBeHealed = false; // Reset the flag

        // NEW: Complete mission 3 and turn off potion item
        if (!healedInteractionDone)
        {
            MissionManager.Instance.mission3_complete = true;
            healedInteractionDone = true;
            UnityEngine.Debug.Log("Mission 3 Complete!");

            // NEW: Turn off the potion item
            if (potionItem != null)
            {
                potionItem.SetActive(false);
                UnityEngine.Debug.Log("Potion item turned off - mission complete.");
            }
        }

        if (anim != null)
        {
            anim.SetTrigger("Healed");
            UnityEngine.Debug.Log("Injured man healed animation triggered.");
        }

        // Add Rigidbody if it doesn't exist, but configure it properly
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            UnityEngine.Debug.Log("Rigidbody added to InjuredManNPC.");
        }

        // Configure rigidbody to prevent falling through floor
        rb.mass = 1f;
        rb.linearDamping = 5f; // Add drag to prevent sliding
        rb.angularDamping = 5f;
        rb.useGravity = true;
        rb.isKinematic = false;

        // Lock all rotation axes and Y position to prevent falling/rotating
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationY |
                        RigidbodyConstraints.FreezeRotationZ |
                        RigidbodyConstraints.FreezePositionY; // Prevent falling

        // Ensure the NPC is positioned correctly on the ground
        StartCoroutine(EnsureProperPositioning());

        UnityEngine.Debug.Log("Injured man has been healed. Press E to start AI conversation.");
    }

    private IEnumerator EnsureProperPositioning()
    {
        // Wait a frame for physics to settle
        yield return new WaitForFixedUpdate();

        // Raycast down to find the ground
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 10f))
        {
            // Position the NPC slightly above the ground
            Vector3 groundPosition = hit.point;
            groundPosition.y += 0.1f; // Small offset to ensure it's above ground
            transform.position = groundPosition;
        }

        // Ensure the NPC is upright
        transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
    }

    private void StartDialogue()
    {
        dialogueUI.SetActive(true);
        isInteracting = true;
        currentDialogueIndex = 0;

        if (!isHealed)
        {
            currentDialogues.Clear();
            currentDialogues.AddRange(injuredDialogues);

            if (!firstInteractionDone)
            {
                MissionManager.Instance.mission1_complete = true;
                UnityEngine.Debug.Log("Mission 1 Complete!");
                firstInteractionDone = true;
            }
        }

        if (interactionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(interactionSound);
        }

        DisplayCurrentSentence();
    }

    private void AdvanceDialogue()
    {
        currentDialogueIndex++;

        if (currentDialogueIndex >= currentDialogues.Count)
        {
            EndDialogue();
        }
        else
        {
            DisplayCurrentSentence();
        }
    }

    private void DisplayCurrentSentence()
    {
        string sentence = currentDialogues[currentDialogueIndex];

        // Format the injured man's dialogue with rich text
        string formattedSentence = $"<b><color=yellow>Injured Man:</color></b> <color=white>{sentence}</color>";

        // Add ">>" to all but the last sentence
        if (currentDialogueIndex < currentDialogues.Count - 1)
        {
            formattedSentence += "  <color=yellow>>></color>";
        }

        dialogueTextBox.text = formattedSentence;
    }

    private void EndDialogue()
    {
        dialogueUI.SetActive(false);
        isInteracting = false;
        dialogueTextBox.text = "";

        if (nextIndicator != null)
            nextIndicator.gameObject.SetActive(false);

        // Mark missions as complete based on interactions
        if (!isHealed && !firstInteractionDone)
        {
            firstInteractionDone = true;
            MissionManager.Instance.mission1_complete = true;
            UnityEngine.Debug.Log("Mission 1 complete after first dialogue.");
        }
    }

    private IEnumerator InitializeAIConversation()
    {
        UnityEngine.Debug.Log("Starting AI conversation initialization...");

        // Clear any existing files
        ClearConversationFiles();

        // Start NPC4 process in the background
        StartNPC4Process();

        // Enter AI conversation mode
        isInAIConversation = true;
        dialogueUI.SetActive(true);

        if (recordingStatus != null)
        {
            recordingStatus.gameObject.SetActive(true);
            recordingStatus.text = "AI conversation starting...";
        }

        // Set initial state - NPC speaks first
        npcTurn = true;
        waitingForUserInput = false;

        // Show initialization message with Neferkare thinking
        dialogueTextBox.text = "<color=yellow>Starting conversation with Neferkare...</color>\n\n<color=yellow>Neferkare is thinking...</color>";

        yield return new WaitForSeconds(1f); // Give NPC4 time to start

        // Check for initial NPC response
        yield return StartCoroutine(WaitForAndProcessNPCResponse());
    }

    private void StartNPC4Process()
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "python",
                Arguments = "npc4.py",
                WorkingDirectory = aiDirectoryPath,
                UseShellExecute = false,
                RedirectStandardInput = true,  // Enable input redirection
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true
            };

            npc4Process = Process.Start(startInfo);
            UnityEngine.Debug.Log("NPC4 process started successfully");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to start NPC4 process: {e.Message}");
        }
    }

    private void ClearConversationFiles()
    {
        // Clear existing files
        try
        {
            if (File.Exists(npcOutputPath))
                File.WriteAllText(npcOutputPath, "");
            if (File.Exists(transcriptionPath))
                File.WriteAllText(transcriptionPath, "");
            if (File.Exists(userInputPath))
                File.WriteAllText(userInputPath, "");
            if (File.Exists(userInputReadyPath))
                File.Delete(userInputReadyPath); // Delete signal file
            if (File.Exists(outputAudioPath))
                File.Delete(outputAudioPath); // Delete old audio file
            if (File.Exists(lastProcessedInputPath))
                File.WriteAllText(lastProcessedInputPath, ""); // Clear last processed input

            // Reset input tracking
            lastSentInput = "";
            inputCounter = 0;
            npcResponseCount = 0;

            UnityEngine.Debug.Log("All conversation files cleared");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Error clearing files: {e.Message}");
        }
    }

    private void CheckForNPCResponse()
    {
        // Only check if we're waiting for NPC and not processing user input and not already waiting or processing
        if (!npcTurn || waitingForUserInput || isProcessingAudio || isWaitingForNPCResponse || isProcessingNPCResponse) return;

        if (File.Exists(npcOutputPath))
        {
            try
            {
                string response = File.ReadAllText(npcOutputPath).Trim();
                if (!string.IsNullOrEmpty(response))
                {
                    // Set flags to prevent multiple calls and conflicts
                    isWaitingForNPCResponse = true;
                    isProcessingNPCResponse = true;
                    npcTurn = false;

                    // Immediately clear the file to prevent re-processing
                    File.WriteAllText(npcOutputPath, "");

                    StartCoroutine(ProcessNPCResponse(response));
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error reading NPC response: {e.Message}");
            }
        }
    }

    private IEnumerator WaitForAndProcessNPCResponse()
    {
        isWaitingForNPCResponse = true;

        float timeout = 15f; // Increased timeout to 15 seconds for audio generation
        float elapsed = 0f;
        bool responseProcessed = false;

        while (elapsed < timeout && !responseProcessed)
        {
            if (File.Exists(npcOutputPath))
            {
                string response = File.ReadAllText(npcOutputPath).Trim();
                if (!string.IsNullOrEmpty(response))
                {
                    // Set processing flag to prevent other methods from interfering
                    isProcessingNPCResponse = true;

                    // Immediately clear the file to prevent re-processing
                    File.WriteAllText(npcOutputPath, "");

                    // Wait a bit more for audio file to be generated by NPC4
                    yield return new WaitForSeconds(2f);

                    yield return StartCoroutine(ProcessNPCResponse(response));
                    responseProcessed = true;
                    isWaitingForNPCResponse = false;
                    yield break;
                }
            }
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        // Only show timeout message if no response was actually processed
        if (!responseProcessed)
        {
            UnityEngine.Debug.LogWarning("Timeout waiting for NPC response");
            dialogueTextBox.text += "\n\n<color=red>Neferkare seems to be having trouble responding. Try speaking again.</color>\n\n<color=yellow>Press Numpad7 to start recording.</color>";

            // Reset to user turn if timeout occurs
            npcTurn = false;
            waitingForUserInput = true;

            if (recordingStatus != null)
                recordingStatus.text = "Your turn - Press Numpad7 to start recording";
        }

        isWaitingForNPCResponse = false;
    }

    private IEnumerator ProcessNPCResponse(string response)
    {
        // Ensure we're not processing multiple responses
        if (isProcessingNPCResponse && response != null)
        {
            UnityEngine.Debug.Log($"Processing NPC response: {response.Substring(0, Mathf.Min(50, response.Length))}...");
        }
        else
        {
            UnityEngine.Debug.LogWarning("Attempted to process NPC response while already processing or response is null");
            yield break;
        }

        // Increment the NPC response counter
        npcResponseCount++;
        UnityEngine.Debug.Log($"NPC Response Count: {npcResponseCount}");

        // Clear files immediately to prevent duplicate processing
        try
        {
            File.WriteAllText(npcOutputPath, "");
            File.WriteAllText(userInputPath, "");

            // Verify the last processed input to ensure we're not processing duplicates
            if (File.Exists(lastProcessedInputPath))
            {
                string lastProcessed = File.ReadAllText(lastProcessedInputPath).Trim();
                UnityEngine.Debug.Log($"Last processed input was: {lastProcessed}");
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Error clearing files: {e.Message}");
        }

        // Display NPC response in text with proper wrapping
        dialogueTextBox.text = $"<b><color=yellow>Neferkare:</color></b> <color=white>{response}</color>";

        // Check if audio file already exists (generated by NPC4)
        if (File.Exists(outputAudioPath))
        {
            // Play the pre-generated audio
            yield return StartCoroutine(PlayAudioFile("output.wav"));

            // Delete the audio file after playing to prevent reuse
            try
            {
                File.Delete(outputAudioPath);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error deleting audio file: {e.Message}");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("Audio file not found - NPC4 may not have generated audio yet");
        }

        // Check if this is the 7th response (final response)
        if (npcResponseCount >= 8)
        {
            // Determine if player is ally or enemy based on response
            string lowerResponse = response.ToLower();
            if (lowerResponse.Contains("ally") || lowerResponse.Contains("friend") ||
                lowerResponse.Contains("trust you") || lowerResponse.Contains("choose you") ||
                lowerResponse.Contains("side with"))
            {
                isPlayerAlly = true;
            }
            else if (lowerResponse.Contains("enemy") || lowerResponse.Contains("foe") ||
                     lowerResponse.Contains("cannot trust") || lowerResponse.Contains("against you"))
            {
                isPlayerAlly = false;
            }
            else
            {
                // Default to checking for positive/negative keywords
                isPlayerAlly = lowerResponse.Contains("trust") || lowerResponse.Contains("help");
            }

            conversationCompleted = true;

            string allyStatus = isPlayerAlly ? "ALLY" : "ENEMY";
            dialogueTextBox.text += $"\n\n<color=yellow>Neferkare has chosen you as an {allyStatus}.</color>";
            dialogueTextBox.text += "\n\n<color=yellow>Press E to end the conversation.</color>";

            // Disable recording controls
            waitingForUserInput = false;
            isWaitingForNPCResponse = false;
            isProcessingNPCResponse = false;

            if (recordingStatus != null)
                recordingStatus.text = $"Conversation ended - You are his {allyStatus.ToLower()}";

            UnityEngine.Debug.Log($"Conversation completed - Player is {allyStatus}");
            yield break;
        }

        // Switch to user turn
        waitingForUserInput = true;
        isWaitingForNPCResponse = false;
        isProcessingNPCResponse = false;

        // Update UI for user input - append to existing text
        if (recordingStatus != null)
            recordingStatus.text = "Your turn - Press Numpad7 to start recording";

        dialogueTextBox.text += "\n\n<color=yellow>Your turn to speak. Press Numpad7 to start recording.</color>";

        UnityEngine.Debug.Log("NPC response processing complete - switched to user turn");
    }

    private void ShowFinalMessage()
    {
        dialogueUI.SetActive(true);
        isInteracting = true;

        string allyStatus = isPlayerAlly ? "ALLY" : "ENEMY";
        string statusColor = isPlayerAlly ? "green" : "red";

        dialogueTextBox.text = $"<b><color=yellow>Neferkare:</color></b> <color=white>Our conversation is finished.</color>";
        dialogueTextBox.text += $"\n\n<color={statusColor}>You have been marked as an {allyStatus}.</color>";
        dialogueTextBox.text += "\n\n<color=yellow>Press E to leave.</color>";

        UnityEngine.Debug.Log($"Showing final message - Player is {allyStatus}");
    }

    private void EndFinalMessage()
    {
        dialogueUI.SetActive(false);
        isInteracting = false;

        UnityEngine.Debug.Log("Final message ended - No further interaction possible");
    }

    private void StartRecording()
    {
        if (audioRecorder != null)
        {
            isRecording = true;
            audioRecorder.StartRecording();

            if (recordingStatus != null)
                recordingStatus.text = "Recording... Press Numpad8 to stop";

            UnityEngine.Debug.Log("Started recording audio...");
        }
    }

    private void StopRecording()
    {
        if (audioRecorder != null)
        {
            isRecording = false;
            audioRecorder.StopRecording();

            if (recordingStatus != null)
                recordingStatus.text = "Recording stopped. Press Numpad9 to process";

            UnityEngine.Debug.Log("Stopped recording audio...");
        }
    }

    private void ProcessRecordedAudio()
    {
        if (!isProcessingAudio)
        {
            StartCoroutine(ProcessAudioAndSendToNPC());
        }
    }

    private IEnumerator ProcessAudioAndSendToNPC()
    {
        isProcessingAudio = true;
        waitingForUserInput = false;

        if (recordingStatus != null)
            recordingStatus.text = "Processing audio...";

        // Clear previous "your turn" message and show processing
        dialogueTextBox.text = dialogueTextBox.text.Split(new string[] { "\n\n<color=yellow>" }, System.StringSplitOptions.None)[0];
        dialogueTextBox.text += "\n\n<color=yellow>Processing your speech...</color>";

        // Run voskCode.py to convert audio to text
        yield return StartCoroutine(RunPythonScript("voskCode.py"));

        // Read the transcribed text
        string transcribedText = ReadTranscribedText();

        if (!string.IsNullOrEmpty(transcribedText))
        {
            // Update dialogue to show what user said and that NPC is thinking
            dialogueTextBox.text = dialogueTextBox.text.Split(new string[] { "\n\n<color=yellow>" }, System.StringSplitOptions.None)[0];
            dialogueTextBox.text += $"\n\n<b>You:</b> <color=lightblue>\"{transcribedText}\"</color>";
            dialogueTextBox.text += "\n\n<color=yellow>Neferkare is thinking...</color>";

            // Send the transcribed text to NPC4
            yield return StartCoroutine(SendTextToNPC(transcribedText));

            // Switch to NPC turn and reset processing flags
            npcTurn = true;
            waitingForUserInput = false;
            isProcessingNPCResponse = false; // Reset this flag before waiting for response

            // Wait for NPC response
            yield return StartCoroutine(WaitForAndProcessNPCResponse());
        }
        else
        {
            if (recordingStatus != null)
                recordingStatus.text = "No speech detected. Try recording again...";

            // Remove processing message and show error
            dialogueTextBox.text = dialogueTextBox.text.Split(new string[] { "\n\n<color=yellow>" }, System.StringSplitOptions.None)[0];
            dialogueTextBox.text += "\n\n<color=red>No speech detected. Please try recording again.</color>\n\n<color=yellow>Press Numpad7 to start recording.</color>";

            waitingForUserInput = true;
        }

        isProcessingAudio = false;
    }

    private IEnumerator RunPythonScript(string scriptName)
    {
        UnityEngine.Debug.Log($"Running {scriptName}...");

        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = "python",
            Arguments = $"\"{Path.Combine(aiDirectoryPath, scriptName)}\"",
            WorkingDirectory = aiDirectoryPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo))
        {
            while (!process.HasExited)
            {
                yield return null;
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (!string.IsNullOrEmpty(output))
                UnityEngine.Debug.Log($"{scriptName} output: {output}");

            if (!string.IsNullOrEmpty(error))
                UnityEngine.Debug.LogError($"{scriptName} error: {error}");
        }
    }

    private IEnumerator PlayAudioFile(string audioFileName)
    {
        string audioFilePath = Path.Combine(aiDirectoryPath, audioFileName);

        if (File.Exists(audioFilePath))
        {
            UnityEngine.Debug.Log($"Playing {audioFileName}...");

            // Load and play the audio file
            yield return StartCoroutine(LoadAndPlayAudio(audioFilePath));
        }
        else
        {
            UnityEngine.Debug.LogWarning($"Audio file not found: {audioFilePath}");
        }
    }

    private IEnumerator LoadAndPlayAudio(string filePath)
    {
        using (UnityEngine.WWW www = new UnityEngine.WWW("file://" + filePath))
        {
            yield return www;

            if (string.IsNullOrEmpty(www.error))
            {
                AudioClip clip = www.GetAudioClip(false, false);
                if (clip != null && audioSource != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();

                    // Wait for audio to finish playing
                    while (audioSource.isPlaying)
                    {
                        yield return null;
                    }
                }
            }
            else
            {
                UnityEngine.Debug.LogError($"Failed to load audio: {www.error}");
            }
        }
    }

    private string ReadTranscribedText()
    {
        if (File.Exists(transcriptionPath))
        {
            try
            {
                string content = File.ReadAllText(transcriptionPath);
                // Clear the file after reading
                File.WriteAllText(transcriptionPath, "");
                return content.Trim();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error reading transcription: {e.Message}");
            }
        }
        return "";
    }

    private IEnumerator SendTextToNPC(string text)
    {
        // Prevent sending duplicate inputs
        if (string.IsNullOrEmpty(text) || text == lastSentInput)
        {
            UnityEngine.Debug.LogWarning($"Skipping duplicate or empty input: '{text}'");
            yield break;
        }

        // Increment counter and create unique input
        inputCounter++;
        string uniqueInput = $"[INPUT_{inputCounter}] {text}";

        // Clear any existing NPC output and user input before sending new input
        if (File.Exists(npcOutputPath))
        {
            try
            {
                File.WriteAllText(npcOutputPath, "");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error clearing NPC output file: {e.Message}");
            }
        }

        if (File.Exists(userInputPath))
        {
            try
            {
                File.WriteAllText(userInputPath, "");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error clearing user input file: {e.Message}");
            }
        }

        // Wait a moment to ensure files are cleared
        yield return new WaitForSeconds(0.2f);

        try
        {
            // Save the unique input to prevent duplicates
            File.WriteAllText(userInputPath, uniqueInput);
            File.WriteAllText(lastProcessedInputPath, uniqueInput);

            // Update tracking variables
            lastSentInput = text;

            UnityEngine.Debug.Log($"Sent unique user input to NPC: {uniqueInput}");

            // Send the text directly to the NPC4 process via standard input
            if (npc4Process != null && !npc4Process.HasExited)
            {
                npc4Process.StandardInput.WriteLine(uniqueInput);
                npc4Process.StandardInput.Flush();
                UnityEngine.Debug.Log("Text sent directly to NPC4 process");
            }
            else
            {
                UnityEngine.Debug.LogWarning("NPC4 process is not running or has exited");
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Error sending text to NPC: {e.Message}");
        }

        // Wait a frame to ensure files are written
        yield return null;
    }

    private void ExitAIConversation()
    {
        isInAIConversation = false;
        isRecording = false;
        isProcessingAudio = false;
        waitingForUserInput = false;
        npcTurn = false;
        isWaitingForNPCResponse = false;
        isProcessingNPCResponse = false;

        // Reset input tracking
        lastSentInput = "";
        inputCounter = 0;
        npcResponseCount = 0;

        // Don't reset conversationCompleted - keep it persistent

        // Stop NPC4 process
        if (npc4Process != null && !npc4Process.HasExited)
        {
            try
            {
                npc4Process.Kill();
                npc4Process.Dispose();
                npc4Process = null;
                UnityEngine.Debug.Log("NPC4 process terminated");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error terminating NPC4 process: {e.Message}");
            }
        }

        dialogueUI.SetActive(false);

        if (recordingStatus != null)
            recordingStatus.gameObject.SetActive(false);

        UnityEngine.Debug.Log("Exited AI conversation mode");
    }

    private void RotateTowardsPlayer()
    {
        if (playerTransform == null) return;

        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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

            // Exit AI conversation if player leaves
            if (isInAIConversation)
            {
                ExitAIConversation();
            }

            dialogueUI.SetActive(false);
            isInteracting = false;
        }
    }

    void OnDestroy()
    {
        // Clean up NPC4 process on destroy
        if (npc4Process != null && !npc4Process.HasExited)
        {
            try
            {
                npc4Process.Kill();
                npc4Process.Dispose();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error cleaning up NPC4 process: {e.Message}");
            }
        }
    }
}