using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace PromptEnhancer.Tests
{
    // An ordinary editor in a separate process; never touches a user's documents.
    internal sealed class CompanionEditor : IDisposable
    {
        private readonly Process process;
        private readonly IntPtr handle;

        public CompanionEditor()
        {
            process = Process.Start(new ProcessStartInfo {
                FileName = Assembly.GetExecutingAssembly().Location, Arguments = "--companion-editor",
                UseShellExecute = false, CreateNoWindow = true
            });
            try
            {
                TestRunner.PumpUntil(() => {
                    process.Refresh();
                    return process.MainWindowHandle != IntPtr.Zero;
                });
                handle = process.MainWindowHandle;
            }
            catch { Dispose(); throw; }
        }

        public static void Run()
        {
            using (var window = new Form {
                Text = "Prompt Enhancer test companion editor", WindowState = FormWindowState.Maximized
            })
            {
                var editor = new TextBox { Multiline = true, Dock = DockStyle.Fill };
                window.Controls.Add(editor);
                window.Shown += delegate { editor.Focus(); };
                Application.Run(window);
            }
        }

        public void AssertBelow(Form overlay)
        {
            TestRunner.Require(SetForegroundWindow(handle), "The companion editor must accept foreground focus.");
            TestRunner.PumpUntil(() => GetForegroundWindow() == handle);
            SendKeys.SendWait("companion");
            var text = new StringBuilder(256);
            SendMessage(GetWindow(handle, 5), 0x000D, new IntPtr(text.Capacity), text); // Child editor, WM_GETTEXT.
            TestRunner.Require(text.ToString().Contains("companion"), "Typing must reach the other process's editor.");
            var point = new Point(overlay.Left + overlay.Width / 2, overlay.Top + overlay.Height / 2);
            WindowRectangle bounds;
            TestRunner.Require(GetWindowRect(handle, out bounds) && point.X >= bounds.Left && point.X < bounds.Right &&
                point.Y >= bounds.Top && point.Y < bounds.Bottom,
                "The companion editor must cover the sampled overlay point.");
            TestRunner.Require(GetAncestor(WindowFromPoint(point), 2) == overlay.Handle,
                "The widget must remain visibly above the active companion editor.");
        }

        public void Dispose()
        {
            if (!process.HasExited)
            {
                process.CloseMainWindow();
                if (!process.WaitForExit(3000)) { process.Kill(); process.WaitForExit(3000); }
            }
            process.Dispose();
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr window, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr window, uint command);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr window, out WindowRectangle rectangle);

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowRectangle
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr parameter, StringBuilder text);
    }
}
