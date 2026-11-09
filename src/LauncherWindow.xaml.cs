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
        OpenButton.Surface = Blob;
        OpenButton.Activated += () => OpenRequested?.Invoke();
        events = new NativeWindowEvents(this);
        events.Message += (message, wParam, _) =>
        {
            if (message != 0x02E0) return; // WM_DPICHANGED, before default sizing.
            int side = (int)(wParam & 0xFFFF); // 96 logical pixels equals the monitor DPI.
            events.MinimumTrackSize = new SizeInt32(side, side);
        };
        Blob.ShapeChanged += UpdateShape;
        AppWindow.Changed += (_, args) =>
        {
            if (args.DidSizeChange) UpdateShape();
            if (args.DidVisibilityChange) Blob.Suspend(!AppWindow.IsVisible);
        };
        Root.SizeChanged += (_, _) => UpdateShape();
        Closed += (_, _) => { Blob.Dispose(); events.Dispose(); };
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
        var size = new SizeInt32((int)(96 * scale), (int)(96 * scale));
        events.MinimumTrackSize = size;
        AppWindow.Resize(size);
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        AppWindow.Move(new PointInt32(area.X + area.Width - AppWindow.Size.Width - (int)(24 * scale), area.Y + (int)(80 * scale)));
        UpdateShape();
    }

    private void UpdateShape()
    {
        nint handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var origin = new PointInt32();
        ClientToScreen(handle, ref origin);
        origin.X -= AppWindow.Position.X;
        origin.Y -= AppWindow.Position.Y;
        double width = AppWindow.ClientSize.Width, height = AppWindow.ClientSize.Height;
        if (Root.XamlRoot != null && Root.ActualWidth > 0 && Root.ActualHeight > 0)
        {
            double scale = Root.XamlRoot.RasterizationScale;
            var offset = Root.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point());
            origin.X += (int)Math.Round(offset.X * scale);
            origin.Y += (int)Math.Round(offset.Y * scale);
            width = Root.ActualWidth * scale;
            height = Root.ActualHeight * scale;
        }
        var points = new PointInt32[128];
        for (int index = 0; index < points.Length; index++)
        {
            var point = Blob.Outline(index * Math.PI * 2 / points.Length);
            points[index] = new PointInt32(origin.X + (int)Math.Round((point.X + 1) * width / 2),
                origin.Y + (int)Math.Round((point.Y + 1) * height / 2));
        }
        nint region = CreatePolygonRgn(points, points.Length, 1);
        if (region == 0) throw new Win32Exception();
        // After success Windows owns the region, including deleting the previous one.
        if (SetWindowRgn(handle, region, true) == 0)
        {
            DeleteObject(region);
            throw new Win32Exception();
        }
    }

    [DllImport("gdi32.dll", SetLastError = true)] private static extern nint CreatePolygonRgn([In] PointInt32[] points, int count, int mode);
    [DllImport("user32.dll", SetLastError = true)] private static extern int SetWindowRgn(nint window, nint region, bool redraw);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(nint window, ref PointInt32 point);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint value);

    private void Hide(object sender, RoutedEventArgs args) => HideRequested?.Invoke();
    private void Quit(object sender, RoutedEventArgs args) => QuitRequested?.Invoke();
}
