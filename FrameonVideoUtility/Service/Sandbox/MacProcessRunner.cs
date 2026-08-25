using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FrameonVideoUtility.Service.Sandbox;

public sealed class MacProcessRunner : ISandboxedProcessRunner
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
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };

        process.Start();
        TrySetLowerPriority(process);

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch { }
        });

        Task outputTask = ReadLinesAsync(process.StandardOutput, onOutput, cancellationToken);
        Task errorTask = ReadLinesAsync(process.StandardError, onError, cancellationToken);

        try
        {
            await Task.WhenAll(outputTask, errorTask, process.WaitForExitAsync(cancellationToken));

            return new SandboxedProcessResult
            {
                ExitCode = process.ExitCode,
                WasCanceled = cancellationToken.IsCancellationRequested
            };
        }
        catch (OperationCanceledException)
        {
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
}
