using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace PromptEnhancer
{
    public sealed class MainForm : Form
    {
        private readonly IEnhancementProvider provider;
        private readonly ICredentialStore credentials;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly TextBox roughPrompt;
        private readonly TextBox enhancedPrompt;
        private readonly Button enhance;
        private readonly Button settings;
        private readonly Label status;

        public MainForm(IEnhancementProvider provider, ICredentialStore credentials)
        {
            this.provider = provider;
            this.credentials = credentials;
            Text = "Prompt Enhancer";
            Name = "PromptEnhancer";
            Font = new Font("Segoe UI", 10);
            BackColor = Color.FromArgb(246, 248, 250);
            ClientSize = new Size(740, 670);
            MinimumSize = new Size(520, 560);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;

            var layout = new TableLayoutPanel {
                Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 9
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            header.Controls.Add(new Label { Text = "Prompt Enhancer", Font = new Font(Font.FontFamily, 20, FontStyle.Bold), AutoSize = true }, 0, 0);
            settings = new Button { Name = "Settings", Text = "Settings", Dock = DockStyle.Fill, AccessibleName = "Settings", TabIndex = 0 };
            settings.Click += OpenSettings;
            header.Controls.Add(settings, 1, 0);
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(new Label { Text = "Turn a rough idea into a clear English prompt.", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(65, 75, 85) }, 0, 1);
            layout.Controls.Add(new Label { Text = "&Rough prompt", AutoSize = true }, 0, 2);
            roughPrompt = Editor("RoughPrompt", false);
            roughPrompt.TabIndex = 1;
            layout.Controls.Add(roughPrompt, 0, 3);
            enhance = new Button {
                Name = "Enhance", Text = "Enhance", AccessibleName = "Enhance", TabIndex = 2,
                Dock = DockStyle.Right, Width = 130, Margin = new Padding(0, 8, 0, 8),
                BackColor = Color.FromArgb(32, 90, 168), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            enhance.Click += Enhance;
            layout.Controls.Add(enhance, 0, 4);
            layout.Controls.Add(new Label { Text = "Enhanced prompt", AutoSize = true }, 0, 5);
            enhancedPrompt = Editor("EnhancedPrompt", true);
            enhancedPrompt.TabIndex = 3;
            layout.Controls.Add(enhancedPrompt, 0, 6);
            status = new Label { Name = "Status", Text = "Ready. Add your API key in Settings to begin.", Dock = DockStyle.Fill, AutoEllipsis = false, Padding = new Padding(0, 5, 0, 0) };
            layout.Controls.Add(status, 0, 7);
            layout.Controls.Add(new Label { Text = "Enhance sends your text to your cloud provider. Usage charges may apply.", Dock = DockStyle.Fill, Font = new Font(Font.FontFamily, 9), ForeColor = Color.DimGray }, 0, 8);
            Controls.Add(layout);
            Shown += delegate {
                try { if (!String.IsNullOrWhiteSpace(credentials.Load())) status.Text = "Ready. Saved API key available."; }
                catch { status.Text = "The saved API key could not be read. Open Settings to replace it."; }
                roughPrompt.Focus();
            };
            FormClosed += delegate { lifetime.Cancel(); lifetime.Dispose(); };
        }

        private static TextBox Editor(string name, bool readOnly)
        {
            return new TextBox {
                Name = name, AccessibleName = name == "RoughPrompt" ? "Rough prompt" : "Enhanced prompt",
                Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical,
                ReadOnly = readOnly, Dock = DockStyle.Fill, BackColor = Color.White,
                Font = new Font("Segoe UI", 11), MaxLength = 32000
            };
        }

        private void OpenSettings(object sender, EventArgs args)
        {
            using (var dialog = new SettingsForm(credentials))
                if (dialog.ShowDialog(this) == DialogResult.OK) status.Text = "API key saved with Windows protection. Ready.";
        }

        private async void Enhance(object sender, EventArgs args)
        {
            if (String.IsNullOrWhiteSpace(roughPrompt.Text)) { status.Text = "Enter a rough prompt first."; return; }
            enhance.Enabled = settings.Enabled = roughPrompt.Enabled = false;
            enhancedPrompt.Clear();
            status.Text = "Enhancing...";
            try
            {
                string result = await provider.EnhanceAsync(roughPrompt.Text, EnhancementPolicy.Instructions, lifetime.Token);
                if (!IsDisposed)
                {
                    if (String.IsNullOrWhiteSpace(result)) throw new EnhancementException(EnhancementFailure.InvalidResponse);
                    enhancedPrompt.Text = result;
                    status.Text = "Enhanced prompt ready.";
                }
            }
            catch (EnhancementException error) { if (!IsDisposed) status.Text = error.Message; }
            catch (CredentialException error) { if (!IsDisposed) status.Text = error.Message; }
            catch { if (!IsDisposed) status.Text = "Enhancement could not finish. Try again."; }
            finally
            {
                if (!IsDisposed) enhance.Enabled = settings.Enabled = roughPrompt.Enabled = true;
            }
        }
    }
}
