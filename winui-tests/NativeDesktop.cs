using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;

namespace PromptEnhancer.WinUI.Tests;

internal static class NativeDesktop
{
    public static void Chord(EditorWindow window, ushort key)
    {
        Activate(window);
        Send([Key(0x11, false), Key(key, false), Key(key, true), Key(0x11, true)]);
    }

    public static void Activate(EditorWindow window)
    {
        nint handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        ShowWindow(handle, 9);
        uint foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
        uint currentThread = GetCurrentThreadId();
        bool attached = foregroundThread != currentThread && AttachThreadInput(currentThread, foregroundThread, true);
        try { SetForegroundWindow(handle); }
        finally { if (attached) AttachThreadInput(currentThread, foregroundThread, false); }
        if (GetForegroundWindow() != handle) throw new Exception("Keyboard input must target only the WinUI test window.");
    }

    public static void Press(EditorWindow window, ushort key)
    {
        Activate(window);
        Send([Key(key, false), Key(key, true)]);
    }

    public static void TypeText(EditorWindow window, string text)
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

    private static Input Key(ushort key, bool up) => new() { Type = 1, Keyboard = new KeyboardInput { VirtualKey = key, Flags = up ? 2u : 0u } };
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct Input { [FieldOffset(0)] public uint Type; [FieldOffset(8)] public KeyboardInput Keyboard; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput { public ushort VirtualKey, ScanCode; public uint Flags, Time; public nuint ExtraInfo; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint handle);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint handle, int command);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint source, uint target, bool attach);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
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
