using System.Diagnostics;
using System.IO;
using UnityEngine;

public class RecordAudioUsingPython : MonoBehaviour
{
    private Process process;
    private bool isRecording = false;
    private string pythonPath = "python"; // Ensure this is your Python executable
    private string scriptPath = "C://Developer//Unity Projects//ChronoRelic//Assets//ai//record_audio.py";
    private string outputFolder = @"C:\Developer\Unity Projects\ChronoRelic\Assets\ai\docker_image\sample_data";

    public void StartRecording()
    {
        if (isRecording)
        {
            UnityEngine.Debug.LogWarning("Recording is already in progress!");
            return;
        }

        if (!File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogError("Python script not found: " + scriptPath);
            return;
        }

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder); // Ensure the output folder exists
        }

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"\"{scriptPath}\" \"{outputFolder}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true, // Allows stopping input
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process = new Process { StartInfo = psi };
        process.OutputDataReceived += (sender, args) => UnityEngine.Debug.Log("Python Output: " + args.Data);
        process.ErrorDataReceived += (sender, args) => UnityEngine.Debug.LogError("Python Error: " + args.Data);

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            isRecording = true;
            UnityEngine.Debug.Log("Recording started.");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Failed to start Python process: " + e.Message);
        }
    }

    public void StopRecording()
    {
        if (!isRecording)
        {
            UnityEngine.Debug.LogWarning("No active recording to stop.");
            return;
        }

        try
        {
            using (StreamWriter writer = process.StandardInput)
            {
                writer.WriteLine(); // Sends an input signal to stop recording
            }

            process.WaitForExit();
            isRecording = false;
            UnityEngine.Debug.Log("Recording stopped.");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Failed to stop Python process: " + e.Message);
        }
    }
}
