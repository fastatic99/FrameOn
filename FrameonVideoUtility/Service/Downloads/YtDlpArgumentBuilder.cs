using System.Collections.Generic;
using FrameonVideoUtility.Service.Conversion;
using IOPath = System.IO.Path;

namespace FrameonVideoUtility.Service.Downloads;

public static class YtDlpArgumentBuilder
{
    public static List<string> BuildDownloadArguments(
        string url,
        string? denoPath,
        string workingDirectory,
        string sandboxDownloadFolder,
        string requestedFileName,
        string selectedFormat,
        bool forceFormat,
        string formatSelector,
        int? ffmpegThreadCount)
    {
        var arguments = new List<string>
        {
            "--newline",
            "--ignore-config",
            "--no-playlist",
            "--socket-timeout",
            "20",
            "--ffmpeg-location",
            workingDirectory,
            "-f",
            formatSelector
        };

        AddJsRuntime(arguments, denoPath);
        AddFfmpegPostProcessorThreadCount(arguments, ffmpegThreadCount);

        if (MediaFormatMapper.IsAudioFormat(selectedFormat))
        {
            arguments.Add("--extract-audio");
            arguments.Add("--audio-format");
            arguments.Add(MediaFormatMapper.MapYtDlpAudioFormat(selectedFormat));
            arguments.Add("--audio-quality");
            arguments.Add("0");
        }
        else if (MediaFormatMapper.IsVideoFormat(selectedFormat))
        {
            arguments.Add("--merge-output-format");
            arguments.Add(MediaFormatMapper.MapYtDlpVideoFormat(selectedFormat));
        }

        arguments.Add("-o");
        arguments.Add(IOPath.Combine(
            sandboxDownloadFolder,
            $"{MediaFormatMapper.SanitizeFileName(requestedFileName)}.%(ext)s"
        ));

        arguments.Add(url);

        return arguments;
    }

    public static List<string> BuildFormatInspectionArguments(string url, string? denoPath)
    {
        var arguments = new List<string>
        {
            "--ignore-config",
            "--no-playlist",
            "--no-warnings",
            "-F"
        };

        AddJsRuntime(arguments, denoPath);
        arguments.Add(url);

        return arguments;
    }

    public static List<string> BuildTitleLookupArguments(string url, string? denoPath)
    {
        var arguments = new List<string>
        {
            "--ignore-config",
            "--no-playlist",
            "--no-progress",
            "--no-warnings",
            "--print",
            "title"
        };

        AddJsRuntime(arguments, denoPath);
        arguments.Add(url);

        return arguments;
    }

    private static void AddJsRuntime(List<string> arguments, string? denoPath)
    {
        if (string.IsNullOrWhiteSpace(denoPath))
        {
            return;
        }

        arguments.Add("--js-runtimes");
        arguments.Add("deno:" + denoPath);
    }

    private static void AddFfmpegPostProcessorThreadCount(List<string> arguments, int? threadCount)
    {
        if (!threadCount.HasValue)
        {
            return;
        }

        arguments.Add("--postprocessor-args");
        arguments.Add($"ffmpeg:-threads {threadCount.Value}");
    }
}
