using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using System.Diagnostics;
using System.Text;

namespace PromptEnhancer.WinUI.Tests;

internal static class NativeDesktop
{
    public static void Close(Microsoft.UI.Xaml.Window window) =>
        PostMessage(WinRT.Interop.WindowNative.GetWindowHandle(window), 0x10, 0, 0);
    [DllImport("user32.dll")] private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
    public static void Chord(Window window, ushort key)
    {
        Activate(window);
        Send([Key(0x11, false), Key(key, false), Key(key, true), Key(0x11, true)]);
    }

    public static void Activate(Window window) => Activate(WinRT.Interop.WindowNative.GetWindowHandle(window));

    private static void Activate(nint handle)
    {
        ShowWindow(handle, IsIconic(handle) ? 9 : 5);
        uint foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
        uint currentThread = GetCurrentThreadId();
        bool attached = foregroundThread != currentThread && AttachThreadInput(currentThread, foregroundThread, true);
        try { SetForegroundWindow(handle); }
        finally { if (attached) AttachThreadInput(currentThread, foregroundThread, false); }
        if (GetForegroundWindow() != handle) throw new Exception("Keyboard input must target only the WinUI test window.");
    }

    public static async Task Stroke(Window window, PointInt32 start, PointInt32 delta, bool right = false)
    {
        Activate(window);
        await Task.Delay(200); // Allow the re-shown XAML input site to attach before native input.
        nint handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        if (GetAncestor(WindowFromPoint(start), 2) != handle) throw new Exception("Mouse input must target the intended acceptance window.");
        GetCursorPos(out var previous);
        try
        {
            await Task.Run(() =>
            {
                Move(start);
                Thread.Sleep(100);
                Send([new Input { Mouse = new MouseInput { Flags = right ? 8u : 2u } }]);
                Thread.Sleep(100); // Let the target establish capture before moving.
                try
                {
                    for (int step = 1; step <= 10; step++)
                    {
                        Move(new PointInt32(start.X + delta.X * step / 10, start.Y + delta.Y * step / 10));
                        Thread.Sleep(30);
                    }
                }
                finally { Send([new Input { Mouse = new MouseInput { Flags = right ? 16u : 4u } }]); }
            });
            await Task.Delay(150);
        }
        finally { SetCursorPos(previous.X, previous.Y); }
    }

    public static PointInt32 Center(Window window) => new(window.AppWindow.Position.X + window.AppWindow.Size.Width / 2,
        window.AppWindow.Position.Y + window.AppWindow.Size.Height / 2);

    public static async Task Hold(Window window, Func<Task> whilePressed)
    {
        Activate(window);
        await Task.Delay(200);
        var point = Center(window);
        if (GetAncestor(WindowFromPoint(point), 2) != WinRT.Interop.WindowNative.GetWindowHandle(window))
            throw new Exception("Mouse input must target the intended acceptance window.");
        GetCursorPos(out var previous);
        try
        {
            Move(point);
            await Task.Delay(100);
            Send([new Input { Mouse = new MouseInput { Flags = 2 } }]);
            try { await Task.Delay(450); await whilePressed(); }
            finally { Send([new Input { Mouse = new MouseInput { Flags = 4 } }]); }
        }
        finally { SetCursorPos(previous.X, previous.Y); }
    }
    public static PointInt32 ClientPoint(Window window, int x, int y)
    {
        var point = new PointInt32(x, y);
        ClientToScreen(WinRT.Interop.WindowNative.GetWindowHandle(window), ref point);
        return point;
    }

    public static async Task AssertAboveCompanion(Window overlay, Process companion)
    {
        companion.Refresh();
        nint handle = companion.MainWindowHandle;
        Activate(handle);
        GetWindowRect(handle, out var bounds);
        // Give the companion's edit control native pointer focus after activation.
        var focusPoint = new PointInt32(bounds.Left + 30, bounds.Bottom - 60);
        if (GetAncestor(WindowFromPoint(focusPoint), 2) != handle) throw new Exception("The companion focus point must be unobstructed.");
        GetCursorPos(out var previous);
        try
        {
            Move(focusPoint);
            Send([new Input { Mouse = new MouseInput { Flags = 2 } }, new Input { Mouse = new MouseInput { Flags = 4 } }]);
            await Task.Delay(150);
        }
        finally { SetCursorPos(previous.X, previous.Y); }
        Send("companion".SelectMany(c => new[]
        {
            new Input { Type = 1, Keyboard = new KeyboardInput { ScanCode = c, Flags = 4 } },
            new Input { Type = 1, Keyboard = new KeyboardInput { ScanCode = c, Flags = 6 } }
        }).ToArray());
        await Task.Delay(150);
        var text = new StringBuilder(256);
        SendMessage(GetWindow(handle, 5), 0xD, text.Capacity, text);
        if (!text.ToString().Contains("companion")) throw new Exception("The active companion editor must receive real keyboard input.");
        var point = Center(overlay);
        if (point.X < bounds.Left || point.X >= bounds.Right || point.Y < bounds.Top || point.Y >= bounds.Bottom ||
            GetAncestor(WindowFromPoint(point), 2) != WinRT.Interop.WindowNative.GetWindowHandle(overlay))
            throw new Exception($"The WinUI window must actually stack above the active ordinary editor. Point={point.X},{point.Y}; companion={bounds.Left},{bounds.Top},{bounds.Right},{bounds.Bottom}; overlay={WinRT.Interop.WindowNative.GetWindowHandle(overlay)}; hit={GetAncestor(WindowFromPoint(point), 2)}; topmost={((Microsoft.UI.Windowing.OverlappedPresenter)overlay.AppWindow.Presenter).IsAlwaysOnTop}");
    }

