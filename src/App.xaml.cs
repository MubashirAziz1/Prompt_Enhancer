using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Net.Http;
using System.Runtime.InteropServices;

namespace PromptEnhancer.WinUI;

public partial class App : Application
{
    // Per-session names: one floating button and one notification icon per Windows user session.
    private const string InstanceName = @"Local\PromptEnhancer.WinUI.Instance";
    private const string ShowName = @"Local\PromptEnhancer.WinUI.Show";
    private FloatingExperience experience;
    private HttpClient client;
    private Mutex instance;
    private EventWaitHandle showSignal;
    private RegisteredWaitHandle showWait;
    private bool failed;

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, args) =>
        {
            // Async handlers report their own failures; this is the last resort.
            args.Handled = true;
            if (failed) return;
            failed = true;
            Report("Prompt Enhancer hit an unexpected problem and will close. Reopen it to try again.");
            Shutdown();
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            instance = new Mutex(false, InstanceName, out bool first);
            showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, ShowName);
            if (!first)
            {
                // Already running: bring its floating button back instead of starting a duplicate.
                showSignal.Set();
                Shutdown();
                return;
            }
            var credentials = new DpapiCredentialStore(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PromptEnhancer", "openrouter-key.dpapi"));
            client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            {
                Timeout = TimeSpan.FromSeconds(60),
                MaxResponseContentBufferSize = 1024 * 1024
            };
            var editor = new EditorWindow(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials);
            experience = new FloatingExperience(editor, new LauncherWindow(), Shutdown);
            experience.Start();
            var queue = DispatcherQueue.GetForCurrentThread();
            showWait = ThreadPool.RegisterWaitForSingleObject(showSignal,
                (_, _) => queue.TryEnqueue(() => experience?.Restore()), null, Timeout.Infinite, false);
        }
        catch
        {
            failed = true;
            Report("Prompt Enhancer could not start. Check that Windows is signed in normally and try again.");
            try { experience?.Quit(); } catch { }
            Shutdown();
        }
    }

    private void Shutdown()
    {
        showWait?.Unregister(null);
        showWait = null;
        client?.Dispose();
        client = null;
        showSignal?.Dispose();
        showSignal = null;
        instance?.Dispose();
        instance = null;
        Exit();
    }

    private static void Report(string message) => MessageBox(0, message, "Prompt Enhancer", 0x10);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int MessageBox(nint window, string text, string caption, uint type);
}
