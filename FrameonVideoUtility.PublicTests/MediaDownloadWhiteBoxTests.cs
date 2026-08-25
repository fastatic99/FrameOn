using FrameonVideoUtility.Service.Downloads;
using Xunit;

namespace FrameonVideoUtility.PublicTests;

public sealed class MediaDownloadWhiteBoxTests
{
    [Theory]
    [InlineData("mp4")]
    [InlineData("mkv")]
    [InlineData("webm")]
    public void PublicVideoDownloadFormatsProduceARestrictedYtDlpCommand(string format)
    {
        List<string> arguments = YtDlpArgumentBuilder.BuildDownloadArguments(
            "https://example.invalid/media",
            null,
            "/tools",
            "/downloads",
            "public-test",
            format,
            false,
            "bestvideo+bestaudio/best",
            null);

        Assert.Contains("--no-playlist", arguments);
        Assert.Contains("--merge-output-format", arguments);
        Assert.Contains(format, arguments);
        Assert.DoesNotContain("--cookies-from-browser", arguments);
        Assert.Equal("https://example.invalid/media", arguments[^1]);
    }

    [Theory]
    [InlineData("ERROR: Unsupported URL", "supported video URL")]
    [InlineData("network connection timed out", "could not be reached")]
    [InlineData("This video is private", "private")]
    public void DownloadFailuresAreMappedWithoutContactingThirdPartySites(
        string rawError,
        string expectedText)
    {
        string message = YtDlpErrorMapper.GetFriendlyError(rawError);
        Assert.Contains(expectedText, message, StringComparison.OrdinalIgnoreCase);
    }
}
