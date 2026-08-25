using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FrameonVideoUtility.Service.App;
using FrameonVideoUtility.Service.Logging;
using FrameonVideoUtility.Service.Sandbox;
using FrameonVideoUtility.Service.Tools;
using IOPath = System.IO.Path;

namespace FrameonVideoUtility.Service.Conversion;

public sealed class VideoConversionService
{
    private readonly ToolPreparationService _toolPreparationService;

    public VideoConversionService()
        : this(new ToolPreparationService())
    {
    }

    public VideoConversionService(ToolPreparationService toolPreparationService)
    {
        _toolPreparationService = toolPreparationService;
    }

    public async Task<SandboxedProcessResult> ConvertAsync(
        string inputFile,
        string outputFolderPath,
        string outputFormat,
        Action<string> onOutput,
        Action<string> onError,
        CancellationToken cancellationToken)
    {
        string ffmpegPath = await Task.Run(_toolPreparationService.GetPreparedSandboxFfmpegPath);
        string workingDirectory = IOPath.GetDirectoryName(ffmpegPath)!;

        string jobId = SandboxPathService.CreateJobId("video-convert");
        string sandboxOutputFolder = SandboxPathService.GetSandboxJobFolder(jobId, "Output");
        string sandboxTempFolder = SandboxPathService.GetSandboxJobFolder(jobId, "Temp");

        await Task.Run(() => SandboxPathService.ClearFolder(sandboxOutputFolder), cancellationToken);

        string outputFileName = MediaFormatMapper.BuildSafeOutputFileName(inputFile, outputFormat);
        string sandboxOutputFile = IOPath.Combine(sandboxOutputFolder, outputFileName);
        int? threadCount = AppSettingsService.GetFfmpegThreadCount();
        var arguments = FfmpegArgumentBuilder.BuildVideoArguments(inputFile, sandboxOutputFile, outputFormat, threadCount);

        AppLog.Info("[ffmpeg] Video conversion started.");
        AppLog.Info($"[ffmpeg] Input: {inputFile}");
        AppLog.Info($"[ffmpeg] Output format: {outputFormat}");
        AppLog.Info($"[ffmpeg] Threads: {(threadCount.HasValue ? threadCount.Value.ToString() : "auto")}");
        AppLog.Info($"[ffmpeg] Destination folder: {outputFolderPath}");

        var runner = SandboxedProcessRunnerFactory.Create();
        var ffmpegErrorBuilder = new StringBuilder();

        SandboxedProcessResult result = await runner.RunAsync(
            ffmpegPath,
            arguments,
            workingDirectory,
            sandboxOutputFolder,
            sandboxTempFolder,
            onOutput,
            line =>
            {
                ffmpegErrorBuilder.AppendLine(line);
                onError(line);
            },
            cancellationToken
        );

        if (result.ExitCode == 0 && !result.WasCanceled)
        {
            string[] outputFiles = await Task.Run(() =>
            {
                return SandboxPathService.MoveSandboxFilesToOutputFolder(sandboxOutputFolder, outputFolderPath);
            }, cancellationToken);

            return new SandboxedProcessResult
            {
                ExitCode = result.ExitCode,
                WasCanceled = result.WasCanceled,
                OutputFiles = outputFiles
            };
        }

        if (!result.WasCanceled && result.ExitCode != 0)
        {
        }

        return result;
    }
}
