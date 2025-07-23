using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class BuildASR : MonoBehaviour
{
    public string cmdFilePath = @"C:\Developer\Unity Projects\ChronoRelic\Assets\ai\infer.cmd";
    public string wavFilePath = @"C:\Developer\Unity Projects\ChronoRelic\Assets\ai\egtts\egtts_output.wav";
    public AudioSource audioSource;

    public void build_asr()
    {
        StartCoroutine(RunProcessAndPlayAudio());
    }

    IEnumerator RunProcessAndPlayAudio()
    {
        // Ensure audio source exists
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("Created AudioSource component");
        }

        // Create and start the process
        Process proc = null;
        bool processStarted = false;

        try
        {
            ProcessStartInfo procInfo = new ProcessStartInfo
            {
                FileName = cmdFilePath,
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            proc = Process.Start(procInfo);
            processStarted = true;
            Debug.Log("ASR process started successfully");
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            Debug.LogError("Admin elevation failed: " + e.Message);
            yield break;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Process start error: " + e.Message);
            yield break;
        }

        // Wait for process completion
        if (processStarted)
        {
            Debug.Log("Waiting for ASR process to complete...");
            while (!proc.HasExited)
            {
                yield return new WaitForSeconds(0.5f);
            }
            Debug.Log("ASR process completed successfully");
        }

        // Play audio after process completes
        yield return StartCoroutine(LoadAndPlayAudio(wavFilePath));

        // Cleanup
        if (proc != null)
        {
            proc.Close();
            proc.Dispose();
        }
    }

    IEnumerator LoadAndPlayAudio(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Audio path is empty");
            yield break;
        }

        if (!File.Exists(path))
        {
            Debug.LogError("Audio file not found: " + path);
            yield break;
        }

        string uri = "file:///" + path.Replace("\\", "/");

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Audio load failed: {www.error}");
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log("Now playing audio: " + Path.GetFileName(path));
            }
            else
            {
                Debug.LogError("Failed to create audio clip from downloaded data");
            }
        }
    }
}