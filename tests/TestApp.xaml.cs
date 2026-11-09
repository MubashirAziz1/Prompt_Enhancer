using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace PromptEnhancer.WinUI.Tests;

public partial class App : Application
{
    private EditorWindow editor;
    private readonly List<string> checks = [];
    public App() => InitializeComponent();

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Keep the test host alive after the application windows exercise Quit.
        var hostWindow = new Window();
        hostWindow.Activate();
        hostWindow.AppWindow.Hide();
        if (Environment.GetCommandLineArgs() is [_, "--live-evaluation"])
        {
            Environment.Exit(await LiveEvaluation());
            return;
        }
        string directory = Path.Combine(AppContext.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
        int exit = 0;
        ClipboardFixture clipboard = null;
        try
        {
            var arguments = Environment.GetCommandLineArgs();
            if (arguments.Length == 4 && arguments[1] == "--floating-process")
            {
                await FloatingProcess(arguments[2], arguments[3]);
                return;
            }
            Directory.CreateDirectory(directory);
            bool reuse = arguments.Length == 3 && arguments[1] == "--credential-reuse";
            string only = arguments.Length == 3 && arguments[1] == "--only" ? arguments[2] : "All";
            if (!reuse && (only == "All" || only == "OpenRouter"))
            {
                var providerChecks = new List<KeyValuePair<string, Action>>();
                PromptEnhancer.Tests.OpenRouterTests.Register(providerChecks);
                foreach (var check in providerChecks)
                {
                    await Task.Run(check.Value);
                    checks.Add("PASS " + check.Key);
                }
                if (only == "OpenRouter")
                {
                    File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "checks.txt"), checks);
                    Directory.Delete(directory, true);
                    Environment.Exit(0);
                    return;
                }
            }
            clipboard = reuse ? null : new ClipboardFixture();
            if (clipboard != null)
            {
                await clipboard.CaptureText();
                await ClipboardFixture.SetText(ClipboardFixture.Sentinel);
            }
            string path = reuse ? Path.GetFullPath(arguments[2]) : Path.Combine(directory, "key.dpapi");
            Require(path.StartsWith(Path.Combine(AppContext.BaseDirectory, "test-data") + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase), "Only isolated test credentials may be used.");
            var credentials = new DpapiCredentialStore(path);
            if (!reuse) await Probe("write", path, "non-sensitive-fixture-key");
            var transport = new ControlledHttp { ExpectedKey = reuse ? "replacement-fixture-key" : "non-sensitive-fixture-key" };
            using var client = new HttpClient(transport) { Timeout = TimeSpan.FromSeconds(60) };
            editor = new EditorWindow(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials);
            editor.Activate();
            await Task.Delay(400);
            Find<TextBox>("RoughPrompt").Text = "write a short email";
            Invoke("Enhance");
            await Until(() => Find<Button>("Enhance").IsEnabled);
            Require(Find<TextBox>("EnhancedPrompt").Text == "Write a short email.", "Enhance must display the HTTP completion in the actual WinUI editor.");
            Require(Find<Button>("Copy").IsEnabled, "A completed result must be available for Copy.");
            if (!reuse)
            {
                Require(await ClipboardFixture.Text() == ClipboardFixture.Sentinel, "Launching and enhancing must not change the clipboard.");
                checks.Add("PASS Legacy protected key loads and enhances in WinUI with exact HTTP configuration");
                if (only == "All" || only == "Settings")
                {
                    Invoke("Settings");
                    await Until(() => Elements().OfType<PasswordBox>().Any());
                    Require(Find<PasswordBox>("ApiKey").Password == "", "Reopened Settings must never reveal the saved key.");
                    Find<PasswordBox>("ApiKey").Password = "replacement-fixture-key";
                    InvokeDialog("Save");
                    await SettingsClosed();
                    Require(credentials.Load() == "replacement-fixture-key", "Settings must replace the isolated protected credential.");
                    await SettingsCompatibility(credentials, path);
                    checks.Add("PASS Settings preserves blank Save/Cancel, rejects invalid input, replaces safely, and reopens blank");
                    await Probe("read", path, "replacement-fixture-key");
                    await FreshWinUIProcess(path);
                    checks.Add("PASS Replacement works in fresh .NET Framework and WinUI processes");
                }
                Require(await ClipboardFixture.Text() == ClipboardFixture.Sentinel, "Settings and relaunch must not change the clipboard.");
                transport.ExpectedKey = credentials.Load();
                if (only == "All" || only == "Session") await ClearWorkflow(transport);
                if (only == "All" || only == "Workflow")
                {
                    await ClipboardWorkflow(transport);
                    checks.Add("PASS Explicit Paste, native Ctrl+V, busy state, edited Copy, and clipboard retry");
                }
                if (only == "All" || only == "Recovery") await RecoveryWorkflows(directory);
                if (only == "All" || only == "Appearance")
                {
                    await ReplacementWorkflow(credentials);
                    await AppearanceWorkflow();
                }
                if (only == "All" || only == "Session")
                {
                    await FloatingSession(credentials);
                    await FreshFloatingProcesses(path);
                }
                File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "checks.txt"), checks);
            }
        }
        catch (Exception error)
        {
            exit = 1;
            checks.Add("FAIL " + error);
            File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "checks.txt"), checks);
        }
        finally
        {
            if (clipboard != null)
            {
                try { await clipboard.Restore(); checks.Add("PASS Original clipboard restored"); }
                catch (Exception error) { exit = 1; checks.Add("FAIL Clipboard restoration: " + error); }
                File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "checks.txt"), checks);
            }
            editor?.Close();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        GC.KeepAlive(hostWindow);
        Environment.Exit(exit);
    }

    private T Find<T>(string id) where T : FrameworkElement =>
        Elements().OfType<T>().Single(e => AutomationProperties.GetAutomationId(e) == id);

    private IEnumerable<FrameworkElement> Elements()
    {
        var root = (FrameworkElement)editor.Content;
        return Descendants(root).Concat(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot)
            .SelectMany(p => p.Child is FrameworkElement child ? Descendants(child) : []));
    }

    private void InvokeDialog(string label) =>
        ((IInvokeProvider)new ButtonAutomationPeer(Elements().OfType<Button>().Single(b => (string)b.Content == label))
            .GetPattern(PatternInterface.Invoke)).Invoke();

    private Task SettingsClosed() => Until(() => !Elements().OfType<PasswordBox>().Any() && Find<Button>("Settings").IsEnabled);

    private static IEnumerable<FrameworkElement> Descendants(FrameworkElement root)
    {
        yield return root;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            if (VisualTreeHelper.GetChild(root, i) is FrameworkElement child)
                foreach (var item in Descendants(child)) yield return item;
    }

    private void Invoke(string id) =>
        ((IInvokeProvider)new ButtonAutomationPeer(Find<Button>(id)).GetPattern(PatternInterface.Invoke)).Invoke();

    private static async Task Until(Func<bool> done)
    {
        for (int i = 0; i < 200; i++) { if (done()) return; await Task.Delay(25); }
        throw new Exception("Timed out waiting for the WinUI workflow.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}

internal sealed class ControlledHttp : HttpMessageHandler
{
    public string ExpectedKey = "non-sensitive-fixture-key";
    public string ExpectedPrompt = "write a short email";
    public int Requests;
    public Func<CancellationToken, Task<HttpResponseMessage>> Respond = _ => Task.FromResult(Completion("Write a short email."));
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests++;
        using var payload = JsonDocument.Parse(await request.Content.ReadAsStringAsync(cancellationToken));
        var body = payload.RootElement;
        if (request.RequestUri.AbsoluteUri != "https://openrouter.ai/api/v1/chat/completions" ||
            request.Method != HttpMethod.Post || request.Headers.Authorization?.Scheme != "Bearer" ||
            request.Headers.Authorization.Parameter != ExpectedKey ||
            body.GetProperty("model").GetString() != "openai/gpt-4.1" ||
            body.GetProperty("temperature").GetDouble() != 0.2 ||
            body.GetProperty("max_tokens").GetInt32() != 2048 || body.GetProperty("stream").GetBoolean() ||
            body.GetProperty("messages").GetArrayLength() != 2 ||
            body.GetProperty("messages")[0].GetProperty("role").GetString() != "system" ||
            body.GetProperty("messages")[0].GetProperty("content").GetString() != EnhancementPolicy.Instructions ||
            body.GetProperty("messages")[1].GetProperty("role").GetString() != "user" ||
            body.GetProperty("messages")[1].GetProperty("content").GetString() != ExpectedPrompt)
            throw new Exception("The HTTP request must preserve the exact current prompt, complete policy, authentication, and evaluated settings.");
        return await Respond(cancellationToken);
    }

    public static HttpResponseMessage Completion(string text, string reason = "stop") =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new
        {
            choices = new[] { new { finish_reason = reason, message = new { role = "assistant", content = text } } }
        })) };
}
