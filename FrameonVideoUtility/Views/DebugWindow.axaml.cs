using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FrameonVideoUtility.Views;

public partial class DebugWindow : Window
{
    private TextBox? _debugLogTextBox;

    public DebugWindow()
    {
        InitializeComponent();
        _debugLogTextBox = this.FindControl<TextBox>("DebugLogTextBox");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void AddLog(string message)
    {
        if (_debugLogTextBox == null)
            return;

        _debugLogTextBox.Text += message + "\n";
        _debugLogTextBox.CaretIndex = _debugLogTextBox.Text?.Length ?? 0;
    }
}