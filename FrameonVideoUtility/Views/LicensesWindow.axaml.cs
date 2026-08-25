using System;
using System.IO;
using System.Reflection;
using Avalonia.Controls;

namespace FrameonVideoUtility.Views;

public partial class LicensesWindow : Window
{
    public LicensesWindow()
    {
        InitializeComponent();

        TermsTextBlock.Text = ReadLegalDocument("FrameonVideoUtility.Legal.TERMS_OF_USE.md");
        PrivacyTextBlock.Text = ReadLegalDocument("FrameonVideoUtility.Legal.PRIVACY.md");

        CloseButton.Click += (_, _) => Close();
    }

    private static string ReadLegalDocument(string resourceName)
    {
        Assembly assembly = typeof(LicensesWindow).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded legal document was not found: {resourceName}");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
