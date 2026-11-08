using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PromptEnhancer.Tests
{
    // Input goes through Windows, including native title-bar movement and text selection.
    internal static class DesktopMouse
    {
        public static Point Center(Control control)
        {
            return control.PointToScreen(new Point(control.ClientSize.Width / 2, control.ClientSize.Height / 2));
        }

        public static void Click(Control control)
        {
            Stroke(control.FindForm(), Center(control), Point.Empty);
        }

        public static void ClickAt(Point position, bool rightButton)
        {
            Stroke(null, position, Point.Empty, rightButton);
        }

        public static void Stroke(Form window, Point start, Point delta, bool rightButton = false)
        {
            TestRunner.Require(window == null || GetAncestor(WindowFromPoint(start), 2) == window.Handle,
                "Mouse input must start on the intended test window.");
            Point previous = Cursor.Position;
            Rectangle desktop = SystemInformation.VirtualScreen;
            var input = Task.Run(() => {
                try
                {
                    Move(start, desktop);
                    Thread.Sleep(40);
                    Send(new MouseInput { Flags = rightButton ? 0x0008u : 0x0002u });
                    Thread.Sleep(60);
                    for (int step = 1; step <= 8; step++)
                    {
                        Move(new Point(start.X + delta.X * step / 8, start.Y + delta.Y * step / 8), desktop);
                        Thread.Sleep(30);
                    }
                }
                finally { Send(new MouseInput { Flags = rightButton ? 0x0010u : 0x0004u }); }
            });
            try
            {
                TestRunner.PumpUntil(() => input.IsCompleted);
                input.GetAwaiter().GetResult();
                // Drain input delivery after SendInput has returned.
                var timer = System.Diagnostics.Stopwatch.StartNew();
                while (timer.ElapsedMilliseconds < 100) { Application.DoEvents(); Thread.Sleep(5); }
            }
            finally { Cursor.Position = previous; }
        }

        private static void Move(Point point, Rectangle desktop)
        {
            Send(new MouseInput {
                X = (point.X - desktop.Left) * 65535 / (desktop.Width - 1),
                Y = (point.Y - desktop.Top) * 65535 / (desktop.Height - 1),
                Flags = 0xC001 // Absolute movement across the virtual desktop.
            });
        }

        private static void Send(MouseInput mouse)
        {
            var input = new[] { new Input { Mouse = mouse } };
            TestRunner.Require(SendInput(1, input, Marshal.SizeOf(typeof(Input))) == 1,
                "Windows did not accept test mouse input.");
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            public uint Type;
            public MouseInput Mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            public int X;
            public int Y;
            public uint Data;
            public uint Flags;
            public uint Time;
            public UIntPtr ExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint count, Input[] inputs, int size);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    }
}
