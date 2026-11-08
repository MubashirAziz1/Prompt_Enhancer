using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows.Forms;

namespace PromptEnhancer
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate {
                MessageBox.Show("The operation could not finish. Reopen the application to try again.", "Prompt Enhancer");
            };
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PromptEnhancer", "openrouter-key.dpapi");
                var credentials = new DpapiCredentialStore(path);
                // Never forward Authorization to a redirected host.
                using (var handler = new HttpClientHandler { AllowAutoRedirect = false })
                using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60), MaxResponseContentBufferSize = 1024 * 1024 })
                using (var window = new MainForm(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials))
                    Application.Run(window);
            }
            catch
            {
                MessageBox.Show("Prompt Enhancer could not start. Check your Windows .NET Framework installation.", "Prompt Enhancer");
            }
        }
    }
}
