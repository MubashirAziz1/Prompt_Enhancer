using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;

namespace PromptEnhancer.WinUI;

internal static class NativeMetrics
{
    // Scale of the monitor currently hosting the window. Available before the first
    // layout pass, so windows can be sized and placed before they are shown.
    public static double Scale(Window window)
    {
        uint dpi = GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(window));
        return dpi == 0 ? 1 : dpi / 96.0;
    }

    public static int SystemMetric(int index, double scale) => GetSystemMetricsForDpi(index, (uint)Math.Round(96 * scale));

    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint window);
    [DllImport("user32.dll")] private static extern int GetSystemMetricsForDpi(int index, uint dpi);
}
