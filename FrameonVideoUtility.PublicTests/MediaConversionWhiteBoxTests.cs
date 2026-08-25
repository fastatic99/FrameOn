using FrameonVideoUtility.Service.Conversion;
using Xunit;

namespace FrameonVideoUtility.PublicTests;

public sealed class MediaConversionWhiteBoxTests
{
    [Theory]
    [InlineData("mp4", "libx264", "aac")]
    [InlineData("mkv", "libx264", "aac")]
    [InlineData("webm", "libvpx-vp9", "libopus")]
    public void PublicVideoFormatsBuildExpectedFfmpegCommands(
        string format,
        string videoCodec,
        string audioCodec)
    {
        List<string> arguments = FfmpegArgumentBuilder.BuildVideoArguments(
            "/fixtures/input.mov",
            $"/output/converted.{format}",
            format,
            2);

        Assert.Contains("-i", arguments);
        Assert.Contains(videoCodec, arguments);
        Assert.Contains(audioCodec, arguments);
        Assert.Contains("-threads", arguments);
        Assert.Equal($"/output/converted.{format}", arguments[^1]);
    }

    [Theory]
    [InlineData("mp3", "libmp3lame")]
    [InlineData("wav", "pcm_s16le")]
    public void PublicAudioFormatsBuildExpectedFfmpegCommands(string format, string codec)
    {
        List<string> arguments = FfmpegArgumentBuilder.BuildAudioArguments(
            "/fixtures/input.mp4",
            $"/output/converted.{format}",
            format,
            null);

        Assert.Contains("-vn", arguments);
        Assert.Contains(codec, arguments);
        Assert.Equal($"/output/converted.{format}", arguments[^1]);
    }

    [Theory]
    [InlineData("clip.mov", "mp4", "clip.mp4")]
    [InlineData("clip.MP4", ".WEBM", "clip.webm")]
    public void OutputNamesUseTheInputStemAndNormalizedExtension(
        string input,
        string format,
        string expected)
    {
        Assert.Equal(expected, MediaFormatMapper.BuildSafeOutputFileName(input, format));
    }
}
