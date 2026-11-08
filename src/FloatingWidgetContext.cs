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
        private readonly NotifyIcon notificationIcon;
        private readonly ContextMenuStrip sessionMenu;
        private bool exiting;
        private bool hidden;

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
            sessionMenu = new ContextMenuStrip();
            var restore = new ToolStripMenuItem("&Show widget") { Name = "RestoreWidget" };
            restore.Click += delegate { RestoreWidget(); };
            var hide = new ToolStripMenuItem("&Hide") { Name = "HideWidget" };
            hide.Click += delegate { HideWidget(); };
            sessionMenu.Items.Add(restore);
            sessionMenu.Items.Add(hide);
            sessionMenu.Items.Add(new ToolStripSeparator());
            var quit = new ToolStripMenuItem("&Quit") { Name = "Quit" };
            quit.Click += delegate { ExitThread(); };
            sessionMenu.Items.Add(quit);
            launcher.ContextMenuStrip = button.ContextMenuStrip = sessionMenu;
            notificationIcon = new NotifyIcon {
                Text = "Prompt Enhancer", Icon = SystemIcons.Application,
                ContextMenuStrip = sessionMenu, Visible = true
            };
            notificationIcon.DoubleClick += delegate { RestoreWidget(); };
            editor.HideRequested += delegate { HideWidget(); };
            editor.QuitRequested += delegate { ExitThread(); };
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
                if (!exiting && editor.WindowState == FormWindowState.Minimized)
                    editor.BeginInvoke(new Action(delegate {
                        // Finish the native minimize transition before restoring normal state.
                        if (!exiting && !editor.IsDisposed && editor.WindowState == FormWindowState.Minimized) Collapse();
                    }));
            };
            MainForm = launcher;
            launcher.Show();
        }

        private void Expand()
        {
            hidden = false;
            var workArea = Screen.FromControl(launcher).WorkingArea;
            Size editorSize = editor.WindowState == FormWindowState.Minimized ? editor.RestoreBounds.Size : editor.Size;
            editor.StartPosition = FormStartPosition.Manual;
            editor.Location = new Point(
                Math.Max(workArea.Left, Math.Min(launcher.Left, workArea.Right - editorSize.Width)),
                Math.Max(workArea.Top, Math.Min(launcher.Top, workArea.Bottom - editorSize.Height)));
            editor.Show();
            editor.WindowState = FormWindowState.Normal;
            editor.Size = editorSize;
            launcher.Hide();
            editor.Activate();
        }

        private void Collapse()
        {
            if (hidden || exiting) return;
            // Keep native restore bounds intact until the next expansion.
            editor.Hide();
            launcher.Show();
        }

        private void HideWidget()
        {
            hidden = true;
            editor.Hide();
            launcher.Hide();
        }

        private void RestoreWidget()
        {
            if (exiting) return;
            hidden = false;
            editor.Hide();
            launcher.Show();
            launcher.Activate();
        }

        protected override void ExitThreadCore()
        {
            if (exiting) return;
            exiting = true;
            notificationIcon.Dispose();
            sessionMenu.Dispose();
            editor.Close();
            launcher.Close();
            base.ExitThreadCore();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                exiting = true;
                notificationIcon.Dispose();
                sessionMenu.Dispose();
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
