using Avalonia.Controls;

namespace FrameonVideoUtility.Views;

public partial class WelcomeWindow : Window
{
    public WelcomeWindow()
    {
        InitializeComponent();

        SkipButton.Click += (_, _) => Close(false);
        CreateShortcutButton.Click += (_, _) => Close(true);
    }
}
