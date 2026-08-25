using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FrameonVideoUtility.Service.App;
using FrameonVideoUtility.Service.Logging;
using FrameonVideoUtility.Service.Conversion;
using FrameonVideoUtility.Service.Sandbox;
using FrameonVideoUtility.Service.Tools;
using IOPath = System.IO.Path;

namespace FrameonVideoUtility.Service.Downloads;

public sealed class VideoDownloadService
{
    private readonly ToolPreparationService _toolPreparationService;

    public VideoDownloadService()
        : this(new ToolPreparationService())
    {
    }

    public VideoDownloadService(ToolPreparationService toolPreparationService)
    {
        _toolPreparationService = toolPreparationService;
    }

    public async Task<SuggestedDownloadTitleResult> GetSuggestedDownloadTitleAsync(
        string url,
        string selectedFormat)
    {
        string suggestedExtension = MediaFormatMapper.IsAudioFormat(selectedFormat)
            ? MediaFormatMapper.MapSuggestedAudioExtension(selectedFormat)
            : selectedFormat;

        try
        {
            AppLog.Info("[yt-dlp] Getting video title for suggested file name.");

            string ytDlpPath = await Task.Run(_toolPreparationService.GetPreparedSandboxYtDlpPath);
            string? denoPath = await Task.Run(_toolPreparationService.TryGetPreparedDenoPath);
            string workingDirectory = IOPath.GetDirectoryName(ytDlpPath)!;

            string jobId = SandboxPathService.CreateJobId("title-lookup");
            string sandboxOutputFolder = SandboxPathService.GetSandboxJobFolder(jobId, "Output");
            string sandboxTempFolder = SandboxPathService.GetSandboxJobFolder(jobId, "Temp");

            using var titleLookupCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            string? title = null;
            var titleLookupErrorBuilder = new StringBuilder();
            var runner = SandboxedProcessRunnerFactory.Create();

            SandboxedProcessResult result = await runner.RunAsync(
                ytDlpPath,
                YtDlpArgumentBuilder.BuildTitleLookupArguments(
                    url,
                    denoPath
                ),
                workingDirectory,
                sandboxOutputFolder,
                sandboxTempFolder,
                line =>
                {
                    if (title is null && IsLikelyVideoTitleLine(line))
                    {
                        title = line.Trim();
                    }
                },
                line =>
                {
                    titleLookupErrorBuilder.AppendLine(line);
                    AppLog.Warning($"[yt-dlp] Title lookup warning: {line}");
                },
                titleLookupCancellation.Token
            );

            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(title))
            {
                string rawError = titleLookupErrorBuilder.ToString();
                string friendlyError = YtDlpErrorMapper.GetFriendlyError(rawError);

                AppLog.Warning($"[yt-dlp] Could not validate video URL. {friendlyError}");
                AppLog.Warning($"[yt-dlp] Raw validation error: {rawError}");

                return SuggestedDownloadTitleResult.Fail(friendlyError);
            }

            string safeTitle = MediaFormatMapper.SanitizeFileName(title);

            AppLog.Info($"[yt-dlp] Suggested title: {safeTitle}");

            return SuggestedDownloadTitleResult.Ok($"{safeTitle}.{suggestedExtension}");
        }
        catch (Exception ex)
        {
            AppLog.Warning($"[yt-dlp] Title lookup failed: {ex.Message}");

            return SuggestedDownloadTitleResult.Fail(
                "The video could not be checked. Check the link and try again."
            );
        }
    }


    public async Task<VideoFormatInspectionResult> InspectAvailableFormatsAsync(
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            AppLog.Info("[yt-dlp] Inspecting available source formats.");

            string ytDlpPath = await Task.Run(_toolPreparationService.GetPreparedSandboxYtDlpPath, cancellationToken);
            string? denoPath = await Task.Run(_toolPreparationService.TryGetPreparedDenoPath, cancellationToken);
            string workingDirectory = IOPath.GetDirectoryName(ytDlpPath)!;

            string jobId = SandboxPathService.CreateJobId("format-inspection");
            string sandboxOutputFolder = SandboxPathService.GetSandboxJobFolder(jobId, "Output");
            string sandboxTempFolder = SandboxPathService.GetSandboxJobFolder(jobId, "Temp");

            var formatLines = new List<string>();
            var errorBuilder = new StringBuilder();
            var runner = SandboxedProcessRunnerFactory.Create();

            using var inspectionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            inspectionCancellation.CancelAfter(TimeSpan.FromSeconds(30));

            SandboxedProcessResult result = await runner.RunAsync(
                ytDlpPath,
                YtDlpArgumentBuilder.BuildFormatInspectionArguments(
                    url,
                    denoPath
                ),
                workingDirectory,
                sandboxOutputFolder,
                sandboxTempFolder,
                line => formatLines.Add(line),
                line =>
                {
                    errorBuilder.AppendLine(line);
                    formatLines.Add(line);
                },
                inspectionCancellation.Token
            );

            if (result.WasCanceled)
            {
                return VideoFormatInspectionResult.Fail("Format inspection timed out or was cancelled.");
            }

            if (result.ExitCode != 0)
            {
                string rawError = errorBuilder.ToString();
                string friendlyError = YtDlpErrorMapper.GetFriendlyError(rawError);

                AppLog.Warning($"[yt-dlp] Format inspection failed: {friendlyError}");

                return VideoFormatInspectionResult.Fail(friendlyError);
            }

            return VideoFormatInspectionResult.Ok(BuildFormatInspectionSummary(formatLines));
        }
        catch (Exception ex)
        {
            AppLog.Warning($"[yt-dlp] Format inspection failed: {ex.Message}");
            return VideoFormatInspectionResult.Fail("Available formats could not be inspected for this video.");
        }
    }

    public async Task<VideoDownloadResult> DownloadAsync(
        VideoDownloadRequest request,
        Action<string> onOutput,
        Action<string> onError,
        CancellationToken cancellationToken)
    {
        try
        {
            string ytDlpPath = await Task.Run(_toolPreparationService.GetPreparedSandboxYtDlpPath);
            string? denoPath = await Task.Run(_toolPreparationService.TryGetPreparedDenoPath);
            string workingDirectory = IOPath.GetDirectoryName(ytDlpPath)!;

            string jobId = SandboxPathService.CreateJobId("download");
            string sandboxDownloadFolder = SandboxPathService.GetSandboxJobFolder(jobId, "Output");
            string sandboxTempFolder = SandboxPathService.GetSandboxJobFolder(jobId, "Temp");
            int? ffmpegThreadCount = AppSettingsService.GetFfmpegThreadCount();

            await Task.Run(() => SandboxPathService.ClearFolder(sandboxDownloadFolder), cancellationToken);

            var arguments = YtDlpArgumentBuilder.BuildDownloadArguments(
                request.Url,
                denoPath,
                workingDirectory,
                sandboxDownloadFolder,
                request.RequestedFileName,
                request.SelectedFormat,
                request.ForceFormat,
                request.FormatSelector,
                ffmpegThreadCount
            );

            var ytDlpErrorBuilder = new StringBuilder();

            AppLog.Info("[yt-dlp] Download started.");
            AppLog.Info($"[yt-dlp] Source URL: {request.Url}");
            AppLog.Info($"[yt-dlp] Final output: {request.FinalOutputPath}");
            AppLog.Info($"[yt-dlp] ffmpeg post-processing threads: {(ffmpegThreadCount.HasValue ? ffmpegThreadCount.Value.ToString() : "auto")}");

            var runner = SandboxedProcessRunnerFactory.Create();

            SandboxedProcessResult result = await runner.RunAsync(
                ytDlpPath,
                arguments,
                workingDirectory,
                sandboxDownloadFolder,
                sandboxTempFolder,
                onOutput,
                line =>
                {
                    ytDlpErrorBuilder.AppendLine(line);
                    onError(line);
                },
                cancellationToken
            );

            if (result.WasCanceled)
            {
                AppLog.Warning("[yt-dlp] Download cancelled by user.");
                return VideoDownloadResult.Canceled();
            }

            if (result.ExitCode != 0)
            {
                string rawError = ytDlpErrorBuilder.ToString();
                string friendlyError = YtDlpErrorMapper.GetFriendlyError(rawError);

                AppLog.Error($"[yt-dlp] Download failed: {friendlyError}");
                AppLog.Error($"[yt-dlp] Raw error: {rawError}");

                return VideoDownloadResult.Fail(friendlyError);
            }

            await Task.Run(() =>
            {
                SandboxPathService.MoveSingleSandboxDownloadToFinalPath(
                    sandboxDownloadFolder,
                    request.FinalOutputPath
                );
            }, cancellationToken);

            AppLog.Info("[yt-dlp] Download completed successfully.");
            return VideoDownloadResult.Ok();
        }
        catch (Exception ex)
        {
            AppLog.Error($"[yt-dlp] Download failed: {ex.Message}");

            return VideoDownloadResult.Fail(
                "The video could not be downloaded. Check the link and try again."
            );
        }
    }


    private static string BuildFormatInspectionSummary(IEnumerable<string> lines)
    {
        List<AvailableVideoFormat> formats = lines
            .Select(ParseAvailableVideoFormat)
            .Where(format => format is not null)
            .Cast<AvailableVideoFormat>()
            .ToList();

        if (formats.Count == 0)
        {
            return "No video format details were found for this source.";
        }

        AvailableVideoFormat? highest = formats
            .OrderByDescending(format => format.Height)
            .ThenByDescending(format => format.BitrateKbps ?? 0)
            .FirstOrDefault();

        AvailableVideoFormat? mp4Compatible = formats
            .Where(format => string.Equals(format.Extension, "mp4", StringComparison.OrdinalIgnoreCase) &&
                             format.Codec.StartsWith("avc1", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(format => format.Height)
            .ThenByDescending(format => format.BitrateKbps ?? 0)
            .FirstOrDefault();

        AvailableVideoFormat? mp4Modern = formats
            .Where(format => string.Equals(format.Extension, "mp4", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(format => format.Height)
            .ThenByDescending(format => format.BitrateKbps ?? 0)
            .FirstOrDefault();

        AvailableVideoFormat? webmModern = formats
            .Where(format => string.Equals(format.Extension, "webm", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(format => format.Height)
            .ThenByDescending(format => format.BitrateKbps ?? 0)
            .FirstOrDefault();

        var parts = new List<string>
        {
            "This video can be downloaded as:"
        };

        if (highest is not null)
        {
            parts.Add($"- Best picture: {highest.FriendlyQuality}. Select Quality: {GetQualitySelectionLabel(highest)}; File format: {GetFormatSelectionLabel(highest)}.");
        }

        if (mp4Compatible is not null)
        {
            parts.Add($"- Easiest playback: {mp4Compatible.FriendlyQuality} MP4. Select Quality: {GetQualitySelectionLabel(mp4Compatible)}; File format: MP4 (Recommended).");
        }

        if (mp4Modern is not null)
        {
            parts.Add($"- Higher-quality MP4: {mp4Modern.FriendlyQuality}. Select Quality: {GetQualitySelectionLabel(mp4Modern)}; File format: MP4 (High Quality).");
        }

        if (webmModern is not null)
        {
            parts.Add($"- High-quality WebM: {webmModern.FriendlyQuality}. Select Quality: {GetQualitySelectionLabel(webmModern)}; File format: WebM.");
        }

        parts.Add("Tip: MP4 (Recommended) is safest. Higher-quality choices may need VLC or another modern player.");

        return string.Join(Environment.NewLine, parts);
    }


    private static string GetQualitySelectionLabel(AvailableVideoFormat format)
    {
        if (format.Width >= 7680 || format.Height >= 4320)
        {
            return "8K (4320p)";
        }

        if (format.Width >= 3840 || format.Height >= 2160)
        {
            return "4K (2160p)";
        }

        if (format.Width >= 2560 || format.Height >= 1440)
        {
            return "1440p";
        }

        if (format.Width >= 1920 || format.Height >= 1080)
        {
            return "1080p";
        }

        if (format.Width >= 1280 || format.Height >= 720)
        {
            return "720p";
        }

        return "Recommended";
    }

    private static string GetFormatSelectionLabel(AvailableVideoFormat format)
    {
        if (string.Equals(format.Extension, "webm", StringComparison.OrdinalIgnoreCase))
        {
            return "WebM";
        }

        if (string.Equals(format.Extension, "mp4", StringComparison.OrdinalIgnoreCase))
        {
            return format.Codec.StartsWith("avc1", StringComparison.OrdinalIgnoreCase)
                ? "MP4 (Recommended)"
                : "MP4 (High Quality)";
        }

        return format.Extension.ToUpperInvariant();
    }

    private static AvailableVideoFormat? ParseAvailableVideoFormat(string line)
    {
        Match match = Regex.Match(
            line,
            @"^\s*(?<id>\S+)\s+(?<ext>\S+)\s+(?<resolution>\d+x\d+)\s+(?<rest>.*)$"
        );

        if (!match.Success)
        {
            return null;
        }

        string resolution = match.Groups["resolution"].Value;
        string[] resolutionParts = resolution.Split('x');

        if (resolutionParts.Length != 2 ||
            !int.TryParse(resolutionParts[0], out int width) ||
            !int.TryParse(resolutionParts[1], out int height))
        {
            return null;
        }

        string rest = match.Groups["rest"].Value;
        string codec = ExtractCodec(rest);
        int? bitrateKbps = ExtractBitrateKbps(rest);

        return new AvailableVideoFormat(
            match.Groups["id"].Value,
            match.Groups["ext"].Value,
            resolution,
            width,
            height,
            codec,
            bitrateKbps
        );
    }

    private static string ExtractCodec(string text)
    {
        Match match = Regex.Match(text, @"\b(?<codec>avc1\.[^\s]+|av01\.[^\s]+|vp9(?:\.[^\s]+)?|h264|hevc|h265)\b", RegexOptions.IgnoreCase);

        return match.Success ? match.Groups["codec"].Value : "unknown codec";
    }

    private static int? ExtractBitrateKbps(string text)
    {
        Match match = Regex.Match(text, @"(?<bitrate>\d+)k\b");

        return match.Success && int.TryParse(match.Groups["bitrate"].Value, out int bitrate)
            ? bitrate
            : null;
    }

    internal static bool IsLikelyVideoTitleLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        string trimmedLine = line.Trim();

        return !trimmedLine.StartsWith("[job]", StringComparison.OrdinalIgnoreCase)
               && !trimmedLine.StartsWith("WARNING:", StringComparison.OrdinalIgnoreCase)
               && !trimmedLine.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class SuggestedDownloadTitleResult
{
    public bool Success { get; init; }
    public string SuggestedFileName { get; init; } = "";
    public string ErrorMessage { get; init; } = "";

    public static SuggestedDownloadTitleResult Ok(string suggestedFileName)
    {
        return new SuggestedDownloadTitleResult
        {
            Success = true,
            SuggestedFileName = suggestedFileName
        };
    }

    public static SuggestedDownloadTitleResult Fail(string errorMessage)
    {
        return new SuggestedDownloadTitleResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}


public sealed class VideoFormatInspectionResult
{
    public bool Success { get; init; }
    public string Summary { get; init; } = "";
    public string ErrorMessage { get; init; } = "";

    public static VideoFormatInspectionResult Ok(string summary)
    {
        return new VideoFormatInspectionResult
        {
            Success = true,
            Summary = summary
        };
    }

    public static VideoFormatInspectionResult Fail(string errorMessage)
    {
        return new VideoFormatInspectionResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

internal sealed record AvailableVideoFormat(
    string Id,
    string Extension,
    string Resolution,
    int Width,
    int Height,
    string Codec,
    int? BitrateKbps)
{
    public string FriendlyQuality
    {
        get
        {
            string size = $"{Width}x{Height}";

            if (Width >= 7680 || Height >= 4320)
            {
                return $"8K ({size})";
            }

            if (Width >= 3840 || Height >= 2160)
            {
                return $"4K ({size})";
            }

            if (Width >= 2560 || Height >= 1440)
            {
                return $"1440p ({size})";
            }

            if (Width >= 1920 || Height >= 1080)
            {
                return $"1080p ({size})";
            }

            if (Width >= 1280 || Height >= 720)
            {
                return $"720p ({size})";
            }

            return $"{Height}p ({size})";
        }
    }
}
