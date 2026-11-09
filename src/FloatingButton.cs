using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace PromptEnhancer.WinUI;

public sealed class FloatingButton : Button
{
    private PointInt32 press, origin;
    private bool pressed, rightPressed, dragged;
    public AppWindow DragWindow { get; set; }
    public event Action Activated;

    public FloatingButton() => Click += (_, _) => Activated?.Invoke(); // Keyboard and automation.

    protected override void OnPointerPressed(PointerRoutedEventArgs args)
    {
        if (args.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            rightPressed = CapturePointer(args.Pointer);
            args.Handled = true;
            return;
        }
        if (!args.GetCurrentPoint(this).Properties.IsLeftButtonPressed) { base.OnPointerPressed(args); return; }
        // Own mouse capture rather than allowing Button's click state to compete with dragging.
        GetCursorPos(out press);
        origin = DragWindow.Position;
        dragged = false;
        pressed = CapturePointer(args.Pointer);
        args.Handled = true;
    }

    protected override void OnPointerMoved(PointerRoutedEventArgs args)
    {
        if (!pressed) { base.OnPointerMoved(args); return; }
        GetCursorPos(out var pointer);
        int dx = pointer.X - press.X, dy = pointer.Y - press.Y;
        double scale = XamlRoot?.RasterizationScale ?? 1;
        // SM_CXDRAG / SM_CYDRAG at this monitor's DPI.
        if (!dragged && Math.Abs(dx) < NativeMetrics.SystemMetric(68, scale) && Math.Abs(dy) < NativeMetrics.SystemMetric(69, scale)) return;
        dragged = true;
        var area = DisplayArea.GetFromPoint(pointer, DisplayAreaFallback.Nearest).WorkArea;
        DragWindow.Move(new PointInt32(
            Math.Clamp(origin.X + dx, area.X, area.X + Math.Max(0, area.Width - DragWindow.Size.Width)),
            Math.Clamp(origin.Y + dy, area.Y, area.Y + Math.Max(0, area.Height - DragWindow.Size.Height))));
        args.Handled = true;
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs args)
    {
        if (rightPressed)
        {
            rightPressed = false;
            var position = args.GetCurrentPoint(this).Position;
            ReleasePointerCapture(args.Pointer);
            // The pointer was handled on press, so the default right-tap path never runs.
            ContextFlyout?.ShowAt(this, new FlyoutShowOptions { Position = position });
            args.Handled = true;
            return;
        }
        if (!pressed) { base.OnPointerReleased(args); return; }
        bool activate = !dragged;
        pressed = false;
        rightPressed = false;
        ReleasePointerCapture(args.Pointer);
        args.Handled = true;
        if (activate) Activated?.Invoke();
    }

    protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
    {
        pressed = false;
        rightPressed = false;
        base.OnPointerCaptureLost(args);
    }

    [DllImport("user32.dll")] private static extern bool GetCursorPos(out PointInt32 point);
}
