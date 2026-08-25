using System.Collections.Generic;

namespace FrameonVideoUtility.Service.Conversion;

public static class FfmpegArgumentBuilder
{
    public static List<string> BuildAudioArguments(
        string inputFile,
        string outputFile,
        string outputFormat,
        int? threadCount)
    {
        var arguments = new List<string>
        {
            "-hide_banner",
            "-y",
            "-i",
            inputFile,
            "-vn"
        };

        switch (outputFormat)
        {
            case "mp3":
                arguments.AddRange(["-c:a", "libmp3lame", "-q:a", "2"]);
                break;
            case "aac":
            case "m4a":
                arguments.AddRange(["-c:a", "aac", "-b:a", "192k"]);
                break;
            case "wav":
                arguments.AddRange(["-c:a", "pcm_s16le"]);
                break;
            case "flac":
                arguments.AddRange(["-c:a", "flac"]);
                break;
            case "ogg":
                arguments.AddRange(["-c:a", "libvorbis", "-q:a", "5"]);
                break;
            case "opus":
                arguments.AddRange(["-c:a", "libopus", "-b:a", "160k"]);
                break;
            default:
                arguments.AddRange(["-c:a", "aac", "-b:a", "192k"]);
                break;
        }

        AddThreadCount(arguments, threadCount);
        arguments.Add(outputFile);
        return arguments;
    }

    public static List<string> BuildVideoArguments(
        string inputFile,
        string outputFile,
        string outputFormat,
        int? threadCount)
    {
        var arguments = new List<string>
        {
            "-hide_banner",
            "-y",
            "-i",
            inputFile
        };

        switch (outputFormat)
        {
            case "mp4":
                arguments.AddRange(["-c:v", "libx264", "-preset", "veryfast", "-crf", "18", "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart"]);
                break;
            case "mov":
            case "mkv":
                arguments.AddRange(["-c:v", "libx264", "-preset", "veryfast", "-crf", "18", "-c:a", "aac", "-b:a", "192k"]);
                break;
            case "webm":
                arguments.AddRange(["-c:v", "libvpx-vp9", "-deadline", "realtime", "-cpu-used", "6", "-b:v", "0", "-crf", "28", "-c:a", "libopus", "-b:a", "160k"]);
                break;
            case "avi":
                arguments.AddRange(["-c:v", "mpeg4", "-q:v", "3", "-c:a", "libmp3lame", "-q:a", "2"]);
                break;
            case "wmv":
                arguments.AddRange(["-c:v", "wmv2", "-b:v", "4000k", "-c:a", "wmav2", "-b:a", "192k"]);
                break;
            case "flv":
                arguments.AddRange(["-c:v", "flv", "-b:v", "4000k", "-c:a", "libmp3lame", "-q:a", "2"]);
                break;
            default:
                arguments.AddRange(["-c:v", "libx264", "-preset", "veryfast", "-crf", "18", "-c:a", "aac", "-b:a", "192k"]);
                break;
        }

        AddThreadCount(arguments, threadCount);
        arguments.Add(outputFile);
        return arguments;
    }

    private static void AddThreadCount(List<string> arguments, int? threadCount)
    {
        if (!threadCount.HasValue)
        {
            return;
        }

        arguments.AddRange(["-threads", threadCount.Value.ToString()]);
    }
}
