using System;
using System.Reflection;
using Avalonia.Controls;

namespace FrameonVideoUtility.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        VersionTextBlock.Text = "Community Source Edition";

        CloseButton.Click += (_, _) => Close();
    }

    private static string GetCurrentVersionText()
    {
        string? version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(version))
        {
            int metadataIndex = version.IndexOf('+', StringComparison.Ordinal);
            return metadataIndex > 0
                ? version[..metadataIndex]
                : version;
        }

        Version? fallbackVersion = Assembly.GetEntryAssembly()?.GetName().Version
            ?? Assembly.GetExecutingAssembly().GetName().Version;

        return fallbackVersion is null
            ? "Unknown"
            : $"{fallbackVersion.Major}.{fallbackVersion.Minor}.{fallbackVersion.Build}";
    }
}
