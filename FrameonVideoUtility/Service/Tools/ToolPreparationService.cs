using System;
using System.IO;
using System.Runtime.InteropServices;
using FrameonVideoUtility.Service.Logging;

namespace FrameonVideoUtility.Service.Tools;

public sealed class ToolPreparationService
{
    public string GetPreparedSandboxYtDlpPath()
    {
        AppLog.Info("[Tools] Using yt-dlp from PATH for the public source build.");
        return ResolveTool(PlatformToolNames.YtDlpFileName, "yt-dlp");
    }

    public string? TryGetPreparedDenoPath()
    {
        string? denoPath = TryResolveTool(PlatformToolNames.DenoFileName, "deno");

        if (string.IsNullOrWhiteSpace(denoPath))
        {
            AppLog.Warning("[Tools] Deno was not found in PATH. Some YouTube URLs may require it.");
            return null;
        }

        AppLog.Info("[Tools] Using Deno from PATH for the public source build.");
        return denoPath;
    }

    public string GetPreparedSandboxFfmpegPath()
    {
        AppLog.Info("[Tools] Using ffmpeg from PATH for the public source build.");
        return ResolveTool(PlatformToolNames.FfmpegFileName, "ffmpeg");
    }

    private static string ResolveTool(params string[] toolNames)
    {
        string? toolPath = TryResolveTool(toolNames);

        if (!string.IsNullOrWhiteSpace(toolPath))
        {
            return toolPath;
        }

        string fallback = toolNames.Length > 0 ? toolNames[^1] : "";
        AppLog.Warning($"[Tools] Could not find {fallback} in PATH. The operating system will try to resolve it when launched.");
        return fallback;
    }

    private static string? TryResolveTool(params string[] toolNames)
    {
        string? pathValue = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (string folder in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (string toolName in toolNames)
            {
                foreach (string candidateName in ExpandCandidateNames(toolName))
                {
                    string candidate = Path.Combine(folder, candidateName);

                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        return null;
    }

    private static string[] ExpandCandidateNames(string toolName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || Path.HasExtension(toolName))
        {
            return [toolName];
        }

        return [toolName, toolName + ".exe", toolName + ".cmd", toolName + ".bat"];
    }
}
