using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace PromptEnhancer.WinUI;

internal sealed class NativeTrayIcon : IDisposable
{
    private const uint CallbackMessage = 0x8001;
    private readonly NativeWindowEvents events;
    private readonly LauncherWindow owner;
    private readonly nint menuHost;
    private readonly nint ownedIcon;
    private readonly Action restore, hide, quit;
    private readonly uint taskbarCreated = RegisterWindowMessage("TaskbarCreated");
    private NotifyIconData icon;
    private bool disposed;

    public NativeTrayIcon(LauncherWindow owner, Action restore, Action hide, Action quit)
    {
        this.owner = owner;
        this.restore = restore;
        this.hide = hide;
        this.quit = quit;
        // A zero-size tool window can own foreground menus while both WinUI
        // windows stay hidden. A hidden WinUI owner cannot reliably hold menu focus.
        menuHost = CreateWindowEx(0x88, "STATIC", "Prompt Enhancer notification menu", 0x80000000,
            0, 0, 0, 0, 0, 0, 0, 0);
        if (menuHost == 0) throw new Win32Exception();
        // The application's own icon, falling back to the generic one.
        ExtractIconEx(Environment.ProcessPath, 0, 0, out ownedIcon, 1);
        icon = new NotifyIconData
        {
            Size = (uint)Marshal.SizeOf<NotifyIconData>(), Window = menuHost,
            Id = 1, Flags = 1 | 2 | 4 | 128, Callback = CallbackMessage,
            Icon = ownedIcon != 0 ? ownedIcon : LoadIcon(0, 32512), Tip = owner.Title, Info = "", InfoTitle = "", Version = 4
        };
        try
        {
            events = new NativeWindowEvents(menuHost);
            events.Message += Message;
        }
        catch { DestroyWindow(menuHost); if (ownedIcon != 0) DestroyIcon(ownedIcon); throw; }
        // Explorer may not be ready yet (for example at sign-in); keep trying rather than failing the app.
        AddWithRetry(10);
    }

    // NIM_ADD, then NIM_SETVERSION for NOTIFYICON_VERSION_4 (including keyboard activation).
    private void Add()
    {
        if (!Shell_NotifyIcon(0, ref icon)) throw new Win32Exception();
        Shell_NotifyIcon(4, ref icon);
    }

    private void AddWithRetry(int attempts)
    {
        if (disposed) return;
        try { Add(); return; }
        catch (Win32Exception) when (attempts > 0) { }
        catch (Win32Exception) { return; }
        var timer = owner.DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.IsRepeating = false;
        timer.Tick += (_, _) => AddWithRetry(attempts - 1);
        timer.Start();
    }

    private void Message(uint message, nuint wParam, nint lParam)
    {
        if (disposed) return;
        if (message == taskbarCreated) { AddWithRetry(10); return; }
        if (message != CallbackMessage) return;
        uint notification = (uint)((long)lParam & 0xffff);
        if (notification is 0x203 or 0x401) owner.DispatcherQueue.TryEnqueue(() => { if (!disposed) restore(); });
        // Version 4 passes the notification anchor (screen coordinates) in wParam.
        if (notification == 0x7b) ShowMenu((short)(wParam & 0xffff), (short)((wParam >> 16) & 0xffff));
    }

    private void ShowMenu(int x, int y)
    {
        nint menu = CreatePopupMenu();
        if (menu == 0) return;
        uint command;
        try
        {
            AppendMenu(menu, 0, 1, "&Show widget");
            AppendMenu(menu, 0, 2, "&Hide");
            AppendMenu(menu, 0x800, 0, null);
            AppendMenu(menu, 0, 3, "&Quit");
            ShowWindow(menuHost, 5);
            SetForegroundWindow(icon.Window);
            command = TrackPopupMenuEx(menu, 0x100 | 0x2, x, y, icon.Window, 0);
            PostMessage(icon.Window, 0, 0, 0);
            Shell_NotifyIcon(3, ref icon);
        }
        finally { ShowWindow(menuHost, 0); DestroyMenu(menu); }
        // Leave the native callback before destroying any owning windows.
        owner.DispatcherQueue.TryEnqueue(() =>
        {
            if (disposed) return;
            if (command == 1) restore();
            if (command == 2) hide();
            if (command == 3) quit();
        });
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Shell_NotifyIcon(2, ref icon);
        events.Dispose();
        DestroyWindow(menuHost);
        if (ownedIcon != 0) DestroyIcon(ownedIcon);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint Size;
        public nint Window;
        public uint Id, Flags, Callback;
        public nint Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State, StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle;
        public uint InfoFlags;
        public Guid Guid;
        public nint BalloonIcon;
    }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern uint ExtractIconEx(string file, int index, nint large, out nint small, uint count);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string name);
    [DllImport("user32.dll")] private static extern nint LoadIcon(nint instance, nint name);
    [DllImport("user32.dll")] private static extern bool DestroyIcon(nint icon);
    [DllImport("user32.dll")] private static extern nint CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool AppendMenu(nint menu, uint flags, nuint id, string text);
    [DllImport("user32.dll")] private static extern bool DestroyMenu(nint menu);
    [DllImport("user32.dll")] private static extern uint TrackPopupMenuEx(nint menu, uint flags, int x, int y, nint owner, nint parameters);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern bool PostMessage(nint window, uint message, nuint wParam, nint lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern nint CreateWindowEx(uint exStyle, string className, string name, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint window, int command);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(nint window);
}
