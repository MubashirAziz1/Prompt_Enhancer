using System;
using System.Drawing;
using System.Windows.Forms;

namespace PromptEnhancer
{
    // The launcher owns application lifetime; closing the editor only collapses it.
    public sealed class FloatingWidgetContext : ApplicationContext
    {
        private readonly Form launcher;
        private readonly MainForm editor;
        private bool exiting;

        public FloatingWidgetContext(IEnhancementProvider provider, ICredentialStore credentials)
        {
            editor = new MainForm(provider, credentials);
            launcher = new Form {
                Name = "FloatingWidget", Text = "Prompt Enhancer", TopMost = true,
                FormBorderStyle = FormBorderStyle.None, ClientSize = new Size(76, 60),
                StartPosition = FormStartPosition.Manual, AutoScaleMode = AutoScaleMode.Dpi
            };
            var button = new DraggableLauncherButton {
                Name = "OpenEditor", Text = "Prompt\nEnhancer", AccessibleName = "Open prompt editor",
                Dock = DockStyle.Fill, BackColor = Color.FromArgb(32, 90, 168),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            button.Click += delegate { Expand(); };
            launcher.Controls.Add(button);
            var workArea = Screen.PrimaryScreen.WorkingArea;
            launcher.Location = new Point(workArea.Right - launcher.Width - 24, workArea.Top + 80);
            editor.FormClosing += delegate(object sender, FormClosingEventArgs args) {
                if (!exiting && args.CloseReason == CloseReason.UserClosing)
                {
                    args.Cancel = true;
                    Collapse();
                }
            };
            editor.Resize += delegate {
                if (!exiting && editor.WindowState == FormWindowState.Minimized) Collapse();
            };
            MainForm = launcher;
            launcher.Show();
        }

        private void Expand()
        {
            var workArea = Screen.FromControl(launcher).WorkingArea;
            editor.StartPosition = FormStartPosition.Manual;
            editor.Location = new Point(
                Math.Max(workArea.Left, Math.Min(launcher.Left, workArea.Right - editor.Width)),
                Math.Max(workArea.Top, Math.Min(launcher.Top, workArea.Bottom - editor.Height)));
            editor.Show();
            launcher.Hide();
            editor.Activate();
        }

        private void Collapse()
        {
            // Normalize while hidden so the next expansion is an ordinary editor window.
            editor.Hide();
            editor.WindowState = FormWindowState.Normal;
            launcher.Show();
        }

        protected override void ExitThreadCore()
        {
            if (exiting) return;
            exiting = true;
            editor.Close();
            launcher.Close();
            base.ExitThreadCore();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                exiting = true;
                editor.Dispose();
                launcher.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class DraggableLauncherButton : Button
    {
        private Point pressPosition;
        private Point windowPosition;
        private bool pressed;
        private bool dragged;

        protected override void OnMouseDown(MouseEventArgs args)
        {
            base.OnMouseDown(args);
            if (args.Button != MouseButtons.Left) return;
            pressed = true;
            dragged = false;
            pressPosition = Cursor.Position;
            windowPosition = FindForm().Location;
        }

        protected override void OnMouseMove(MouseEventArgs args)
        {
            base.OnMouseMove(args);
            if (!pressed) return;
            Point pointer = Cursor.Position;
            var threshold = new Rectangle(
                pressPosition.X - SystemInformation.DragSize.Width / 2,
                pressPosition.Y - SystemInformation.DragSize.Height / 2,
                SystemInformation.DragSize.Width, SystemInformation.DragSize.Height);
            if (!dragged && threshold.Contains(pointer)) return;
            dragged = true;
            FindForm().Location = new Point(windowPosition.X + pointer.X - pressPosition.X,
                windowPosition.Y + pointer.Y - pressPosition.Y);
        }

        protected override void OnMouseUp(MouseEventArgs args)
        {
            pressed = false;
            base.OnMouseUp(args);
        }

        protected override void OnMouseCaptureChanged(EventArgs args)
        {
            pressed = false;
            base.OnMouseCaptureChanged(args);
        }

        protected override void OnKeyDown(KeyEventArgs args)
        {
            dragged = false;
            base.OnKeyDown(args);
        }

        protected override void OnClick(EventArgs args)
        {
            if (!dragged) base.OnClick(args);
        }
    }
}
