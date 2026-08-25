using System;
using System.IO;
using System.Text.Json;
using FrameonVideoUtility.Service.Logging;
using IOPath = System.IO.Path;

namespace FrameonVideoUtility.Service.App;

public enum ConversionPerformanceMode
{
    Auto,
    LowImpact,
    Balanced,
    HighImpact,
    MaxImpact
}

public sealed class AppSettings
{
    public int SettingsVersion { get; set; }
    public ConversionPerformanceMode ConversionPerformanceMode { get; set; } = ConversionPerformanceMode.Auto;
    public bool AdvancedFormatInspectionEnabled { get; set; }
    public bool FirstLaunchWelcomeCompleted { get; set; }
    public bool DesktopShortcutRequested { get; set; }
}

public static class AppSettingsService
{
    private const int CurrentSettingsVersion = 3;

    private static readonly object SyncRoot = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static AppSettings? _settings;

    public static AppSettings Settings
    {
        get
        {
            lock (SyncRoot)
            {
                _settings ??= LoadSettings();
                return _settings;
            }
        }
    }

    public static int? GetFfmpegThreadCount()
    {
        return GetThreadCount(Settings.ConversionPerformanceMode);
    }

    public static int? GetThreadCount(ConversionPerformanceMode mode)
    {
        int processorCount = Math.Max(1, Environment.ProcessorCount);

        return mode switch
        {
            ConversionPerformanceMode.Auto => null,
            ConversionPerformanceMode.LowImpact => Math.Max(1, processorCount / 4),
            ConversionPerformanceMode.Balanced => Math.Max(1, processorCount / 2),
            ConversionPerformanceMode.HighImpact => Math.Max(1, (int)Math.Ceiling(processorCount * 0.75)),
            ConversionPerformanceMode.MaxImpact => processorCount,
            _ => null
        };
    }

    public static string GetPerformanceModeDisplayName(ConversionPerformanceMode mode)
    {
        int? threadCount = GetThreadCount(mode);

        return mode switch
        {
            ConversionPerformanceMode.Auto => "Auto - ffmpeg auto",
            ConversionPerformanceMode.LowImpact => $"Low Impact - {threadCount} CPU Threads",
            ConversionPerformanceMode.Balanced => $"Balanced - {threadCount} CPU Threads",
            ConversionPerformanceMode.HighImpact => $"High Impact - {threadCount} CPU Threads",
            ConversionPerformanceMode.MaxImpact => $"Max Impact - {threadCount} CPU Threads",
            _ => mode.ToString()
        };
    }

    public static void SetConversionPerformanceMode(ConversionPerformanceMode mode)
    {
        bool changed;

        lock (SyncRoot)
        {
            _settings ??= LoadSettings();
            changed = _settings.ConversionPerformanceMode != mode;

            if (!changed)
            {
                return;
            }

            _settings.ConversionPerformanceMode = mode;
            SaveSettings(_settings);
        }

        AppLog.Info($"[Settings] Conversion performance set to {GetPerformanceModeDisplayName(mode)}.");
    }

    public static void SetAdvancedFormatInspectionEnabled(bool enabled)
    {
        lock (SyncRoot)
        {
            _settings ??= LoadSettings();
            _settings.AdvancedFormatInspectionEnabled = enabled;
        }
    }

    public static void CompleteFirstLaunchWelcome(bool desktopShortcutRequested)
    {
        lock (SyncRoot)
        {
            _settings ??= LoadSettings();
            _settings.FirstLaunchWelcomeCompleted = true;
            _settings.DesktopShortcutRequested = desktopShortcutRequested;
            SaveSettings(_settings);
        }
    }

    public static void ResetFirstLaunchWelcomeForDiagnostics()
    {
        lock (SyncRoot)
        {
            _settings ??= LoadSettings();
            _settings.FirstLaunchWelcomeCompleted = false;
            _settings.DesktopShortcutRequested = false;
            SaveSettings(_settings);
        }
    }

    private static AppSettings LoadSettings()
    {
        try
        {
            string path = GetSettingsPath();

            if (!File.Exists(path))
            {
                return CreateDefaultSettings();
            }

            string json = File.ReadAllText(path);
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return settings ?? CreateDefaultSettings();
        }
        catch (Exception ex)
        {
            AppLog.Warning($"[Settings] Could not load settings: {ex.Message}");
            return CreateDefaultSettings();
        }
    }

    private static AppSettings CreateDefaultSettings()
    {
        return new AppSettings
        {
            SettingsVersion = CurrentSettingsVersion,
            ConversionPerformanceMode = ConversionPerformanceMode.Auto,
            AdvancedFormatInspectionEnabled = false,
            FirstLaunchWelcomeCompleted = false,
            DesktopShortcutRequested = false
        };
    }

    private static void SaveSettings(AppSettings settings)
    {
        try
        {
            string path = GetSettingsPath();
            Directory.CreateDirectory(IOPath.GetDirectoryName(path)!);

            settings.SettingsVersion = CurrentSettingsVersion;
            settings.AdvancedFormatInspectionEnabled = false;

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            AppLog.Warning($"[Settings] Could not save settings: {ex.Message}");
        }
    }

    private static string GetSettingsPath()
    {
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (string.IsNullOrWhiteSpace(appDataFolder))
        {
            appDataFolder = IOPath.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config"
            );
        }

        return IOPath.Combine(appDataFolder, "FrameOn", "settings.json");
    }
}
