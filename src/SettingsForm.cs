using System;
using System.Drawing;
using System.Windows.Forms;

namespace PromptEnhancer
{
    public sealed class SettingsForm : Form
    {
        private readonly TextBox apiKey;
        private readonly Label status;

        public SettingsForm(ICredentialStore credentials)
        {
            Text = "Settings";
            Name = "SettingsDialog";
            Font = new Font("Segoe UI", 10);
            ClientSize = new Size(520, 270);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 5 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.Controls.Add(new Label { Text = "&OpenRouter API key", AutoSize = true }, 0, 0);
            apiKey = new TextBox { Name = "ApiKey", AccessibleName = "OpenRouter API key", UseSystemPasswordChar = true, Dock = DockStyle.Top, MaxLength = 4096 };
            layout.Controls.Add(apiKey, 0, 1);
            layout.Controls.Add(new Label { Text = "Stored with Windows protection for your Windows account.\nLeave blank to keep the saved key.", Dock = DockStyle.Fill }, 0, 2);
            status = new Label { Name = "CredentialStatus", Text = "No key saved yet.", Dock = DockStyle.Fill };
            try { if (!String.IsNullOrWhiteSpace(credentials.Load())) status.Text = "A protected key is saved. Enter a key to replace it."; }
            catch { status.Text = "Saved key unavailable. Enter a key to replace it."; }
            layout.Controls.Add(status, 0, 3);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var cancel = new Button { Name = "Cancel", Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            var save = new Button { Name = "SaveKey", Text = "Save", AccessibleName = "Save API key", AutoSize = true };
            save.Click += delegate {
                if (String.IsNullOrWhiteSpace(apiKey.Text)) { DialogResult = DialogResult.Cancel; return; }
                string key = apiKey.Text.Trim();
                foreach (char character in key)
                    if (Char.IsWhiteSpace(character) || Char.IsControl(character)) { status.Text = "Enter a key without spaces or line breaks."; return; }
                try
                {
                    credentials.Save(key);
                    apiKey.Clear();
                    DialogResult = DialogResult.OK;
                }
                catch { status.Text = "The API key could not be saved with Windows protection. Try again."; }
            };
            actions.Controls.Add(cancel);
            actions.Controls.Add(save);
            layout.Controls.Add(actions, 0, 4);
            Controls.Add(layout);
            AcceptButton = save;
            CancelButton = cancel;
            FormClosed += delegate { apiKey.Clear(); };
        }
    }
}
