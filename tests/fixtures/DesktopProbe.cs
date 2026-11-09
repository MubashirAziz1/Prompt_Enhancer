using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;

// Out-of-process shell accessibility and native input. No application internals.
internal static class DesktopProbe
{
    private static int menuProcess;
    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        if (args[0] == "companion")
        {
            using (var form = new Form { Text = "WinUI acceptance companion", WindowState = FormWindowState.Maximized })
            {
                var text = new TextBox { Multiline = true, Dock = DockStyle.Fill };
                form.Controls.Add(text);
                form.Shown += delegate { text.Focus(); };
                Application.Run(form);
            }
            return 0;
        }
        try
        {
            if (args[0] == "startup")
            {
                int process = int.Parse(args[1]);
                var windows = AutomationElement.RootElement.FindAll(TreeScope.Children,
                    new PropertyCondition(AutomationElement.ProcessIdProperty, process));
                AutomationElement launcher = null;
                foreach (AutomationElement window in windows)
                {
                    var size = window.Current.BoundingRectangle;
                    if (window.Current.IsOffscreen || size.IsEmpty || size.Width <= 0 || size.Height <= 0 ||
                        !IsWindowVisible(new IntPtr(window.Current.NativeWindowHandle))) continue;
                    var button = window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "OpenEditor"));
                    if (button == null) throw new Exception("Published startup must show only the floating button.");
                    double scale = GetDpiForWindow(new IntPtr(window.Current.NativeWindowHandle)) / 96.0;
                    if (size.Width > 100 * scale || size.Height > 100 * scale) throw new Exception("Published launcher must remain compact at the actual DPI.");
                    launcher = window;
                }
                if (launcher == null) throw new Exception("Published floating button unavailable.");
                PostMessage(new IntPtr(launcher.Current.NativeWindowHandle), 0x10, IntPtr.Zero, IntPtr.Zero);
                return 0;
            }
            if (!int.TryParse(args[1].Substring(args[1].LastIndexOf(' ') + 1), out menuProcess))
                throw new Exception("Only isolated acceptance notification icons may be used.");
            if (args[0] == "absent")
            {
                AssertAbsent(args[1]);
                return 0;
            }
            string command = args[0] == "restore" ? "Show widget" : args[0] == "hide" ? "Hide" : "Quit";
            for (int attempt = 0; attempt < 8; attempt++)
            {
                try
                {
                    var item = args[0] == "double" ? null : Find(command, false);
                    if (item != null) { Click(item, false); return 0; }
                    var icon = Find(args[1], true);
                    if (icon == null)
                    {
                        var overflow = Find("Show hidden icons", true);
                        if (overflow == null) throw new Exception("Tray icon and overflow are unavailable.");
                        Click(overflow, false);
                        icon = WaitFor(args[1], true);
                        Console.WriteLine("overflow");
                    }
                    if (args[0] == "double") Click(icon, false, true);
                    else
                    {
                        Click(icon, true);
                        Click(WaitFor(command, false), false);
                    }
                    return 0;
                }
                // Reacquire only when no mouse button has yet been sent to a
                // disappearing shell target. A missing application menu still fails.
                catch (StaleShellTargetException) { if (attempt == 7) throw; Thread.Sleep(100); }
            }
            throw new Exception("The notification icon did not settle for native input.");
        }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
    }

    private static ShellTarget WaitFor(string name, bool tray)
    {
        var timer = Stopwatch.StartNew();
        while (timer.ElapsedMilliseconds < 5000)
        {
            var found = Find(name, tray);
            if (found != null) return found;
            Thread.Sleep(50);
        }
        throw new Exception("Shell element unavailable: " + name);
    }

    private static void AssertAbsent(string name)
    {
        if (Find(name, true) != null) throw new Exception("Notification icon must be removed after Quit.");
        bool opened = false;
        try
        {
            if (!OverflowVisible())
            {
                var overflow = Find("Show hidden icons", true);
                if (overflow != null)
                {
                    Click(overflow, false);
                    opened = true;
                    var timer = Stopwatch.StartNew();
                    while (!OverflowVisible() && timer.ElapsedMilliseconds < 5000) Thread.Sleep(50);
                    if (!OverflowVisible()) throw new Exception("The real overflow must open before verifying notification-icon removal.");
                }
            }
            if (Find(name, true) != null) throw new Exception("Quit left the notification icon in overflow.");
        }
        finally
        {
            if (opened && OverflowVisible())
            {
                var overflow = Find("Show hidden icons", true);
                if (overflow != null) Click(overflow, false);
            }
        }
    }

    private static bool OverflowVisible()
    {
        var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition);
        foreach (AutomationElement window in windows)
            if ((window.Current.ClassName == "NotifyIconOverflowWindow" || window.Current.ClassName == "TopLevelWindowForOverflowXamlIsland") &&
                !window.Current.IsOffscreen && IsWindowVisible(new IntPtr(window.Current.NativeWindowHandle))) return true;
        return false;
    }

    private static ShellTarget Find(string name, bool tray)
    {
        var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition);
        foreach (AutomationElement window in windows)
        {
            string cls = window.Current.ClassName;
            if (tray && cls != "Shell_TrayWnd" && cls != "NotifyIconOverflowWindow" && cls != "TopLevelWindowForOverflowXamlIsland") continue;
            if (!tray && (cls != "#32768" || window.Current.ProcessId != menuProcess)) continue;
            // The shell can retain overflow children reporting IsOffscreen=false
            // after their top-level XAML island has hidden.
            if (window.Current.IsOffscreen || (window.Current.NativeWindowHandle != 0 &&
                !IsWindowVisible(new IntPtr(window.Current.NativeWindowHandle)))) continue;
            var matches = window.FindAll(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.NameProperty, name, PropertyConditionFlags.IgnoreCase),
                new PropertyCondition(AutomationElement.ControlTypeProperty, tray ? ControlType.Button : ControlType.MenuItem)));
            foreach (AutomationElement match in matches)
            {
                var bounds = match.Current.BoundingRectangle;
                var nativeWindow = new IntPtr(window.Current.NativeWindowHandle);
                if (match.Current.IsOffscreen || bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0 || nativeWindow == IntPtr.Zero) continue;
                var center = new Point((int)(bounds.Left + bounds.Width / 2), (int)(bounds.Top + bounds.Height / 2));
                // Tooltip descendants can share the button's name and role. The
                // real target must physically belong to this tray/menu window.
                if (GetAncestor(WindowFromPoint(center), 2) == nativeWindow) return new ShellTarget(match, nativeWindow);
            }
        }
        return null;
    }

    private static void Click(ShellTarget target, bool right, bool doubleClick = false)
    {
        System.Windows.Rect bounds;
        try { bounds = target.Element.Current.BoundingRectangle; }
        catch (ElementNotAvailableException) { throw new StaleShellTargetException(); }
        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0) throw new StaleShellTargetException();
        var previous = Cursor.Position;
        try
        {
            var desktop = SystemInformation.VirtualScreen;
            Send(new MouseInput {
                X = ((int)(bounds.Left + bounds.Width / 2) - desktop.Left) * 65535 / (desktop.Width - 1),
                Y = ((int)(bounds.Top + bounds.Height / 2) - desktop.Top) * 65535 / (desktop.Height - 1), Flags = 0xC001 });
            Thread.Sleep(70);
            var actual = Cursor.Position;
            if (Math.Abs(actual.X - (bounds.Left + bounds.Width / 2)) > 2 || Math.Abs(actual.Y - (bounds.Top + bounds.Height / 2)) > 2)
                throw new Exception("Native shell input did not reach its expected physical point.");
            if (GetAncestor(WindowFromPoint(actual), 2) != target.Window) throw new StaleShellTargetException();
            for (int click = 0; click < (doubleClick ? 2 : 1); click++)
            {
                Send(new MouseInput { Flags = right ? 8u : 2u });
                Thread.Sleep(40);
                Send(new MouseInput { Flags = right ? 16u : 4u });
                if (doubleClick && click == 0) Thread.Sleep(60);
            }
            Thread.Sleep(120);
        }
        finally { Cursor.Position = previous; }
    }
    private static void Send(MouseInput mouse)
    {
        if (SendInput(1, new[] { new Input { Mouse = mouse } }, Marshal.SizeOf(typeof(Input))) != 1)
            throw new Exception("Windows did not accept the shell test mouse input.");
    }
    private sealed class StaleShellTargetException : Exception
    {
        public StaleShellTargetException() : base("The shell target disappeared before mouse-down.") { }
    }
    private sealed class ShellTarget
    {
        public readonly AutomationElement Element;
        public readonly IntPtr Window;
        public ShellTarget(AutomationElement element, IntPtr window) { Element = element; Window = window; }
    }
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput { public int X, Y; public uint Data, Flags, Time; public UIntPtr Extra; }
    [DllImport("user32.dll")] private static extern uint SendInput(uint count, Input[] input, int size);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
}
