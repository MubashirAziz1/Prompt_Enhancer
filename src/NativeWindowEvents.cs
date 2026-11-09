using Microsoft.UI.Xaml;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace PromptEnhancer.WinUI;

// Keep the delegate alive until the subclass is explicitly removed on the UI thread.
internal sealed class NativeWindowEvents : IDisposable
{
    private readonly nint handle;
    private readonly Subclass callback;
    private bool disposed;
    public event Action Minimized;
    public event Action<uint, nuint, nint> Message;
    // Windows enforces a system minimum width on small borderless windows. Lowering
    // the tracking minimum lets the compact floating button keep its intended size.
    public SizeInt32? MinimumTrackSize { get; set; }

    public NativeWindowEvents(Window window) : this(WinRT.Interop.WindowNative.GetWindowHandle(window)) { }

    public NativeWindowEvents(nint handle)
    {
        this.handle = handle;
        callback = WindowMessage;
        if (!SetWindowSubclass(handle, callback, 1, 0)) throw new Win32Exception();
    }

    private nint WindowMessage(nint window, uint message, nuint wParam, nint lParam, nuint id, nuint data)
    {
        if (message == 5 && wParam == 1) Minimized?.Invoke();
        Message?.Invoke(message, wParam, lParam);
        nint result = DefSubclassProc(window, message, wParam, lParam);
        // WM_GETMINMAXINFO: MINMAXINFO.ptMinTrackSize follows three POINT fields.
        if (message == 0x24 && lParam != 0 && MinimumTrackSize is { } minimum)
        {
            Marshal.WriteInt32(lParam, 24, minimum.Width);
            Marshal.WriteInt32(lParam, 28, minimum.Height);
        }
        return result;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        RemoveWindowSubclass(handle, callback, 1);
    }

    private delegate nint Subclass(nint window, uint message, nuint wParam, nint lParam, nuint id, nuint data);
    [DllImport("comctl32.dll", SetLastError = true)] private static extern bool SetWindowSubclass(nint window, Subclass callback, nuint id, nuint data);
    [DllImport("comctl32.dll")] private static extern bool RemoveWindowSubclass(nint window, Subclass callback, nuint id);
    [DllImport("comctl32.dll")] private static extern nint DefSubclassProc(nint window, uint message, nuint wParam, nint lParam);
}
