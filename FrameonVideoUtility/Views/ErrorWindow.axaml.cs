using Avalonia.Controls;

namespace FrameonVideoUtility.Views;

public partial class ErrorWindow : Window
{
    public ErrorWindow()
    {
        InitializeComponent();
        WireButtonEvents();
    }

    public ErrorWindow(string title, string message, string details = "")
    {
        InitializeComponent();

        TitleText.Text = title;
        MessageText.Text = message;
        DetailsText.Text = string.IsNullOrWhiteSpace(details)
            ? ""
            : details;

        WireButtonEvents();
    }

    private void WireButtonEvents()
    {
        RetryButton.Click += (_, _) => Close("Retry");
        ExitButton.Click += (_, _) => Close("Exit");
    }
}