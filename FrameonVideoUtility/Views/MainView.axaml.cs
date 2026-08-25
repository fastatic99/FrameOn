using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FrameonVideoUtility.Service.App;
using FrameonVideoUtility.Service.Logging;
using FrameonVideoUtility.Service.Conversion;
using FrameonVideoUtility.Service.Downloads;
using FrameonVideoUtility.Service.Sandbox;
using FrameonVideoUtility.Service.Tools;
using IOPath = System.IO.Path;

namespace FrameonVideoUtility.Views
{
    public partial class MainView : UserControl
    {
        private const string PublicRepositoryUrl = "https://github.com/fastatic99/FrameOn/releases";
        private const string WebsiteUrl = "https://frameon.choatehome.com";
        private static readonly TimeSpan ToolStartupOverlayDelay = TimeSpan.FromMilliseconds(700);
        private static readonly TimeSpan UpdateButtonRefreshInterval = TimeSpan.FromMinutes(20);

        private readonly ToolPreparationService _toolPreparationService = new();
        private readonly VideoDownloadService _videoDownloadService = new();
        private readonly AudioConversionService _audioConversionService = new();
        private readonly VideoConversionService _videoConversionService = new();

        private DebugWindow? _debugWindow;
        private LicensesWindow? _licensesWindow;
        private WikiWindow? _wikiWindow;

        private string? _selectedAudioVideoFilePath;
        private string? _selectedVideoFilePath;
        private string? _lastDownloadedFilePath;
        private string? _lastAudioConvertedFilePath;
        private string? _lastVideoConvertedFilePath;

        private CancellationTokenSource? _downloadCancellationTokenSource;
        private CancellationTokenSource? _audioConvertCancellationTokenSource;
        private CancellationTokenSource? _videoConvertCancellationTokenSource;
        private CancellationTokenSource? _toolStartupOverlayDelayCancellationTokenSource;
        private int _lastLoggedYtDlpProgress = -1;
        private DateTime _lastLoggedFfmpegProgressUtc = DateTime.MinValue;
        private DateTime _lastLoggedYtDlpProcessLineUtc = DateTime.MinValue;
        private DateTime _lastLoggedFfmpegProcessLineUtc = DateTime.MinValue;
        private bool _isAppLocked = false;

        private enum DownloadFormatMode
        {
            Standard,
            Compatible,
            BestQuality
        }

        private sealed record DownloadFormatSelection(
            string Extension,
            DownloadFormatMode Mode,
            string DisplayName);


        public MainView()
        {
            InitializeComponent();
            WireUiEvents();

            AppLog.LogReceived += OnLogReceived;
            AppLog.Info("FrameOn UI initialized.");
        }

        private async Task PrepareSandboxToolsAsync()
        {
            _toolStartupOverlayDelayCancellationTokenSource?.Cancel();
            _toolStartupOverlayDelayCancellationTokenSource?.Dispose();
            _toolStartupOverlayDelayCancellationTokenSource = new CancellationTokenSource();

            Task overlayDelayTask = ShowToolStartupOverlayAfterDelayAsync(_toolStartupOverlayDelayCancellationTokenSource.Token);

            try
            {
                SetToolStartupStatus("FrameOn is getting tools ready...");
                AppLog.Info($"[Sandbox] Preparing tools for {PlatformToolNames.CurrentThirdPartyPlatformFolderName}.");

                await Task.Run(() =>
                {
                    _toolPreparationService.GetPreparedSandboxYtDlpPath();
                    _toolPreparationService.GetPreparedSandboxFfmpegPath();
                });

                AppLog.Info("[Sandbox] Tools ready.");
            }
            catch (Exception ex)
            {
                AppLog.Warning($"[Sandbox] Tool preparation failed: {ex.Message}");
            }
            finally
            {
                _toolStartupOverlayDelayCancellationTokenSource.Cancel();

                try
                {
                    await overlayDelayTask;
                }
                catch (OperationCanceledException)
                {
                }

                _toolStartupOverlayDelayCancellationTokenSource.Dispose();
                _toolStartupOverlayDelayCancellationTokenSource = null;

                SetToolStartupStatus("");
                HideToolStartupOverlay();
            }
        }

