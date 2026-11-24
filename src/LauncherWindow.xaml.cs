using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using System.ComponentModel;
using System.Runtime.InteropServices;

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
        AppWindow.Changed += (_, args) => { if (args.DidSizeChange) UpdateRoundRegion(); };
        Closed += (_, _) => events.Dispose();
        // Size and place before the first show so the button never flashes at a default size.
        Place(NativeMetrics.Scale(this));
        Root.Loaded += (_, _) =>
        {
            if (positioned) return;
            positioned = true;
            double scale = Root.XamlRoot.RasterizationScale;
            if (Math.Abs(scale - placedScale) > 0.01) Place(scale);
            OpenButton.Focus(FocusState.Pointer);
        };
    }

    private void Place(double scale)
    {
        placedScale = scale;
        var size = new SizeInt32((int)(80 * scale), (int)(80 * scale));
        events.MinimumTrackSize = size;
        AppWindow.ResizeClient(size);
        UpdateRoundRegion();
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        AppWindow.Move(new PointInt32(area.X + area.Width - AppWindow.Size.Width - (int)(24 * scale), area.Y + (int)(80 * scale)));
    }

    private void UpdateRoundRegion()
    {
        nint handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        GetClientRect(handle, out var client);
        GetWindowRect(handle, out var window);
        var origin = new PointInt32();
        ClientToScreen(handle, ref origin);
        int x = origin.X - window.Left, y = origin.Y - window.Top;
        // Match the XAML surface, excluding the invisible native resize margins.
        nint region = CreateEllipticRgn(x, y, x + client.Right + 1, y + client.Bottom + 1);
        if (region == 0) throw new Win32Exception();
        // Windows owns the region after a successful SetWindowRgn call.
        if (SetWindowRgn(handle, region, true) == 0)
        {
            int error = Marshal.GetLastWin32Error();
            DeleteObject(region);
            throw new Win32Exception(error);
        }
    }

    [DllImport("gdi32.dll", SetLastError = true)] private static extern nint CreateEllipticRgn(int left, int top, int right, int bottom);
    [DllImport("user32.dll", SetLastError = true)] private static extern int SetWindowRgn(nint window, nint region, bool redraw);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint value);
    [StructLayout(LayoutKind.Sequential)] private struct Rectangle { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool GetClientRect(nint window, out Rectangle rectangle);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out Rectangle rectangle);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(nint window, ref PointInt32 point);

    private void Hide(object sender, RoutedEventArgs args) => HideRequested?.Invoke();
    private void Quit(object sender, RoutedEventArgs args) => QuitRequested?.Invoke();
}
