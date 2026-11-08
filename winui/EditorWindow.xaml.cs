using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;

namespace PromptEnhancer.WinUI;

public sealed partial class EditorWindow : Window
{
    private readonly IEnhancementProvider provider;
    private readonly ICredentialStore credentials;
    private readonly CancellationTokenSource lifetime = new();
    private bool busy;
    private bool closed;
    private SettingsDialog activeSettings;

    public EditorWindow(IEnhancementProvider provider, ICredentialStore credentials)
    {
        this.provider = provider;
        this.credentials = credentials;
        InitializeComponent();
        Root.ActualThemeChanged += (_, _) => UpdateWindowTheme();
        UpdateWindowTheme();
        Root.Loaded += (_, _) =>
        {
            double scale = Root.XamlRoot.RasterizationScale;
            var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
            AppWindow.Resize(new SizeInt32(Math.Min((int)(780 * scale), area.Width), Math.Min((int)(850 * scale), area.Height)));
            try
            {
                if (!string.IsNullOrWhiteSpace(credentials.Load())) Status.Text = "Ready. Saved API key available.";
            }
            catch { Status.Text = "The saved API key could not be read. Open Settings to replace it."; }
            RoughPrompt.Focus(FocusState.Programmatic);
        };
        Closed += (_, _) =>
        {
            closed = true;
            lifetime.Cancel();
            lifetime.Dispose();
        };
    }

    private void ResultChanging(TextBox sender, TextBoxTextChangingEventArgs args) => UpdateActions();

    private void UpdateWindowTheme()
    {
        AppWindow.TitleBar.PreferredTheme = Root.ActualTheme == ElementTheme.Dark ? TitleBarTheme.Dark : TitleBarTheme.Light;
        if (activeSettings != null) activeSettings.RequestedTheme = Root.ActualTheme;
    }

    private async void OpenSettings(object sender, RoutedEventArgs args)
    {
        if (busy || closed) return;
        SettingsButton.IsEnabled = false;
        var dialog = new SettingsDialog(credentials) { XamlRoot = Root.XamlRoot, RequestedTheme = Root.ActualTheme };
        activeSettings = dialog;
        try
        {
            if (await dialog.ShowAsync() == ContentDialogResult.Primary && dialog.KeySaved && !closed)
                Status.Text = "API key saved with Windows protection. Ready.";
        }
        catch { if (!closed) Status.Text = "Settings could not open. Try again."; }
        finally { activeSettings = null; if (!closed) SettingsButton.IsEnabled = true; }
    }

    private void UpdateActions()
    {
        if (CopyButton != null) CopyButton.IsEnabled = !busy && !string.IsNullOrWhiteSpace(EnhancedPrompt.Text);
    }

    private async void Enhance(object sender, RoutedEventArgs args)
    {
        if (busy || closed) return;
        if (string.IsNullOrWhiteSpace(RoughPrompt.Text)) { Status.Text = "Enter a rough prompt first."; return; }
        busy = true;
        EnhanceButton.IsEnabled = SettingsButton.IsEnabled = RoughPrompt.IsEnabled = PasteButton.IsEnabled = EnhancedPrompt.IsEnabled = false;
        EnhanceButton.Content = "Enhance";
        EnhancedPrompt.Text = "";
        UpdateActions();
        Progress.Visibility = Visibility.Visible;
        Status.Text = "Enhancing…";
        try
        {
            string result = await provider.EnhanceAsync(RoughPrompt.Text, EnhancementPolicy.Instructions, lifetime.Token);
            if (closed) return;
            if (string.IsNullOrWhiteSpace(result)) throw new EnhancementException(EnhancementFailure.InvalidResponse);
            EnhancedPrompt.Text = result;
            Status.Text = "Enhanced prompt ready. Review, edit, then Copy.";
        }
        catch (EnhancementException error) { ShowFailure(error.Message); }
        catch (CredentialException error) { ShowFailure(error.Message); }
        catch { ShowFailure("Enhancement could not finish. Try again."); }
        finally
        {
            if (!closed)
            {
                busy = false;
                EnhanceButton.IsEnabled = SettingsButton.IsEnabled = RoughPrompt.IsEnabled = PasteButton.IsEnabled = EnhancedPrompt.IsEnabled = true;
                Progress.Visibility = Visibility.Collapsed;
                UpdateActions();
            }
        }
    }

    private void ShowFailure(string message)
    {
        if (closed) return;
        EnhancedPrompt.Text = "";
        Status.Text = "Enhancement failed. " + message;
        EnhanceButton.Content = "Retry";
    }

    private async void Paste(object sender, RoutedEventArgs args)
    {
        if (busy || closed) return;
        // Clipboard access starts only with this explicit user action.
        try
        {
            var content = Clipboard.GetContent();
            if (!content.Contains(StandardDataFormats.Text)) { Status.Text = "The clipboard has no text to paste."; return; }
            string text = await content.GetTextAsync();
            if (closed || busy) return;
            RoughPrompt.SelectedText = text;
            RoughPrompt.Focus(FocusState.Programmatic);
            Status.Text = "Pasted into the rough prompt.";
        }
        catch { if (!closed) Status.Text = "The clipboard is busy. Try Paste again."; }
    }

    private void Copy(object sender, RoutedEventArgs args)
    {
        if (busy || closed || string.IsNullOrWhiteSpace(EnhancedPrompt.Text)) return;
        try
        {
            var content = new DataPackage();
            content.SetText(EnhancedPrompt.Text);
            Clipboard.SetContent(content);
            Clipboard.Flush();
            Status.Text = "Enhanced prompt copied.";
        }
        catch { Status.Text = "The clipboard is busy. Try Copy again."; }
    }
}
