using System.Runtime.InteropServices;

namespace FrameonVideoUtility.Service.Tools;

public static class PlatformToolNames
{
    public static string CurrentThirdPartyPlatformFolderName =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "macOS";

    public static string YtDlpFileName =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "yt-dlp.exe" : "yt-dlp";

    public static string DenoFileName =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "deno.exe" : "deno";

    public static string FfmpegFileName =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";

    public static string FfprobeFileName =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffprobe.exe" : "ffprobe";
}
