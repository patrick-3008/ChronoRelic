using UnityEngine;

public class CompanionManagement : MonoBehaviour
{
    public GameObject asrController;
    public GameObject Recorder;

    private RecordAudioUsingPython recordAudio;
    private BuildASR buildASR;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get the RecordAudio component from the Recorder GameObject
        if (Recorder != null)
        {
            recordAudio = Recorder.GetComponent<RecordAudioUsingPython>();
            if (recordAudio == null)
            {
                Debug.LogError("RecordAudio component not found on Recorder GameObject!");
            }
        }
        else
        {
            Debug.LogError("Recorder GameObject is not assigned!");
        }

        // Get the BuildASR component from the asrController GameObject
        if (asrController != null)
        {
            buildASR = asrController.GetComponent<BuildASR>();
            if (buildASR == null)
            {
                Debug.LogError("BuildASR component not found on asrController GameObject!");
            }
        }
        else
        {
            Debug.LogError("asrController GameObject is not assigned!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Check for Numpad1 key press - Start Recording
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            if (recordAudio != null)
            {
                Debug.Log("Numpad1 pressed - Starting recording...");
                recordAudio.StartRecording();
            }
            else
            {
                Debug.LogError("RecordAudio component is null!");
            }
        }

        // Check for Numpad2 key press - Stop Recording
        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            if (recordAudio != null)
            {
                Debug.Log("Numpad2 pressed - Stopping recording...");
                recordAudio.StopRecording();
            }
            else
            {
                Debug.LogError("RecordAudio component is null!");
            }
        }

        // Check for Numpad3 key press - Run ASR
        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            if (buildASR != null)
            {
                Debug.Log("Numpad3 pressed - Running ASR...");
                buildASR.build_asr();
            }
            else
            {
                Debug.LogError("BuildASR component is null!");
            }
        }
    }
}