    public static void Press(Window window, ushort key)
    {
        Activate(window);
        Send([Key(key, false), Key(key, true)]);
    }

    public static void TypeText(Window window, string text)
    {
        Activate(window);
        Send(text.SelectMany(character => new[]
        {
            new Input { Type = 1, Keyboard = new KeyboardInput { ScanCode = character, Flags = 4 } },
            new Input { Type = 1, Keyboard = new KeyboardInput { ScanCode = character, Flags = 6 } }
        }).ToArray());
    }

    private static void Send(Input[] inputs)
    {
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
            throw new Exception("Windows did not accept the test keyboard input.");
    }

    private static void Move(PointInt32 point) => Send([new Input { Mouse = new MouseInput
    {
        X = (point.X - GetSystemMetrics(76)) * 65535 / (GetSystemMetrics(78) - 1),
        Y = (point.Y - GetSystemMetrics(77)) * 65535 / (GetSystemMetrics(79) - 1),
        Flags = 0xC001 // Native absolute movement over the virtual desktop.
    } }]);

    private static Input Key(ushort key, bool up) => new() { Type = 1, Keyboard = new KeyboardInput { VirtualKey = key, Flags = up ? 2u : 0u } };
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct Input { [FieldOffset(0)] public uint Type; [FieldOffset(8)] public KeyboardInput Keyboard; [FieldOffset(8)] public MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput { public int X, Y; public uint Data, Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] private struct Rectangle { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput { public ushort VirtualKey, ScanCode; public uint Flags, Time; public nuint ExtraInfo; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint handle);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint handle, int command);
    [DllImport("user32.dll")] private static extern bool IsIconic(nint handle);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint source, uint target, bool attach);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out PointInt32 point);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(nint window, ref PointInt32 point);
    [DllImport("user32.dll")] private static extern nint WindowFromPoint(PointInt32 point);
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out Rectangle rectangle);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint SendMessage(nint window, uint message, nint parameter, StringBuilder text);
}

internal sealed class ClipboardFixture
{
    public const string Sentinel = "WinUI clipboard preservation sentinel";
    private readonly object original;
    private string originalText;
    public ClipboardFixture()
    {
        Marshal.ThrowExceptionForHR(OleGetClipboard(out original));
    }

    public async Task CaptureText() => originalText = await Text();

    public static async Task SetText(string text)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                var data = new DataPackage();
                data.SetText(text);
                Clipboard.SetContent(data);
                Clipboard.Flush();
                return;
            }
            catch (COMException) when (attempt < 20) { await Task.Delay(50); }
        }
    }

    public static async Task<string> Text()
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                var content = Clipboard.GetContent();
                return content.Contains(StandardDataFormats.Text) ? await content.GetTextAsync() : "";
            }
            catch (COMException) when (attempt < 20) { await Task.Delay(50); }
        }
    }

    public async Task Restore()
    {
        try
        {
            int result = 0;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                result = OleSetClipboard(original);
                if (result >= 0)
                {
                    result = OleFlushClipboard();
                    if (result >= 0)
                    {
                        if (await Text() != originalText) throw new Exception("Clipboard text did not match its pre-test contents after restoration.");
                        return;
                    }
                }
                await Task.Delay(50);
            }
            Marshal.ThrowExceptionForHR(result);
        }
        finally { if (original != null && Marshal.IsComObject(original)) Marshal.ReleaseComObject(original); }
    }

    [DllImport("ole32.dll")] private static extern int OleGetClipboard([MarshalAs(UnmanagedType.Interface)] out object data);
    [DllImport("ole32.dll")] private static extern int OleSetClipboard([MarshalAs(UnmanagedType.Interface)] object data);
    [DllImport("ole32.dll")] private static extern int OleFlushClipboard();
}

internal sealed class ClipboardLock : IDisposable
{
    private readonly ManualResetEventSlim release = new();
    private readonly Thread thread;
    private ClipboardLock(TaskCompletionSource ready)
    {
        thread = new Thread(() =>
        {
            try
            {
                if (!OpenClipboard(0)) throw new Exception("Could not hold the clipboard for the busy-clipboard check.");
                try { ready.SetResult(); release.Wait(TimeSpan.FromSeconds(12)); }
                finally { CloseClipboard(); }
            }
            catch (Exception error) { ready.TrySetException(error); }
        }) { IsBackground = true };
        thread.Start();
    }
    public static async Task<ClipboardLock> Acquire()
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var holder = new ClipboardLock(ready);
        try { await ready.Task; return holder; }
        catch { holder.Dispose(); throw; }
    }
    public void Dispose() { release.Set(); thread.Join(); release.Dispose(); }
    [DllImport("user32.dll")] private static extern bool OpenClipboard(nint owner);
    [DllImport("user32.dll")] private static extern bool CloseClipboard();
}
