using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using System.Collections;
using Debug = UnityEngine.Debug;
using System.Runtime.InteropServices;

public class SetupEverything : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(RunScriptsSimultaneously());
    }

    private IntPtr jobHandle;
    private string tempBatchFile;
    private Process batchProcess;

    private void OnEnable()
    {
        Application.quitting += Cleanup;
    }

    private void OnDisable()
    {
        Application.quitting -= Cleanup;
        Cleanup();
    }

    public void StartScripts()
    {
        StartCoroutine(RunScriptsSimultaneously());
    }

    private IEnumerator RunScriptsSimultaneously()
    {
        string[] scriptPaths = new string[]
        {
            "C:/Developer/Unity Projects/ChronoRelic/Assets/ai/load_egtts.cmd",
            "C:/Developer/Unity Projects/ChronoRelic/Assets/ai/load_docker.cmd",
            "C:/Developer/Unity Projects/ChronoRelic/Assets/ai/load_rag.cmd"
            // Add other script paths here
        };

        // Create unique batch file
        tempBatchFile = Path.Combine(Path.GetTempPath(), $"launch_scripts_{Guid.NewGuid()}.cmd");

        try
        {
            // Create batch content
            using (StreamWriter sw = new StreamWriter(tempBatchFile))
            {
                foreach (string scriptPath in scriptPaths)
                {
                    if (!File.Exists(scriptPath))
                    {
                        UnityEngine.Debug.LogError($"Script not found: {scriptPath}");
                        continue;
                    }
                    sw.WriteLine($"start \"\" /B cmd /C \"{scriptPath}\"");
                }
            }

            // Create job object to manage processes
            CreateJobObject();

            // Start batch process
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = tempBatchFile,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true,
                CreateNoWindow = true
            };

            batchProcess = Process.Start(psi);
            AddProcessToJob(batchProcess);

            UnityEngine.Debug.Log("Scripts launched with admin privileges");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Launch failed: {ex.Message}");
            Cleanup();
        }

        yield return null;
    }

    private void CreateJobObject()
    {
        jobHandle = CreateJobObject(IntPtr.Zero, null);

        if (jobHandle == IntPtr.Zero)
            throw new Exception("Failed to create job object");

        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = 0x2000 // JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            }
        };

        int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
        IntPtr infoPtr = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(info, infoPtr, false);

        if (!SetInformationJobObject(jobHandle, 9, infoPtr, (uint)length))
            throw new Exception("Failed to set job information");

        Marshal.FreeHGlobal(infoPtr);
    }

    private void AddProcessToJob(Process process)
    {
        if (jobHandle == IntPtr.Zero) return;

        if (!AssignProcessToJobObject(jobHandle, process.Handle))
            throw new Exception("Failed to add process to job");
    }

    private void Cleanup()
    {
        // Close job handle (automatically kills all child processes)
        if (jobHandle != IntPtr.Zero)
        {
            CloseHandle(jobHandle);
            jobHandle = IntPtr.Zero;
        }

        // Clean up batch process
        if (batchProcess != null && !batchProcess.HasExited)
        {
            try { batchProcess.Kill(); }
            catch { /* Ignore */ }
            batchProcess = null;
        }

        // Delete temporary batch file
        if (!string.IsNullOrEmpty(tempBatchFile) && File.Exists(tempBatchFile))
        {
            try { File.Delete(tempBatchFile); }
            catch { /* Ignore */ }
            tempBatchFile = null;
        }
    }

    // Native interop for job management
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string name);

    [DllImport("kernel32.dll")]
    private static extern bool SetInformationJobObject(IntPtr job, int infoType,
        IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll")]
    private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

}
