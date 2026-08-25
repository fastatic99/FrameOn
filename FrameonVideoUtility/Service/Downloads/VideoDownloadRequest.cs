namespace FrameonVideoUtility.Service.Downloads;

public sealed class VideoDownloadRequest
{
    public required string Url { get; init; }
    public required string SelectedFormat { get; init; }
    public required string FinalOutputPath { get; init; }
    public required string RequestedFileName { get; init; }
    public required string FormatSelector { get; init; }
    public bool ForceFormat { get; init; }
}
