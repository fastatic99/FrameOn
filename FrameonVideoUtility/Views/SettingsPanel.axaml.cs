using System;
using Avalonia.Controls;
using FrameonVideoUtility.Service.App;

namespace FrameonVideoUtility.Views;

public partial class SettingsPanel : UserControl
{
    public event EventHandler? CloseRequested;

    public SettingsPanel()
    {
        InitializeComponent();

        LoadPerformanceSettings();

        CloseSettingsPanelButton.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
        ConversionPerformanceComboBox.SelectionChanged += (_, _) => SavePerformanceSettings();
    }

    private void LoadPerformanceSettings()
    {
        ConversionPerformanceComboBox.Items.Clear();

        foreach (ConversionPerformanceMode mode in Enum.GetValues<ConversionPerformanceMode>())
        {
            ConversionPerformanceComboBox.Items.Add(new ComboBoxItem
            {
                Content = AppSettingsService.GetPerformanceModeDisplayName(mode),
                Tag = mode
            });
        }

        ConversionPerformanceComboBox.SelectedIndex = (int)AppSettingsService.Settings.ConversionPerformanceMode;
        SetPerformanceHelpText(AppSettingsService.Settings.ConversionPerformanceMode);
    }

    private void SavePerformanceSettings()
    {
        if (ConversionPerformanceComboBox.SelectedItem is not ComboBoxItem item ||
            item.Tag is not ConversionPerformanceMode mode)
        {
            return;
        }

        AppSettingsService.SetConversionPerformanceMode(mode);
        SetPerformanceHelpText(mode);
    }

    private void SetPerformanceHelpText(ConversionPerformanceMode mode)
    {
        ConversionPerformanceHelpText.Text = mode == ConversionPerformanceMode.Auto
            ? "Lets ffmpeg choose the thread count for this computer."
            : "Limits ffmpeg CPU use during audio and video conversion so the app stays more responsive.";
    }
}
