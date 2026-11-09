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
    private bool initialized;
    private double sizedScale;
    private SettingsDialog activeSettings;
    public event Action HideRequested;
    public event Action QuitRequested;

    public EditorWindow(IEnhancementProvider provider, ICredentialStore credentials)
    {
        this.provider = provider;
        this.credentials = credentials;
        InitializeComponent();
        ((OverlappedPresenter)AppWindow.Presenter).IsAlwaysOnTop = true;
        Root.ActualThemeChanged += (_, _) => UpdateWindowTheme();
        UpdateWindowTheme();
        // Size before the first show; correct it on load only if the real scale differs.
        SizeForScale(NativeMetrics.Scale(this));
        Root.Loaded += (_, _) =>
        {
            if (initialized) return;
            initialized = true;
            double scale = Root.XamlRoot.RasterizationScale;
            if (Math.Abs(scale - sizedScale) > 0.01) SizeForScale(scale);
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
            try { activeSettings?.Hide(); } catch { } // Quit while Settings is open.
            lifetime.Cancel();
            lifetime.Dispose();
        };
    }

    private void SizeForScale(double scale)
    {
        sizedScale = scale;
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        AppWindow.Resize(new SizeInt32(Math.Min((int)(780 * scale), area.Width), Math.Min((int)(850 * scale), area.Height)));
        AppWindow.Move(new PointInt32(
            Math.Clamp(AppWindow.Position.X, area.X, area.X + Math.Max(0, area.Width - AppWindow.Size.Width)),
            Math.Clamp(AppWindow.Position.Y, area.Y, area.Y + Math.Max(0, area.Height - AppWindow.Size.Height))));
    }

    private void ResultChanging(TextBox sender, TextBoxTextChangingEventArgs args) => UpdateActions();

    private void Clear(object sender, RoutedEventArgs args)
    {
        if (busy || closed) return;
        RoughPrompt.Text = EnhancedPrompt.Text = "";
        EnhanceButton.Content = "Enhance";
        Status.Text = "Cleared. Enter a rough prompt to begin.";
        RoughPrompt.Focus(FocusState.Programmatic);
        UpdateActions();
    }

    private void Hide(object sender, RoutedEventArgs args) => HideRequested?.Invoke();
    private void Quit(object sender, RoutedEventArgs args) => QuitRequested?.Invoke();

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
        EnhanceButton.IsEnabled = SettingsButton.IsEnabled = RoughPrompt.IsEnabled = PasteButton.IsEnabled = EnhancedPrompt.IsEnabled = ClearButton.IsEnabled = false;
        EnhanceButton.Content = "Enhance";
        EnhancedPrompt.Text = "";
        UpdateActions();
        Progress.Visibility = Visibility.Visible;
        Status.Text = "Enhancing…";
        bool succeeded = false;
        try
        {
            string result = await provider.EnhanceAsync(RoughPrompt.Text, EnhancementPolicy.Instructions, lifetime.Token);
            if (closed) return;
            if (string.IsNullOrWhiteSpace(result)) throw new EnhancementException(EnhancementFailure.InvalidResponse);
            EnhancedPrompt.Text = result;
            Status.Text = "Enhanced prompt ready. Review, edit, then Copy.";
            succeeded = true;
        }
        catch (EnhancementException error) { ShowFailure(error.Message); }
        catch (CredentialException error) { ShowFailure(error.Message); }
        catch { ShowFailure("Enhancement could not finish. Try again."); }
        finally
        {
            if (!closed)
            {
                busy = false;
                EnhanceButton.IsEnabled = SettingsButton.IsEnabled = RoughPrompt.IsEnabled = PasteButton.IsEnabled = EnhancedPrompt.IsEnabled = ClearButton.IsEnabled = true;
                Progress.Visibility = Visibility.Collapsed;
                UpdateActions();
                // Disabling the controls dropped keyboard focus; put it where the user continues.
                if (AppWindow.IsVisible) (succeeded ? EnhancedPrompt : (Control)EnhanceButton).Focus(FocusState.Programmatic);
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
            // SelectedText bypasses MaxLength; apply the same limit as typing and Ctrl+V.
            int room = Math.Max(0, RoughPrompt.MaxLength - (RoughPrompt.Text.Length - RoughPrompt.SelectionLength));
            bool truncated = text.Length > room;
            if (truncated)
            {
                text = text.Substring(0, room);
                if (text.Length > 0 && char.IsHighSurrogate(text[^1])) text = text.Substring(0, text.Length - 1);
            }
            RoughPrompt.SelectedText = text;
            RoughPrompt.Focus(FocusState.Programmatic);
            Status.Text = truncated
                ? $"Pasted the first part only. The rough prompt is limited to {RoughPrompt.MaxLength:N0} characters."
                : "Pasted into the rough prompt.";
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
