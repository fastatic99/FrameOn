using Avalonia.Controls;

namespace FrameonVideoUtility.Views;

public partial class WikiWindow : Window
{
    public WikiWindow()
    {
        InitializeComponent();

        CloseButton.Click += (_, _) => Close();
    }
}
