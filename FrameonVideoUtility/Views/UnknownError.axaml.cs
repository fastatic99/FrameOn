using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace FrameonVideoUtility.Views;

public partial class UnknownError : Window
{
    public UnknownError()
    {
        InitializeComponent();

        ExitButton.Click += (_, _) => ExitApplication();
    }

    public UnknownError(string title, string message, string details = "")
    {
        InitializeComponent();

        TitleText.Text = title;
        MessageText.Text = message;
        DetailsText.Text = string.IsNullOrWhiteSpace(details)
            ? "FrameOn ran into an unexpected problem."
            : details;

        ExitButton.Click += (_, _) => ExitApplication();
    }

    private static void ExitApplication()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
            return;
        }

        Environment.Exit(0);
    }
}