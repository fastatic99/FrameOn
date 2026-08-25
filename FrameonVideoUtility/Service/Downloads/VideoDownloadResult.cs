namespace FrameonVideoUtility.Service.Downloads;

public sealed class VideoDownloadResult
{
    public bool Success { get; init; }
    public bool WasCanceled { get; init; }
    public string ErrorMessage { get; init; } = "";

    public static VideoDownloadResult Ok()
    {
        return new VideoDownloadResult { Success = true };
    }

    public static VideoDownloadResult Canceled()
    {
        return new VideoDownloadResult { WasCanceled = true };
    }

    public static VideoDownloadResult Fail(string errorMessage)
    {
        return new VideoDownloadResult { ErrorMessage = errorMessage };
    }
}
