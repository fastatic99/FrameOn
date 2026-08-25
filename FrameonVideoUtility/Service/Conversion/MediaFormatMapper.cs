using System;
using IOPath = System.IO.Path;

namespace FrameonVideoUtility.Service.Conversion;

public static class MediaFormatMapper
{
    public static string NormalizeExtension(string value)
    {
        return value.Trim().TrimStart('.').ToLowerInvariant();
    }

    public static string BuildSafeOutputFileName(string inputPath, string outputExtension)
    {
        string baseName = IOPath.GetFileNameWithoutExtension(inputPath);
        baseName = SanitizeFileName(baseName);
        outputExtension = NormalizeExtension(outputExtension);

        return $"{baseName}.{outputExtension}";
    }

    public static string SanitizeFileName(string fileName)
    {
        foreach (char invalid in IOPath.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(fileName)
            ? $"FrameOn-{DateTime.Now:yyyyMMdd-HHmmss}"
            : fileName;
    }

    public static bool IsSupportedAudioVideoInputFile(string filePath)
    {
        string extension = NormalizeExtension(IOPath.GetExtension(filePath));

        return extension is
            "mp4" or "mov" or "avi" or "wmv" or "mkv" or "flv" or "webm" or
            "mp3" or "aac" or "m4a" or "wav" or "flac" or "ogg" or "opus";
    }

    public static bool IsSupportedVideoInputFile(string filePath)
    {
        string extension = NormalizeExtension(IOPath.GetExtension(filePath));

        return extension is "mp4" or "mov" or "avi" or "wmv" or "mkv" or "flv" or "webm";
    }

    public static bool IsVideoFormat(string format)
    {
        return format is "mp4" or "mov" or "avi" or "wmv" or "mkv" or "flv" or "webm";
    }

    public static bool IsAudioFormat(string format)
    {
        return format is "mp3" or "aac" or "m4a" or "wav" or "flac" or "ogg" or "opus";
    }

    public static string MapYtDlpVideoFormat(string format)
    {
        return format switch
        {
            "webm" => "webm",
            "mkv" => "mkv",
            _ => "mp4"
        };
    }

    public static string MapYtDlpAudioFormat(string format)
    {
        return format switch
        {
            "aac" => "m4a",
            "ogg" => "vorbis",
            _ => format
        };
    }

    public static string MapSuggestedAudioExtension(string format)
    {
        return format switch
        {
            "aac" => "m4a",
            "ogg" => "ogg",
            _ => format
        };
    }
}
