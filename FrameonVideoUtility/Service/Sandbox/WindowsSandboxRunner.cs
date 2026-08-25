using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FrameonVideoUtility.Service.Sandbox;

public sealed class WindowsSandboxRunner : ISandboxedProcessRunner
{
    public async Task<SandboxedProcessResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        string allowedDownloadFolder,
        string allowedTempFolder,
        Action<string>? onOutput,
        Action<string>? onError,
        CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("WindowsSandboxRunner can only run on Windows.");

        if (!File.Exists(executablePath))
            throw new FileNotFoundException("Executable was not found.", executablePath);

        Directory.CreateDirectory(workingDirectory);
        Directory.CreateDirectory(allowedDownloadFolder);
        Directory.CreateDirectory(allowedTempFolder);

        using var job = WindowsJobObject.Create();

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.Environment.Clear();
        startInfo.Environment["PATH"] = workingDirectory;
        startInfo.Environment["TEMP"] = allowedTempFolder;
        startInfo.Environment["TMP"] = allowedTempFolder;
        startInfo.Environment["SystemRoot"] = Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows";
        startInfo.Environment["WINDIR"] = Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows";

        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        try
        {
            onOutput?.Invoke("[job] Starting process...");
            onOutput?.Invoke($"[job] Executable: {executablePath}");
            onOutput?.Invoke($"[job] Working directory: {workingDirectory}");
            onOutput?.Invoke($"[job] Download folder: {allowedDownloadFolder}");
            onOutput?.Invoke($"[job] Temp folder: {allowedTempFolder}");

            if (!process.Start())
                throw new InvalidOperationException("Failed to start process.");

            TrySetLowerPriority(process);
            job.AssignProcess(process);

            using var registration = cancellationToken.Register(() =>
            {
                try
                {
                    onOutput?.Invoke("[job] Cancellation requested. Terminating job...");
                    job.Terminate();
                }
                catch
                {
                    // Ignore cancel race conditions.
                }
            });

            Task outputTask = ReadLinesAsync(process.StandardOutput, onOutput, cancellationToken);
            Task errorTask = ReadLinesAsync(process.StandardError, onError, cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            try
            {
                await Task.WhenAll(outputTask, errorTask);
            }
            catch (OperationCanceledException)
            {
                // Cancellation already handled by the job object.
            }

            onOutput?.Invoke($"[job] Process exited with code {process.ExitCode}.");

            return new SandboxedProcessResult
            {
                ExitCode = process.ExitCode,
                WasCanceled = cancellationToken.IsCancellationRequested
            };
        }
        catch (OperationCanceledException)
        {
            try
            {
                job.Terminate();
            }
            catch
            {
                // Ignore cancel race conditions.
            }

            return new SandboxedProcessResult
            {
                ExitCode = -1,
                WasCanceled = true
            };
        }
    }

    private static async Task ReadLinesAsync(
        StreamReader reader,
        Action<string>? handler,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync();

            if (line == null)
                break;

            handler?.Invoke(line);
        }
    }

    private static void TrySetLowerPriority(Process process)
    {
        try
        {
            process.PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        catch
        {
            // Process priority is best-effort and platform dependent.
        }
    }

    private sealed class WindowsJobObject : IDisposable
    {
        private readonly IntPtr _handle;
        private bool _disposed;

        private WindowsJobObject(IntPtr handle)
        {
            _handle = handle;
        }

        public static WindowsJobObject Create()
        {
            IntPtr handle = CreateJobObject(IntPtr.Zero, null);

            if (handle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create Windows Job Object.");

            var job = new WindowsJobObject(handle);
            job.ConfigureKillOnClose();
            return job;
        }

        public void AssignProcess(Process process)
        {
            if (!AssignProcessToJobObject(_handle, process.Handle))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to assign process to Job Object.");
        }

        public void Terminate()
        {
            bool success = TerminateJobObject(_handle, 1);

            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to terminate Windows Job Object.");
        }

        private void ConfigureKillOnClose()
        {
            var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                {
                    LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                }
            };

            int length = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
            IntPtr infoPtr = Marshal.AllocHGlobal(length);

            try
            {
                Marshal.StructureToPtr(info, infoPtr, false);

                bool success = SetInformationJobObject(
                    _handle,
                    JobObjectExtendedLimitInformation,
                    infoPtr,
                    (uint)length
                );

                if (!success)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to configure Windows Job Object.");
            }
            finally
            {
                Marshal.FreeHGlobal(infoPtr);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            CloseHandle(_handle);
            _disposed = true;
        }
    }

    private const int JobObjectExtendedLimitInformation = 9;
    private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public long Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
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

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(
        IntPtr hJob,
        int jobObjectInfoClass,
        IntPtr lpJobObjectInfo,
        uint cbJobObjectInfoLength
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateJobObject(IntPtr hJob, uint uExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}
