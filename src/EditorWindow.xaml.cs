using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Microsoft.UI.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;

namespace PromptEnhancer.WinUI;

public sealed partial class EditorWindow : Window
{
    private readonly CurrentPromptSession session;
    private readonly ICredentialStore credentials;
    private bool rendering;
    private bool closed;
    private bool initialized;
    private double sizedScale;
    private SettingsDialog activeSettings;
    private bool hasKey;
    public event Action CollapseRequested;
    public event Action QuitRequested;

    public EditorWindow(IEnhancementProvider provider, ICredentialStore credentials)
    {
        session = new CurrentPromptSession(provider);
        this.credentials = credentials;
        InitializeComponent();
        session.Changed += RenderSession;
        RenderSession();
        var presenter = (OverlappedPresenter)AppWindow.Presenter;
        presenter.IsAlwaysOnTop = true;
        presenter.IsMaximizable = false;
        presenter.SetBorderAndTitleBar(true, false);
        DragRegion.SizeChanged += (_, _) => UpdateDragRegion();
        AppWindow.Changed += (_, args) =>
        {
            if (args.DidSizeChange) UpdateDragRegion();
        };
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
                hasKey = !string.IsNullOrWhiteSpace(credentials.Load());
                if (hasKey) Status.Text = "Ready. Saved API key available.";
            }
            catch { Status.Text = "The saved API key could not be read. Open Settings to replace it."; }
            UpdateActions();
            UpdateDragRegion();
            RoughPrompt.Focus(FocusState.Programmatic);
        };
        Closed += (_, _) =>
        {
            closed = true;
            session.Changed -= RenderSession;
            try { activeSettings?.Hide(); } catch { } // Quit while Settings is open.
            session.End();
        };
    }

    private void SizeForScale(double scale)
    {
        sizedScale = scale;
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        AppWindow.Resize(new SizeInt32(Math.Min((int)(420 * scale), area.Width), Math.Min((int)(530 * scale), area.Height)));
        AppWindow.Move(new PointInt32(
            Math.Clamp(AppWindow.Position.X, area.X, area.X + Math.Max(0, area.Width - AppWindow.Size.Width)),
            Math.Clamp(AppWindow.Position.Y, area.Y, area.Y + Math.Max(0, area.Height - AppWindow.Size.Height))));
    }

    private void UpdateDragRegion()
    {
        if (closed || Root.XamlRoot == null || DragRegion.ActualWidth <= 0) return;
        double scale = Root.XamlRoot.RasterizationScale;
        var point = DragRegion.TransformToVisual(Root).TransformPoint(new Windows.Foundation.Point());
        InputNonClientPointerSource.GetForWindowId(AppWindow.Id).SetRegionRects(NonClientRegionKind.Caption,
            [new RectInt32((int)(point.X * scale), (int)(point.Y * scale),
                (int)(DragRegion.ActualWidth * scale), (int)(DragRegion.ActualHeight * scale))]);
    }

    private void OpenSessionMenu(object sender, RoutedEventArgs args) => SessionFlyout.ShowAt((FrameworkElement)sender);
    private void Collapse(object sender, RoutedEventArgs args) => CollapseRequested?.Invoke();
    private void RoughChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        if (!rendering && !closed) session.EditRoughPrompt(sender.Text);
    }

    private void ResultChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        if (!rendering && !closed) session.EditEnhancedPrompt(sender.Text);
    }
    private void ResultFocusChanged(object sender, RoutedEventArgs args) => UpdateActions();

    private void Clear(object sender, RoutedEventArgs args)
    {
        if (closed || !session.New()) return;
        Status.Text = session.State.Guidance;
        RoughPrompt.Focus(FocusState.Programmatic);
    }

    private void UndoSession(object sender, RoutedEventArgs args)
    {
        if (closed || !session.SwapRecovery()) return;
        Status.Text = session.State.Guidance;
        RoughPrompt.Focus(FocusState.Programmatic);
    }

    private void Quit(object sender, RoutedEventArgs args) { SessionFlyout.Hide(); QuitRequested?.Invoke(); }

    private void UpdateWindowTheme()
    {
        AppWindow.TitleBar.PreferredTheme = Root.ActualTheme == ElementTheme.Dark ? TitleBarTheme.Dark : TitleBarTheme.Light;
        if (activeSettings != null) activeSettings.RequestedTheme = Root.ActualTheme;
    }

    private async void OpenSettings(object sender, RoutedEventArgs args)
    {
        if (!session.State.IsIdle || closed || activeSettings != null) return;
        SettingsButton.IsEnabled = false;
        SetupButton.IsEnabled = false;
        var dialog = new SettingsDialog(credentials) { XamlRoot = Root.XamlRoot, RequestedTheme = Root.ActualTheme };
        activeSettings = dialog;
        try
        {
            if (await dialog.ShowAsync() == ContentDialogResult.Primary && dialog.KeySaved && !closed)
            {
                hasKey = true;
                Status.Text = "API key saved with Windows protection. Ready.";
            }
        }
        catch { if (!closed) Status.Text = "Settings could not open. Try again."; }
        finally { activeSettings = null; if (!closed) UpdateActions(); }
    }

    private void RenderSession()
    {
        if (closed) return;
        if (!DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(RenderSession);
            return;
        }
        rendering = true;
        try
        {
            // Avoid feeding rendered values back as edits or resetting the user's caret.
            if (RoughPrompt.Text != session.State.RoughPrompt) RoughPrompt.Text = session.State.RoughPrompt;
            if (EnhancedPrompt.Text != session.State.EnhancedPrompt) EnhancedPrompt.Text = session.State.EnhancedPrompt;
            UpdateActions();
        }
        finally { rendering = false; }
    }

    private void UpdateActions()
    {
        if (closed || CopyButton == null || EmptyState == null || EnhancedPrompt == null) return;
        var state = session.State;
        EnhanceButton.IsEnabled = RoughPrompt.IsEnabled = PasteButton.IsEnabled = EnhancedPrompt.IsEnabled = ClearButton.IsEnabled = state.IsIdle;
        SettingsButton.IsEnabled = SetupButton.IsEnabled = state.IsIdle && activeSettings == null;
        CopyButton.IsEnabled = state.CanCopy;
        EnhanceButton.Content = state.EnhancementAction == EnhancementAction.Retry ? "Retry" : "Enhance";
        Progress.Visibility = state.Phase == SessionPhase.Pending ? Visibility.Visible : Visibility.Collapsed;
        EmptyState.Visibility = !state.HasResult && state.IsIdle && EnhancedPrompt.FocusState == FocusState.Unfocused ? Visibility.Visible : Visibility.Collapsed;
        ResultTitle.Text = state.ResultKind == ResultKind.Previous && state.HasResult ? "Previous result" : "Enhanced prompt";
        SetupButton.Visibility = hasKey ? Visibility.Collapsed : Visibility.Visible;
        UndoButton.Visibility = state.RecoveryAction == RecoveryAction.None ? Visibility.Collapsed : Visibility.Visible;
        UndoButton.IsEnabled = state.CanRecover;
        UndoButton.Content = state.RecoveryAction == RecoveryAction.Redo ? "Redo" : "Undo";
    }

    private async void Enhance(object sender, RoutedEventArgs args)
    {
        if (closed || !session.State.IsIdle) return;
        var operation = session.EnhanceAsync();
        Status.Text = session.State.Guidance;
        bool succeeded = await operation;
        if (closed) return;
        Status.Text = session.State.Guidance;
        // Disabling controls dropped keyboard focus; hidden completion must not activate a window.
        if (AppWindow.IsVisible) (succeeded ? EnhancedPrompt : (Control)EnhanceButton).Focus(FocusState.Programmatic);
    }

    private async void Paste(object sender, RoutedEventArgs args)
    {
        if (!session.State.IsIdle || closed) return;
        // Clipboard access starts only with this explicit user action.
        try
        {
            var content = Clipboard.GetContent();
            if (!content.Contains(StandardDataFormats.Text)) { Status.Text = "The clipboard has no text to paste."; return; }
            string text = await content.GetTextAsync();
            if (closed || !session.State.IsIdle) return;
            // SelectedText bypasses MaxLength; apply the same limit as typing and Ctrl+V.
            int room = Math.Max(0, RoughPrompt.MaxLength - (RoughPrompt.Text.Length - RoughPrompt.SelectionLength));
            bool truncated = text.Length > room;
            if (truncated)
            {
                text = text.Substring(0, room);
                if (text.Length > 0 && char.IsHighSurrogate(text[^1])) text = text.Substring(0, text.Length - 1);
            }
            RoughPrompt.SelectedText = text;
            UpdateDragRegion();
            RoughPrompt.Focus(FocusState.Programmatic);
            Status.Text = truncated
                ? $"Pasted the first part only. The rough prompt is limited to {RoughPrompt.MaxLength:N0} characters."
                : "Pasted into the rough prompt.";
        }
        catch { if (!closed) Status.Text = "The clipboard is busy. Try Paste again."; }
    }

    private void Copy(object sender, RoutedEventArgs args)
    {
        if (closed || !session.State.CanCopy) return;
        try
        {
            var content = new DataPackage();
            content.SetText(session.State.EnhancedPrompt);
            Clipboard.SetContent(content);
            Clipboard.Flush();
            Status.Text = "Enhanced prompt copied.";
        }
        catch { Status.Text = "The clipboard is busy. Try Copy again."; }
    }
}
