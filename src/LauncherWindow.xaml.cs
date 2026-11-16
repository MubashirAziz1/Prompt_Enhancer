using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace PromptEnhancer.WinUI;

public sealed partial class LauncherWindow : Window
{
    private readonly NativeWindowEvents events;
    private bool positioned;
    private double placedScale;
    public event Action OpenRequested;
    public event Action HideRequested;
    public event Action QuitRequested;

    public LauncherWindow()
    {
        InitializeComponent();
        var presenter = (OverlappedPresenter)AppWindow.Presenter;
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsAlwaysOnTop = true;
        presenter.IsResizable = presenter.IsMaximizable = presenter.IsMinimizable = false;
        AppWindow.IsShownInSwitchers = false;
        OpenButton.DragWindow = AppWindow;
        OpenButton.Activated += () => OpenRequested?.Invoke();
        events = new NativeWindowEvents(this);
        Closed += (_, _) => events.Dispose();
        // Size and place before the first show so the button never flashes at a default size.
        Place(NativeMetrics.Scale(this));
        Root.Loaded += (_, _) =>
        {
            if (positioned) return;
            positioned = true;
            double scale = Root.XamlRoot.RasterizationScale;
            if (Math.Abs(scale - placedScale) > 0.01) Place(scale);
            OpenButton.Focus(FocusState.Programmatic);
        };
    }

    private void Place(double scale)
    {
        placedScale = scale;
        var size = new SizeInt32((int)(84 * scale), (int)(64 * scale));
        events.MinimumTrackSize = size;
        AppWindow.Resize(size);
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        AppWindow.Move(new PointInt32(area.X + area.Width - AppWindow.Size.Width - (int)(24 * scale), area.Y + (int)(80 * scale)));
    }

    private void Hide(object sender, RoutedEventArgs args) => HideRequested?.Invoke();
    private void Quit(object sender, RoutedEventArgs args) => QuitRequested?.Invoke();
}