        private async Task ShowToolStartupOverlayAfterDelayAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(ToolStartupOverlayDelay, cancellationToken);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        ToolsLoadingOverlay.IsVisible = true;
                    }
                });
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void HideToolStartupOverlay()
        {
            Dispatcher.UIThread.Post(() =>
            {
                ToolsLoadingOverlay.IsVisible = false;
            });
        }

        private void SetToolStartupStatus(string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ToolStartupStatusTextBlock.Text = message;
                ToolStartupStatusTextBlock.IsVisible = !string.IsNullOrWhiteSpace(message);
            });
        }

        private void WireUiEvents()
        {
            AdvancedDownloadOptionsToggleButton.IsChecked = false;
            SettingsButton.Click += (_, _) => OpenSettingsPanel();
            AdvancedDownloadOptionsToggleButton.IsCheckedChanged += (_, _) => UpdateAdvancedDownloadOptionsMode();
            EmbeddedSettingsPanel.CloseRequested += (_, _) => HideSettingsPanel();
            OpenWikiButton.Click += (_, _) => OpenWikiWindow();
            OpenWebsiteButton.Click += (_, _) => OpenWebsite();
            OpenRepositoryButton.Click += (_, _) => OpenPublicRepository();
            OpenLicensesButton.Click += (_, _) => OpenLicensesWindow();
            QualityComboBox.SelectionChanged += (_, _) => UpdateDownloadFormatHelp();
            FormatComboBox.SelectionChanged += (_, _) =>
            {
                UpdateDownloadFormatHelp();
            };

            OpenDebugWindowMenuItem.Click += (_, _) => OpenDebugWindow();
            SaveLogsMenuItem.Click += async (_, _) => await SaveLogsAsync();

            SelectConvertFileButton.Click += async (_, _) => await BrowseAudioVideoFileAsync();
            SelectVideoConvertFileButton.Click += async (_, _) => await BrowseVideoFileAsync();

            DownloadVideoButton.Click += async (_, _) => await DownloadVideoAsync();
            CancelDownloadButton.Click += (_, _) => CancelDownload();

            ConvertAudioButton.Click += async (_, _) => await ConvertAudioAsync();
            CancelAudioConvertButton.Click += (_, _) => CancelAudioConversion();

            ConvertVideoFormatButton.Click += async (_, _) => await ConvertVideoAsync();
            CancelVideoConvertButton.Click += (_, _) => CancelVideoConversion();

            ShowDownloadedFileButton.Click += (_, _) => ShowFile(_lastDownloadedFilePath);
            ShowAudioConvertedFileButton.Click += (_, _) => ShowFile(_lastAudioConvertedFilePath);
            ShowVideoConvertedFileButton.Click += (_, _) => ShowFile(_lastVideoConvertedFilePath);

            CancelDownloadButton.IsEnabled = false;
            CancelAudioConvertButton.IsEnabled = false;
            CancelVideoConvertButton.IsEnabled = false;

            HideCompletedActions();
            PopulateDownloadFormatOptions();
            UpdateAdvancedDownloadOptionsMode();
            UpdateDownloadFormatHelp();
        }

        private void CancelActiveOperationsForAppLock()
        {
            if (_downloadCancellationTokenSource is not null)
            {
                _downloadCancellationTokenSource.Cancel();
                AppLog.Warning("[Access] Active download cancelled because FrameOn is locked.");
            }

            if (_audioConvertCancellationTokenSource is not null)
            {
                _audioConvertCancellationTokenSource.Cancel();
                AppLog.Warning("[Access] Active audio conversion cancelled because FrameOn is locked.");
            }

            if (_videoConvertCancellationTokenSource is not null)
            {
                _videoConvertCancellationTokenSource.Cancel();
                AppLog.Warning("[Access] Active video conversion cancelled because FrameOn is locked.");
            }
        }

        private bool CanUseApp(TextBlock errorTextBlock)
        {
            if (!_isAppLocked)
            {
                return true;
            }

            string message = "FrameOn is not available right now.";
            ShowInlineError(errorTextBlock, string.IsNullOrWhiteSpace(message)
                ? "FrameOn is not available right now."
                : message);

            return false;
        }

        private bool CanUseAppForDownload()
        {
            if (!_isAppLocked)
            {
                return true;
            }

            string message = "FrameOn is not available right now.";
            ShowDownloadError(string.IsNullOrWhiteSpace(message)
                ? "FrameOn is not available right now."
                : message);

            return false;
        }

        private void OnLogReceived(string logLine)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_debugWindow is { IsVisible: true })
                {
                    _debugWindow.AddLog(logLine);
                }
            });
        }

        private void OpenSettingsPanel()
        {
            if (SettingsOverlay.IsVisible)
            {
                return;
            }

            SettingsOverlay.IsVisible = true;
            AppLog.Info("Settings panel opened.");
        }

        private void HideSettingsPanel()
        {
            SettingsOverlay.IsVisible = false;
            PopulateDownloadFormatOptions();
            UpdateDownloadFormatHelp();
            AppLog.Info("Settings panel closed.");
        }

        private void OpenWikiWindow()
        {
            if (_wikiWindow is { IsVisible: true })
            {
                _wikiWindow.Activate();
                return;
            }

            _wikiWindow = new WikiWindow();
            _wikiWindow.Closed += (_, _) => _wikiWindow = null;

            ShowCenteredOverMainWindow(_wikiWindow);

            AppLog.Info("Wiki window opened.");
        }

        private void OpenPublicRepository()
        {
            OpenExternalUrl(PublicRepositoryUrl, "GitHub repo");
        }

        private void OpenWebsite()
        {
            OpenExternalUrl(WebsiteUrl, "FrameOn website");
        }

        private static void OpenExternalUrl(string url, string description)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AppLog.Warning($"Could not open {description}: {ex.Message}");
            }
        }

        private void OpenLicensesWindow()
        {
            if (_licensesWindow is { IsVisible: true })
            {
                _licensesWindow.Activate();
                return;
            }

            _licensesWindow = new LicensesWindow();
            _licensesWindow.Closed += (_, _) => _licensesWindow = null;

            ShowCenteredOverMainWindow(_licensesWindow);

            AppLog.Info("Licenses window opened.");
        }


        private void OpenDebugWindow()
        {
            if (_debugWindow is { IsVisible: true })
            {
                _debugWindow.Activate();
                return;
            }

            _debugWindow = new DebugWindow();
            _debugWindow.Closed += (_, _) => _debugWindow = null;

            _debugWindow.AddLog(AppLog.GetLogText());
            ShowCenteredOverMainWindow(_debugWindow);

            AppLog.Info("Debug window opened.");
        }

        private async Task SaveLogsAsync()
        {
            var topLevel = GetTopLevel();

            if (topLevel is null)
            {
                AppLog.Error("Unable to save logs because no top-level window was found.");
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save FrameOn Logs",
                SuggestedFileName = $"FrameOn-Logs-{DateTime.Now:yyyyMMdd-HHmmss}.txt",
                DefaultExtension = "txt",
                FileTypeChoices =
                [
                    new FilePickerFileType("Text Logs")
                    {
                        Patterns = ["*.txt", "*.log"]
                    },
                    FilePickerFileTypes.All
                ]
            });

            if (file is null)
            {
                AppLog.Info("Save logs cancelled.");
                return;
            }

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, Encoding.UTF8);

            await writer.WriteAsync(AppLog.GetLogText());

            AppLog.Info($"Logs saved to: {file.Name}");
        }

        private void UpdateAdvancedDownloadOptionsMode()
        {
            bool advancedEnabled = AdvancedDownloadOptionsToggleButton.IsChecked == true;
            AdvancedDownloadOptionsPanel.IsVisible = advancedEnabled;
            SimpleDownloadQualityBadge.IsVisible = !advancedEnabled;
            AppSettingsService.SetAdvancedFormatInspectionEnabled(advancedEnabled);

            if (!advancedEnabled)
            {
                QualityComboBox.SelectedIndex = 0;
                ForceFormatCheckBox.IsChecked = false;
            }

            PopulateDownloadFormatOptions();
            UpdateDownloadLayoutMode();
            UpdateDownloadFormatHelp();
        }

        private void UpdateDownloadLayoutMode()
        {
            bool expandedModeEnabled = AdvancedDownloadOptionsToggleButton.IsChecked == true;
            Grid.SetRow(DownloadActionsPanel, expandedModeEnabled ? 4 : 2);
            Grid.SetRow(DownloadDestinationTextBlock, expandedModeEnabled ? 5 : 3);
            Grid.SetRow(DownloadFormatHelpTextBlock, expandedModeEnabled ? 6 : 4);
            Grid.SetRow(DownloadErrorTextBlock, expandedModeEnabled ? 7 : 5);
            Grid.SetRow(DownloadStatusPanel, expandedModeEnabled ? 8 : 7);
        }



        private async Task BrowseAudioVideoFileAsync()
        {
            if (!CanUseApp(AudioConvertErrorTextBlock))
                return;
                var file = await PickSingleFileAsync(
                    "Select a video or audio file",
                    "Audio / Video Files",
                    [
                        "*.mp4", "*.mov", "*.avi", "*.wmv", "*.mkv", "*.flv", "*.webm",
                        "*.mp3", "*.aac", "*.m4a", "*.wav", "*.flac", "*.ogg", "*.opus"
                    ]);

                if (file is null)
                {
                    AppLog.Info("Audio/video file browse cancelled.");
                    return;
                }
            _selectedAudioVideoFilePath = file.Path.LocalPath;
            SelectedConvertFileNameTextBlock.Text = IOPath.GetFileName(_selectedAudioVideoFilePath);

            AppLog.Info($"Selected audio/video file: {_selectedAudioVideoFilePath}");
        }

        private async Task BrowseVideoFileAsync()
        {
            if (!CanUseApp(VideoConvertErrorTextBlock))
                return;
                var file = await PickSingleFileAsync(
                    "Select a video file",
                    "Video Files",
                    ["*.mp4", "*.mov", "*.avi", "*.wmv", "*.mkv", "*.flv", "*.webm"]);

                if (file is null)
                {
                    AppLog.Info("Video file browse cancelled.");
                    return;
                }
            _selectedVideoFilePath = file.Path.LocalPath;
            SelectedVideoConvertFileNameTextBlock.Text = IOPath.GetFileName(_selectedVideoFilePath);

            AppLog.Info($"Selected video file: {_selectedVideoFilePath}");
        }

        private async Task DownloadVideoAsync()
        {
            ClearDownloadError();
            ClearDownloadCompletionActions();

            if (!CanUseAppForDownload())
                return;

            if (!TryValidateVideoUrl(VideoUrlTextBox.Text, out string url, out string validationError))
            {
                ShowDownloadError(validationError);
                AppLog.Warning($"Download validation failed: {validationError}");
                return;
            }

            VideoUrlTextBox.Text = url;

            DownloadFormatSelection? downloadFormat = GetDownloadFormatSelection();

            if (downloadFormat is null)
            {
                ShowDownloadError("Please select a valid output format.");
                AppLog.Warning("Download requested, but no valid output format was selected.");
                return;
            }

            string selectedFormat = downloadFormat.Extension;

            ShowStatus(DownloadStatusPanel, DownloadStatusTextBlock, "Preparing tools...");

            SuggestedDownloadTitleResult titleResult = await _videoDownloadService.GetSuggestedDownloadTitleAsync(
                url,
                selectedFormat
            );

            if (!titleResult.Success)
            {
                SetStatusText(DownloadStatusTextBlock, "Download failed.");
                ShowDownloadError(titleResult.ErrorMessage);

                AppLog.Warning($"[yt-dlp] Download preflight failed: {titleResult.ErrorMessage}");
                return;
            }

            if (false &&
                MediaFormatMapper.IsVideoFormat(selectedFormat))
            {
                SetStatusText(DownloadStatusTextBlock, "Inspecting available video formats...");

                VideoFormatInspectionResult inspectionResult = await _videoDownloadService.InspectAvailableFormatsAsync(
                    url,
                    CancellationToken.None
                );

                if (inspectionResult.Success)
                {
                    SetStatusText(DownloadStatusTextBlock, inspectionResult.Summary);
                    AppLog.Info($"[yt-dlp] Format inspection summary: {inspectionResult.Summary}");
                }
                else
                {
                    SetStatusText(DownloadStatusTextBlock, "Could not inspect available formats. You can still continue with the selected format.");
                    AppLog.Warning($"[yt-dlp] Format inspection skipped: {inspectionResult.ErrorMessage}");
                }

                await WaitForAdvancedInspectionContinueAsync();
            }

            string finalOutputPath;
                var outputFile = await PickOutputFileAsync(
                    "Save downloaded file as",
                    titleResult.SuggestedFileName,
                    selectedFormat
                );

                if (outputFile is null)
                {
                    SetStatusText(DownloadStatusTextBlock, "Download cancelled.");
                    AppLog.Info("Download cancelled because no output file was selected.");
                    return;
                }

                finalOutputPath = await Dispatcher.UIThread.InvokeAsync(() => outputFile.Path.LocalPath);

            try
            {
                SetDownloadButtonsForActiveOperation(true);
                _lastLoggedYtDlpProgress = -1;
                _lastLoggedYtDlpProcessLineUtc = DateTime.MinValue;

                _downloadCancellationTokenSource?.Dispose();
                _downloadCancellationTokenSource = new CancellationTokenSource();

                SetStatusText(DownloadStatusTextBlock, "Preparing tools...");

                string requestedFileName = IOPath.GetFileNameWithoutExtension(finalOutputPath);

                var request = new VideoDownloadRequest
                {
                    Url = url,
                    SelectedFormat = selectedFormat,
                    FinalOutputPath = finalOutputPath,
                    RequestedFileName = requestedFileName,
                    ForceFormat = IsForceFormatEnabled(),
                    FormatSelector = BuildYtDlpFormatSelector(downloadFormat, GetSelectedDownloadQualityText())
                };

                AppLog.Info(
                    $"[yt-dlp] Download mode: Quality={GetSelectedDownloadQualityText()}; " +
                    $"Format={downloadFormat.DisplayName}; Output={downloadFormat.Extension}; " +
                    $"Selector={request.FormatSelector}");

                SetStatusText(DownloadStatusTextBlock, $"Downloading {downloadFormat.DisplayName}...");

                VideoDownloadResult result = await _videoDownloadService.DownloadAsync(
                    request,
                    HandleYtDlpOutput,
                    HandleYtDlpError,
                    _downloadCancellationTokenSource.Token
                );

                if (result.WasCanceled)
                {
                    SetStatusText(DownloadStatusTextBlock, "Download cancelled.");
                    return;
                }

                if (!result.Success)
                {
                    SetStatusText(DownloadStatusTextBlock, "Download failed.");
                    ShowDownloadError(result.ErrorMessage);
                    return;
                }

                SetStatusText(DownloadStatusTextBlock, "Download completed.");
                _lastDownloadedFilePath = finalOutputPath;
                ShowDownloadCompletedActions();
            }
            finally
            {
                SetDownloadButtonsForActiveOperation(false);

                _downloadCancellationTokenSource?.Dispose();
                _downloadCancellationTokenSource = null;
            }
        }


        private static string GetUniqueOutputPath(string folderPath, string fileName)
        {
            string safeFileName = MediaFormatMapper.SanitizeFileName(fileName);
            string extension = IOPath.GetExtension(safeFileName);
            string baseName = IOPath.GetFileNameWithoutExtension(safeFileName);
            string candidate = IOPath.Combine(folderPath, safeFileName);
            int suffix = 2;

            while (File.Exists(candidate))
            {
                candidate = IOPath.Combine(folderPath, $"{baseName} ({suffix}){extension}");
                suffix++;
            }

            return candidate;
        }

        private async Task ConvertAudioAsync()
        {
            ClearInlineError(AudioConvertErrorTextBlock);
            ClearAudioConvertCompletionActions();

            if (!CanUseApp(AudioConvertErrorTextBlock))
                return;

            if (string.IsNullOrWhiteSpace(_selectedAudioVideoFilePath))
            {
                ShowInlineError(AudioConvertErrorTextBlock, "Please select a video or audio file.");
                AppLog.Warning("Audio/video conversion requested, but no file was selected.");
                return;
            }

            if (!File.Exists(_selectedAudioVideoFilePath))
            {
                ShowInlineError(AudioConvertErrorTextBlock, "The selected file no longer exists.");
                AppLog.Error($"Selected file no longer exists: {_selectedAudioVideoFilePath}");
                return;
            }

            if (!MediaFormatMapper.IsSupportedAudioVideoInputFile(_selectedAudioVideoFilePath))
            {
                ShowInlineError(AudioConvertErrorTextBlock, "Please select a supported video or audio file.");
                AppLog.Warning($"Unsupported audio/video conversion input: {_selectedAudioVideoFilePath}");
                return;
            }

            string outputFolderPath;
            var outputFolder = await PickOutputFolderAsync("Select audio output folder");

                if (outputFolder is null)
                {
                    AppLog.Info("Audio conversion cancelled because no output folder was selected.");
                    return;
                }

                outputFolderPath = await Dispatcher.UIThread.InvokeAsync(() => outputFolder.Path.LocalPath);

            ShowStatus(AudioConvertStatusPanel, AudioConvertStatusTextBlock, "Preparing file...");

            string outputFormat = MediaFormatMapper.NormalizeExtension(GetSelectedComboBoxText(AudioOutputCodecComboBox));

            if (string.IsNullOrWhiteSpace(outputFormat))
            {
                SetStatusText(AudioConvertStatusTextBlock, "Audio conversion failed.");
                ShowInlineError(AudioConvertErrorTextBlock, "Please select a valid audio output format.");
                AppLog.Warning("Audio conversion requested, but no output format was selected.");
                return;
            }

            try
            {
                SetAudioConvertButtonsForActiveOperation(true);
                _lastLoggedFfmpegProgressUtc = DateTime.MinValue;
                _lastLoggedFfmpegProcessLineUtc = DateTime.MinValue;

                _audioConvertCancellationTokenSource?.Dispose();
                _audioConvertCancellationTokenSource = new CancellationTokenSource();

                SetStatusText(AudioConvertStatusTextBlock, "Preparing tools...");
                SetStatusText(AudioConvertStatusTextBlock, "Converting audio...");

                SandboxedProcessResult result = await _audioConversionService.ConvertAsync(
                    _selectedAudioVideoFilePath,
                    outputFolderPath,
                    outputFormat,
                    HandleFfmpegOutput,
                    HandleFfmpegError,
                    _audioConvertCancellationTokenSource.Token
                );

                if (result.WasCanceled)
                {
                    SetStatusText(AudioConvertStatusTextBlock, "Audio conversion cancelled.");
                    AppLog.Warning("[ffmpeg] Audio conversion cancelled by user.");
                    return;
                }

                if (result.ExitCode != 0)
                {
                    SetStatusText(AudioConvertStatusTextBlock, "Audio conversion failed.");
                    ShowInlineError(AudioConvertErrorTextBlock, "Audio conversion failed. Check the selected file and try again.");
                    AppLog.Error("[ffmpeg] Audio conversion failed. Check debug logs.");
                    return;
                }

                SetStatusText(AudioConvertStatusTextBlock, "Audio conversion completed.");
                _lastAudioConvertedFilePath = result.OutputFiles.Length > 0
                    ? result.OutputFiles[0]
                    : null;

                if (!string.IsNullOrWhiteSpace(_lastAudioConvertedFilePath))
                {
                    ShowCompletedActions(AudioConvertCompletedActionsPanel);
                }

                AppLog.Info("[ffmpeg] Audio conversion completed successfully.");
            }
            catch (Exception ex)
            {
                SetStatusText(AudioConvertStatusTextBlock, "Audio conversion failed.");
                ShowInlineError(AudioConvertErrorTextBlock, "Audio conversion failed. Check the selected file and try again.");

                AppLog.Error($"[ffmpeg] Audio conversion failed: {ex.Message}");
            }
            finally
            {
                SetAudioConvertButtonsForActiveOperation(false);

                _audioConvertCancellationTokenSource?.Dispose();
                _audioConvertCancellationTokenSource = null;
            }
        }

        private async Task ConvertVideoAsync()
        {
            ClearInlineError(VideoConvertErrorTextBlock);
            ClearVideoConvertCompletionActions();

            if (!CanUseApp(VideoConvertErrorTextBlock))
                return;

            if (string.IsNullOrWhiteSpace(_selectedVideoFilePath))
            {
                ShowInlineError(VideoConvertErrorTextBlock, "Please select a video file.");
                AppLog.Warning("Video conversion requested, but no video file was selected.");
                return;
            }

            if (!File.Exists(_selectedVideoFilePath))
            {
                ShowInlineError(VideoConvertErrorTextBlock, "The selected video file no longer exists.");
                AppLog.Error($"Selected video file no longer exists: {_selectedVideoFilePath}");
                return;
            }

            if (!MediaFormatMapper.IsSupportedVideoInputFile(_selectedVideoFilePath))
            {
                ShowInlineError(VideoConvertErrorTextBlock, "Please select a supported video file.");
                AppLog.Warning($"Unsupported video conversion input: {_selectedVideoFilePath}");
                return;
            }

            string outputFolderPath;
            try
            {
                var outputFolder = await PickOutputFolderAsync("Select video output folder");

                if (outputFolder is null)
                {
                    AppLog.Info("Video conversion cancelled because no output folder was selected.");
                    return;
                }

                outputFolderPath = outputFolder.Path.LocalPath;
            }
            catch (Exception ex)
            {
                ShowInlineError(VideoConvertErrorTextBlock, "The selected output folder could not be opened.");
                AppLog.Error($"Video output folder selection failed: {ex}");
                return;
            }

            ShowStatus(VideoConvertStatusPanel, VideoConvertStatusTextBlock, "Preparing video...");

            string outputFormat = MediaFormatMapper.NormalizeExtension(GetSelectedComboBoxText(VideoOutputFormatComboBox));

            if (string.IsNullOrWhiteSpace(outputFormat))
            {
                SetStatusText(VideoConvertStatusTextBlock, "Video conversion failed.");
                ShowInlineError(VideoConvertErrorTextBlock, "Please select a valid video output format.");
                AppLog.Warning("Video conversion requested, but no output format was selected.");
                return;
            }

            try
            {
                SetVideoConvertButtonsForActiveOperation(true);
                _lastLoggedFfmpegProgressUtc = DateTime.MinValue;
                _lastLoggedFfmpegProcessLineUtc = DateTime.MinValue;

                _videoConvertCancellationTokenSource?.Dispose();
                _videoConvertCancellationTokenSource = new CancellationTokenSource();

                SetStatusText(VideoConvertStatusTextBlock, "Preparing tools...");
                SetStatusText(VideoConvertStatusTextBlock, "Converting video...");

                SandboxedProcessResult result = await _videoConversionService.ConvertAsync(
                    _selectedVideoFilePath,
                    outputFolderPath,
                    outputFormat,
                    HandleFfmpegOutput,
                    HandleFfmpegError,
                    _videoConvertCancellationTokenSource.Token
                );

                if (result.WasCanceled)
                {
                    SetStatusText(VideoConvertStatusTextBlock, "Video conversion cancelled.");
                    AppLog.Warning("[ffmpeg] Video conversion cancelled by user.");
                    return;
                }

                if (result.ExitCode != 0)
                {
                    SetStatusText(VideoConvertStatusTextBlock, "Video conversion failed.");
                    ShowInlineError(VideoConvertErrorTextBlock, "Video conversion failed. Check the selected file and try again.");
                    AppLog.Error("[ffmpeg] Video conversion failed. Check debug logs.");
                    return;
                }

                SetStatusText(VideoConvertStatusTextBlock, "Video conversion completed.");
                _lastVideoConvertedFilePath = result.OutputFiles.Length > 0
                    ? result.OutputFiles[0]
                    : null;

                if (!string.IsNullOrWhiteSpace(_lastVideoConvertedFilePath))
                {
                    ShowCompletedActions(VideoConvertCompletedActionsPanel);
                }

                AppLog.Info("[ffmpeg] Video conversion completed successfully.");
            }
            catch (Exception ex)
            {
                SetStatusText(VideoConvertStatusTextBlock, "Video conversion failed.");
                ShowInlineError(VideoConvertErrorTextBlock, "Video conversion failed. Check the selected file and try again.");

                AppLog.Error($"[ffmpeg] Video conversion failed: {ex.Message}");
            }
            finally
            {
                SetVideoConvertButtonsForActiveOperation(false);

                _videoConvertCancellationTokenSource?.Dispose();
                _videoConvertCancellationTokenSource = null;
            }
        }

        private void CancelDownload()
        {
            if (_downloadCancellationTokenSource is null)
            {
                AppLog.Info("[yt-dlp] Cancel requested, but no download is active.");
                return;
            }

            _downloadCancellationTokenSource.Cancel();
            AppLog.Warning("[yt-dlp] Cancel requested by user.");
        }

        private void CancelAudioConversion()
        {
            if (_audioConvertCancellationTokenSource is null)
            {
                AppLog.Info("[ffmpeg] Cancel requested, but no audio conversion is active.");
                return;
            }

            _audioConvertCancellationTokenSource.Cancel();
            AppLog.Warning("[ffmpeg] Audio conversion cancel requested by user.");
        }

        private void CancelVideoConversion()
        {
            if (_videoConvertCancellationTokenSource is null)
            {
                AppLog.Info("[ffmpeg] Cancel requested, but no video conversion is active.");
                return;
            }

            _videoConvertCancellationTokenSource.Cancel();
            AppLog.Warning("[ffmpeg] Video conversion cancel requested by user.");
        }

        public static void CleanupSandbox()
        {
            SandboxCleanupService.CleanupSandbox();
        }

        private async Task<IStorageFile?> PickSingleFileAsync(
            string title,
            string fileTypeName,
            string[] patterns)
        {
            var topLevel = GetTopLevel();

            if (topLevel is null)
            {
                AppLog.Error($"Unable to open file picker: {title}");
                return null;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType(fileTypeName)
                    {
                        Patterns = patterns
                    },
                    FilePickerFileTypes.All
                ]
            });

            return files.Count > 0 ? files[0] : null;
        }

        private async Task<IStorageFolder?> PickOutputFolderAsync(string title)
        {
            var topLevel = GetTopLevel();

            if (topLevel is null)
            {
                AppLog.Error($"Unable to open folder picker: {title}");
                return null;
            }

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            });

            return folders.Count > 0 ? folders[0] : null;
        }

        private async Task<IStorageFile?> PickOutputFileAsync(
            string title,
            string suggestedFileName,
            string extension)
        {
            var topLevel = GetTopLevel();

            if (topLevel is null)
            {
                AppLog.Error($"Unable to open save picker: {title}");
                return null;
            }

            extension = MediaFormatMapper.NormalizeExtension(extension);

            return await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = suggestedFileName,
                DefaultExtension = extension,
                FileTypeChoices =
                [
                    new FilePickerFileType($"{extension.ToUpperInvariant()} File")
                    {
                        Patterns = [$"*.{extension}"]
                    },
                    FilePickerFileTypes.All
                ]
            });
        }

        private void PopulateDownloadFormatOptions()
        {
            FormatComboBox.Items.Clear();
            AddFormatOption(FormatComboBox, "MP4 (Recommended)", new DownloadFormatSelection("mp4", DownloadFormatMode.Compatible, "MP4 (Recommended)"));
            AddFormatOption(FormatComboBox, "WebM", new DownloadFormatSelection("webm", DownloadFormatMode.BestQuality, "WebM"));
            AddFormatOption(FormatComboBox, "MKV", new DownloadFormatSelection("mkv", DownloadFormatMode.BestQuality, "MKV"));
            AddFormatOption(FormatComboBox, "MP3", new DownloadFormatSelection("mp3", DownloadFormatMode.Standard, "MP3"));
            FormatComboBox.SelectedIndex = 0;
        }


        private static void AddFormatHeader(ComboBox comboBox, string text)
        {
            comboBox.Items.Add(new ComboBoxItem
            {
                Classes = { "combo-header" },
                Content = $"── {text} ──",
                IsEnabled = false
            });
        }

        private void AddFormatOption(string text, DownloadFormatSelection selection)
        {
            AddFormatOption(FormatComboBox, text, selection);
        }

        private static void AddFormatOption(ComboBox comboBox, string text, DownloadFormatSelection selection)
        {
            comboBox.Items.Add(new ComboBoxItem
            {
                Content = text,
                Tag = selection
            });
        }

        private ComboBox GetActiveDownloadFormatComboBox()
        {
            return FormatComboBox;
        }


        private int FindFormatOptionIndex(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return -1;
            }

            for (int index = 0; index < FormatComboBox.Items.Count; index++)
            {
                if (FormatComboBox.Items[index] is ComboBoxItem item &&
                    item.Tag is DownloadFormatSelection &&
                    string.Equals(item.Content?.ToString(), displayName, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private DownloadFormatSelection? GetDownloadFormatSelection()
        {
            if (AdvancedDownloadOptionsToggleButton.IsChecked != true &&
                string.Equals(GetSelectedDownloadQualityText(), "Highest", StringComparison.OrdinalIgnoreCase))
            {
                return new DownloadFormatSelection("mkv", DownloadFormatMode.BestQuality, "Highest available");
            }

            ComboBox activeFormatComboBox = GetActiveDownloadFormatComboBox();

            if (activeFormatComboBox.SelectedItem is ComboBoxItem { Tag: DownloadFormatSelection selection })
            {
                return selection;
            }

            string displayName = GetSelectedComboBoxText(activeFormatComboBox).Trim();

            if (string.IsNullOrWhiteSpace(displayName) || displayName.Contains("──"))
            {
                return null;
            }

            return BuildStandardDownloadFormatSelection(displayName);
        }

        private static DownloadFormatSelection? BuildStandardDownloadFormatSelection(string displayName)
        {
            string extension = MediaFormatMapper.NormalizeExtension(displayName);

            if (string.IsNullOrWhiteSpace(extension) ||
                extension.Contains("video", StringComparison.OrdinalIgnoreCase) ||
                extension.Contains("audio", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new DownloadFormatSelection(extension, DownloadFormatMode.Standard, displayName);
        }

        private string BuildYtDlpFormatSelector(DownloadFormatSelection selection, string quality)
        {
            if (MediaFormatMapper.IsAudioFormat(selection.Extension))
            {
                return "bestaudio/best";
            }

            int? maxHeight = QualityToHeight(quality);

            string heightFilter = maxHeight.HasValue
                ? $"[height<={maxHeight.Value}]"
                : "";

            if (string.Equals(selection.Extension, "mp4", StringComparison.OrdinalIgnoreCase) &&
                selection.Mode == DownloadFormatMode.Compatible)
            {
                return
                    $"bv*[vcodec^=avc1][ext=mp4]{heightFilter}+ba[ext=m4a]/" +
                    $"b[vcodec^=avc1][ext=mp4]{heightFilter}/" +
                    $"best[vcodec^=avc1][ext=mp4]{heightFilter}/" +
                    $"best[ext=mp4]{heightFilter}/18";
            }

            if (string.Equals(selection.Extension, "mp4", StringComparison.OrdinalIgnoreCase) &&
                selection.Mode == DownloadFormatMode.BestQuality)
            {
                return
                    $"bv*[ext=mp4]{heightFilter}+ba[ext=m4a]/" +
                    $"b[ext=mp4]{heightFilter}/" +
                    $"bv*{heightFilter}+ba[ext=m4a]/" +
                    $"bestvideo{heightFilter}+bestaudio/" +
                    $"best{heightFilter}";
            }

            if (string.Equals(selection.Extension, "webm", StringComparison.OrdinalIgnoreCase) &&
                selection.Mode == DownloadFormatMode.BestQuality)
            {
                return
                    $"bv*[ext=webm]{heightFilter}+ba[ext=webm]/" +
                    $"b[ext=webm]{heightFilter}/" +
                    $"bestvideo{heightFilter}+bestaudio/" +
                    $"best{heightFilter}";
            }

            if (string.Equals(selection.Extension, "mkv", StringComparison.OrdinalIgnoreCase) &&
                selection.Mode == DownloadFormatMode.BestQuality)
            {
                return
                    $"bv*{heightFilter}+ba/" +
                    $"bestvideo{heightFilter}+bestaudio/" +
                    $"best{heightFilter}";
            }

            return $"bv*{heightFilter}+ba/b{heightFilter}/best";
        }

        private void UpdateDownloadFormatHelp()
        {
            if (AdvancedDownloadOptionsToggleButton.IsChecked != true)
            {
                DownloadFormatHelpTextBlock.Text = "Auto chooses the recommended quality and file format. Turn on Advanced to choose the quality and file format.";
                return;
            }

            string displayName = GetSelectedComboBoxText(GetActiveDownloadFormatComboBox()).Trim();
            string quality = GetSelectedDownloadQualityText();
            string qualityPrefix = string.Equals(quality, "Recommended", StringComparison.OrdinalIgnoreCase)
                ? ""
                : quality + " is requested when available. ";

            DownloadFormatHelpTextBlock.Text = displayName switch
            {
                "MP4 (Recommended)" => qualityPrefix + "Recommended for most users. Best chance of playing in QuickTime, Windows media apps, browsers, and common players.",
                "MP4 (High Quality)" => qualityPrefix + "Keeps an MP4 file while allowing newer source codecs when available. Some older players may not open every file.",
                "WebM" => qualityPrefix + "Good for browsers and VLC. Less friendly to QuickTime.",
                "MKV" => qualityPrefix + "Flexible container for high-quality video. Best with VLC or similar players.",
                _ when MediaFormatMapper.IsAudioFormat(MediaFormatMapper.NormalizeExtension(displayName)) => "Audio formats extract the audio track only.",
                _ => qualityPrefix + "Uses the selected output container when possible. Compatibility depends on the source video codecs."
            };
        }


        private string GetSelectedDownloadQualityText()
        {
            return AdvancedDownloadOptionsToggleButton.IsChecked == true
                ? GetSelectedComboBoxText(QualityComboBox).Trim()
                : "Recommended";
        }


        private bool IsForceFormatEnabled()
        {
            return AdvancedDownloadOptionsToggleButton.IsChecked == true && ForceFormatCheckBox.IsChecked == true;
        }

        private static int? QualityToHeight(string quality)
        {
            return quality switch
            {
                "8K (4320p)" => 4320,
                "4320p" => 4320,
                "4K (2160p)" => 2160,
                "2160p" => 2160,
                "1440p" => 1440,
                "1080p" => 1080,
                "720p" => 720,
                _ => null
            };
        }

        private static bool TryValidateVideoUrl(
            string? input,
            out string normalizedUrl,
            out string errorMessage)
        {
            normalizedUrl = "";
            errorMessage = "";

            if (string.IsNullOrWhiteSpace(input))
            {
                errorMessage = "Please enter a video link.";
                return false;
            }

            input = input.Trim();

            if (!input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Video links must start with http:// or https://.";
                return false;
            }

            if (input.Any(char.IsWhiteSpace))
            {
                errorMessage = "Video links cannot contain spaces.";
                return false;
            }

            if (!Uri.TryCreate(input, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                errorMessage = "Please enter a valid video URL.";
                return false;
            }

            if (!uri.IsDefaultPort || !string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                errorMessage = "Video links with custom ports or credentials are not allowed.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(uri.Host) ||
                uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                !uri.Host.Contains('.', StringComparison.Ordinal))
            {
                errorMessage = "Please enter a complete public video URL.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(uri.AbsolutePath) || uri.AbsolutePath == "/")
            {
                errorMessage = "The video link is missing a video path.";
                return false;
            }

            normalizedUrl = VideoUrlNormalizer.Normalize(uri);
            return true;
        }

        private void HandleYtDlpOutput(string line)
        {
            if (TryUpdateProgressFromPercentLine("yt-dlp", line))
            {
                return;
            }

            if (!ShouldLogProcessLine(ref _lastLoggedYtDlpProcessLineUtc, line))
            {
                return;
            }

            AppLog.Info($"[yt-dlp] {line}");
        }

        private void HandleYtDlpError(string line)
        {
            if (TryUpdateProgressFromPercentLine("yt-dlp", line))
            {
                return;
            }

            if (!ShouldLogProcessLine(ref _lastLoggedYtDlpProcessLineUtc, line))
            {
                return;
            }

            AppLog.Error($"[yt-dlp] {line}");
        }

        private void HandleFfmpegOutput(string line)
        {
            if (TryUpdateProgressFromFfmpegLine(line))
            {
                return;
            }

            if (!ShouldLogProcessLine(ref _lastLoggedFfmpegProcessLineUtc, line))
            {
                return;
            }

            AppLog.Info($"[ffmpeg] {line}");
        }

        private void HandleFfmpegError(string line)
        {
            if (TryUpdateProgressFromFfmpegLine(line))
            {
                return;
            }

            if (!ShouldLogProcessLine(ref _lastLoggedFfmpegProcessLineUtc, line))
            {
                return;
            }

            AppLog.Info($"[ffmpeg] {line}");
        }

        private static void ShowStatus(StackPanel panel, TextBlock textBlock, string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                textBlock.Text = message;
                panel.IsVisible = true;
            });
        }

        private static void SetStatusText(TextBlock textBlock, string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                textBlock.Text = message;
            });
        }

        private void ClearDownloadCompletionActions()
        {
            _lastDownloadedFilePath = null;
            Dispatcher.UIThread.Post(() =>
            {
                ContinueDownloadAfterInspectionButton.IsVisible = false;
                ShowDownloadedFileButton.IsVisible = false;
                DownloadCompletedActionsPanel.IsVisible = false;
            });
        }


        private async Task WaitForAdvancedInspectionContinueAsync()
        {
            var completion = new TaskCompletionSource<bool>();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                void ContinueDownload(object? sender, Avalonia.Interactivity.RoutedEventArgs args)
                {
                    ContinueDownloadAfterInspectionButton.Click -= ContinueDownload;
                    ContinueDownloadAfterInspectionButton.IsVisible = false;
                    DownloadCompletedActionsPanel.IsVisible = false;
                    completion.TrySetResult(true);
                }

                ShowDownloadedFileButton.IsVisible = false;
                ContinueDownloadAfterInspectionButton.IsVisible = true;
                DownloadCompletedActionsPanel.IsVisible = true;
                ContinueDownloadAfterInspectionButton.Click += ContinueDownload;
            });

            await completion.Task;
        }

        private void ShowDownloadCompletedActions()
        {
            Dispatcher.UIThread.Post(() =>
            {
                ContinueDownloadAfterInspectionButton.IsVisible = false;
                ShowDownloadedFileButton.IsVisible = true;
                DownloadCompletedActionsPanel.IsVisible = true;
            });
        }

        private void ClearAudioConvertCompletionActions()
        {
            _lastAudioConvertedFilePath = null;
            HideCompletedActions(AudioConvertCompletedActionsPanel);
        }

        private void ClearVideoConvertCompletionActions()
        {
            _lastVideoConvertedFilePath = null;
            HideCompletedActions(VideoConvertCompletedActionsPanel);
        }

        private void HideCompletedActions()
        {
            HideCompletedActions(DownloadCompletedActionsPanel);
            HideCompletedActions(AudioConvertCompletedActionsPanel);
            HideCompletedActions(VideoConvertCompletedActionsPanel);
        }

        private static void HideCompletedActions(StackPanel panel)
        {
            Dispatcher.UIThread.Post(() =>
            {
                panel.IsVisible = false;
            });
        }

        private static void ShowCompletedActions(StackPanel panel)
        {
            Dispatcher.UIThread.Post(() =>
            {
                panel.IsVisible = true;
            });
        }

        private static void ShowFile(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                AppLog.Warning($"[Shell] Cannot show file because it no longer exists: {filePath}");
                return;
            }

            try
            {
                if (OperatingSystem.IsMacOS())
                {
                    StartProcess("open", "-R", filePath);
                    return;
                }

                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{filePath}\"",
                        UseShellExecute = true
                    });
                    return;
                }

                string? folderPath = IOPath.GetDirectoryName(filePath);

                if (!string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath))
                {
                    StartProcess("xdg-open", folderPath);
                }
            }
            catch (Exception ex)
            {
                AppLog.Warning($"[Shell] Could not show file: {ex.Message}");
            }
        }

        private static void StartProcess(string fileName, params string[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false
            };

            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            Process.Start(startInfo);
        }

        private static void ShowInlineError(TextBlock textBlock, string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                textBlock.Text = message;
                textBlock.IsVisible = true;
            });
        }

        private static void ClearInlineError(TextBlock textBlock)
        {
            Dispatcher.UIThread.Post(() =>
            {
                textBlock.Text = "";
                textBlock.IsVisible = false;
            });
        }

        private void ShowDownloadError(string message)
        {
            ShowInlineError(DownloadErrorTextBlock, message);
        }

        private void ClearDownloadError()
        {
            ClearInlineError(DownloadErrorTextBlock);
        }

        private void SetDownloadButtonsForActiveOperation(bool isActive)
        {
            DownloadVideoButton.IsEnabled = !_isAppLocked && !isActive;
            CancelDownloadButton.IsEnabled = !_isAppLocked && isActive;
        }

        private void SetAudioConvertButtonsForActiveOperation(bool isActive)
        {
            ConvertAudioButton.IsEnabled = !_isAppLocked && !isActive;
            CancelAudioConvertButton.IsEnabled = !_isAppLocked && isActive;
        }

        private void SetVideoConvertButtonsForActiveOperation(bool isActive)
        {
            ConvertVideoFormatButton.IsEnabled = !_isAppLocked && !isActive;
            CancelVideoConvertButton.IsEnabled = !_isAppLocked && isActive;
        }

        private static string GetSelectedComboBoxText(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString() ?? "";
            }

            return comboBox.SelectedItem?.ToString() ?? "";
        }

        private static void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AppLog.Error($"Could not open link: {ex.Message}");
            }
        }

        private TopLevel? GetTopLevel()
        {
            return TopLevel.GetTopLevel(this);
        }

        private void ShowCenteredOverMainWindow(Window window)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (GetOwningWindow() is { } owner)
            {
                window.Show(owner);
                return;
            }

            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
        }

        private Window? GetOwningWindow()
        {
            if (TopLevel.GetTopLevel(this) is Window owner)
            {
                return owner;
            }

            return Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
        }

        private bool TryUpdateProgressFromPercentLine(string source, string line)
        {
            Match match = Regex.Match(line, @"(\d{1,3}(?:\.\d+)?)%");

            if (!match.Success)
            {
                return false;
            }

            if (!double.TryParse(match.Groups[1].Value, out double percent))
            {
                return false;
            }

            int rounded = Math.Clamp((int)Math.Round(percent), 0, 100);

            if (rounded == _lastLoggedYtDlpProgress)
            {
                return true;
            }

            _lastLoggedYtDlpProgress = rounded;
            AppLog.Info($"[{source}] Progress: {rounded}%");
            return true;
        }

        private bool TryUpdateProgressFromFfmpegLine(string line)
        {
            if (!line.Contains("time=", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Match match = Regex.Match(line, @"time=(\d{2}:\d{2}:\d{2}\.\d{2})");

            if (!match.Success)
            {
                return false;
            }

            DateTime now = DateTime.UtcNow;

            if (now - _lastLoggedFfmpegProgressUtc < TimeSpan.FromSeconds(1))
            {
                return true;
            }

            _lastLoggedFfmpegProgressUtc = now;
            AppLog.Info($"[ffmpeg] Processed time: {match.Groups[1].Value}");
            return true;
        }

        private static bool ShouldLogProcessLine(ref DateTime lastLoggedUtc, string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            if (IsImportantProcessLine(line))
            {
                return true;
            }

            DateTime now = DateTime.UtcNow;

            if (now - lastLoggedUtc < TimeSpan.FromSeconds(4))
            {
                return false;
            }

            lastLoggedUtc = now;
            return true;
        }

        private static bool IsImportantProcessLine(string line)
        {
            return line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                   line.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
                   line.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                   line.Contains("destination", StringComparison.OrdinalIgnoreCase) ||
                   line.Contains("merging", StringComparison.OrdinalIgnoreCase) ||
                   line.Contains("deleting original file", StringComparison.OrdinalIgnoreCase);
        }
    }
}
