using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace PromptEnhancer.WinUI;

// Owns the windows for one in-memory prompt session. Only Quit destroys them.
public sealed class FloatingExperience : IDisposable
{
    private readonly EditorWindow editor;
    private readonly LauncherWindow launcher;
    private readonly Action onQuit;
    private readonly NativeWindowEvents editorEvents;
    private NativeTrayIcon tray;
    private bool exiting, hidden;

    public FloatingExperience(EditorWindow editor, LauncherWindow launcher, Action onQuit)
    {
        this.editor = editor;
        this.launcher = launcher;
        this.onQuit = onQuit;
        editor.HideRequested += Hide;
        editor.QuitRequested += Quit;
        launcher.OpenRequested += OpenEditor;
        launcher.HideRequested += Hide;
        launcher.QuitRequested += Quit;
        editor.AppWindow.Closing += (_, args) =>
        {
            if (exiting) return;
            args.Cancel = true;
            Collapse();
        };
        launcher.AppWindow.Closing += (_, args) =>
        {
            if (exiting) return;
            args.Cancel = true;
            launcher.DispatcherQueue.TryEnqueue(Quit);
        };
        // WM_SIZE arrives for actual native minimize, including the title-bar button.
        editorEvents = new NativeWindowEvents(editor);
        editorEvents.Minimized += () => editor.DispatcherQueue.TryEnqueue(Collapse);
    }

    public void Start()
    {
        if (exiting || tray != null) return;
        tray = new NativeTrayIcon(launcher, Restore, Hide, Quit);
        launcher.Activate();
    }

    public void OpenEditor()
    {
        if (exiting) return;
        hidden = false;
        ((OverlappedPresenter)editor.AppWindow.Presenter).Restore();
        // Place before showing so the editor never appears at the wrong spot first.
        PlaceEditorAtLauncher();
        editor.Activate();
        // A monitor with a different DPI resizes the window on arrival; clamp again.
        PlaceEditorAtLauncher();
        launcher.AppWindow.Hide();
    }

    private void PlaceEditorAtLauncher()
    {
        var area = DisplayArea.GetFromWindowId(launcher.AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        var size = editor.AppWindow.Size;
        editor.AppWindow.Move(new PointInt32(
            Math.Clamp(launcher.AppWindow.Position.X, area.X, area.X + Math.Max(0, area.Width - size.Width)),
            Math.Clamp(launcher.AppWindow.Position.Y, area.Y, area.Y + Math.Max(0, area.Height - size.Height))));
    }

    private void Collapse()
    {
        if (hidden || exiting) return;
        editor.AppWindow.Hide();
        launcher.Activate();
    }

    private void Hide()
    {
        if (exiting) return;
        hidden = true;
        editor.AppWindow.Hide();
        launcher.AppWindow.Hide();
    }

    // Tray restoration: bring back the floating button, unless the editor is already open
    // and in use, in which case only bring it forward without interrupting editing.
    public void Restore()
    {
        if (exiting) return;
        if (!hidden && editor.AppWindow.IsVisible) { editor.Activate(); return; }
        hidden = false;
        editor.AppWindow.Hide();
        launcher.Activate();
    }

    public void Quit()
    {
        if (exiting) return;
        exiting = true;
        tray?.Dispose();
        editorEvents.Dispose();
        editor.Close();
        launcher.Close();
        onQuit();
    }

    public void Dispose() => Quit();
}
