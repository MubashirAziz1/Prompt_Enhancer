using Microsoft.UI.Xaml.Controls;

namespace PromptEnhancer.WinUI;

public sealed partial class SettingsDialog : ContentDialog
{
    private readonly ICredentialStore credentials;
    public bool KeySaved { get; private set; }

    public SettingsDialog(ICredentialStore credentials)
    {
        this.credentials = credentials;
        InitializeComponent();
        try
        {
            if (!string.IsNullOrWhiteSpace(credentials.Load()))
                CredentialStatus.Text = "A protected key is saved. Enter a key to replace it.";
        }
        catch { CredentialStatus.Text = "Saved key unavailable. Enter a key to replace it."; }
        Closed += (_, _) => ApiKey.Password = "";
    }

    private void SaveKey(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(ApiKey.Password)) return;
        string key = ApiKey.Password.Trim();
        if (key.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
        {
            CredentialStatus.Text = "Enter a key without spaces or line breaks.";
            args.Cancel = true;
            return;
        }
        try
        {
            credentials.Save(key);
            ApiKey.Password = "";
            KeySaved = true;
        }
        catch
        {
            CredentialStatus.Text = "The API key could not be saved with Windows protection. Try again.";
            args.Cancel = true;
        }
    }
}
