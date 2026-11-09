using Microsoft.UI.Xaml.Controls;
using System.Net;
using System.Net.Http;

namespace PromptEnhancer.WinUI.Tests;

public partial class App
{
    private const string FixtureKey = "non-sensitive-fixture-key";
    private sealed record FailureCase(string Name, string Key, Func<CancellationToken, Task<HttpResponseMessage>> Failure,
        string Guidance, bool CorrectKey = false, bool CorruptBlob = false);

    private async Task RecoveryWorkflows(string directory)
    {
        static Task<HttpResponseMessage> InvalidTransport(CancellationToken _) =>
            throw new Exception("Invalid or missing credentials must not reach the HTTP transport.");
        static Func<CancellationToken, Task<HttpResponseMessage>> Http(HttpStatusCode code, string body) =>
            _ => Task.FromResult(new HttpResponseMessage(code) { Content = new StringContent(body) });
        static Func<CancellationToken, Task<HttpResponseMessage>> Completion(string text, string finish = "stop") =>
            _ => Task.FromResult(ControlledHttp.Completion(text, finish));

        FailureCase[] cases =
        [
            new("Missing credential", null, InvalidTransport, "Settings", true),
            new("Malformed credential", "invalid\r\nkey", InvalidTransport, "Settings", true),
            new("Corrupt protected key", FixtureKey, InvalidTransport, "Settings", true, true),
            new("Rejected credential", "rejected-fixture-key", Http(HttpStatusCode.Unauthorized, "REMOTE " + FixtureKey), "Settings", true),
            new("Forbidden credential", FixtureKey, Http(HttpStatusCode.Forbidden, "REMOTE"), "Settings", true),
            new("Account credits", FixtureKey, Http(HttpStatusCode.PaymentRequired, "REMOTE"), "credits"),
            new("Rate limit", FixtureKey, Http((HttpStatusCode)429, "REMOTE"), "busy"),
            new("Connectivity", FixtureKey, _ => throw new HttpRequestException("REMOTE " + FixtureKey), "could not be reached"),
            new("Timeout", FixtureKey, async cancellation =>
            {
                await Task.Delay(Timeout.Infinite, cancellation);
                return ControlledHttp.Completion("unreachable");
            }, "timed out"),
            new("Provider failure", FixtureKey, Http(HttpStatusCode.ServiceUnavailable, "REMOTE " + FixtureKey), "could not be reached"),
            new("Empty completion", FixtureKey, Completion("  "), "complete"),
            new("Malformed response", FixtureKey, Http(HttpStatusCode.OK, "REMOTE " + FixtureKey), "complete"),
            new("Truncated response", FixtureKey, Completion("Partial rewrite", "length"), "complete"),
            new("Secret echo", FixtureKey, Completion(FixtureKey), "complete"),
            new("Provider error envelope", FixtureKey, Http(HttpStatusCode.OK, "{\"error\":{\"message\":\"REMOTE\"},\"choices\":[]}"), "complete"),
            new("Absent choices", FixtureKey, Http(HttpStatusCode.OK, "{\"choices\":[]}"), "complete"),
            new("Unexpected message role", FixtureKey, Http(HttpStatusCode.OK,
                "{\"choices\":[{\"finish_reason\":\"stop\",\"message\":{\"role\":\"user\",\"content\":\"Source text\"}}]}"), "complete")
        ];
        foreach (var scenario in cases)
        {
            string path = Path.Combine(directory, Guid.NewGuid().ToString("N"), "key.dpapi");
            var credentials = new DpapiCredentialStore(path);
            if (scenario.Key != null) credentials.Save(scenario.Key);
            if (scenario.CorruptBlob) File.WriteAllBytes(path, [1, 2, 3, 4]);
            var transport = new ControlledHttp
            {
                ExpectedKey = scenario.Key,
                ExpectedPrompt = "Keep this draft after failure.",
                Respond = scenario.Failure
            };
            using var client = new HttpClient(transport)
            {
                Timeout = scenario.Name == "Timeout" ? TimeSpan.FromMilliseconds(120) : TimeSpan.FromSeconds(60),
                MaxResponseContentBufferSize = 1024 * 1024
            };
            await OpenEditor(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials);
            var rough = Find<TextBox>("RoughPrompt");
            var result = Find<TextBox>("EnhancedPrompt");
            rough.Text = transport.ExpectedPrompt;
            result.Text = "Stale result that must disappear.";
            Invoke("Enhance");
            await Until(() => Find<Button>("Enhance").IsEnabled && Find<TextBlock>("Status").Text.StartsWith("Enhancement failed."));
            string feedback = Find<TextBlock>("Status").Text;
            Require(rough.Text == "Keep this draft after failure." && result.Text == "" && !Find<Button>("Copy").IsEnabled &&
                (string)Find<Button>("Enhance").Content == "Retry", scenario.Name + ": failure must retain the draft and forbid copying an error.");
            Require(feedback.Contains(scenario.Guidance) && !feedback.Contains("REMOTE") && !feedback.Contains(FixtureKey) &&
                !feedback.Contains(rough.Text), scenario.Name + ": feedback must be safe and explain recovery.");
            if (scenario.CorrectKey)
            {
                Invoke("Settings");
                await Until(() => Elements().OfType<PasswordBox>().Any());
                Require(Find<PasswordBox>("ApiKey").Password == "", "Credential correction must start with a blank field.");
                Find<PasswordBox>("ApiKey").Password = FixtureKey;
                InvokeDialog("Save");
                await SettingsClosed();
            }
            const string revised = "compare \"these\" two laptops\rfor café work \\ research";
            rough.Text = revised;
            transport.ExpectedPrompt = revised;
            transport.ExpectedKey = FixtureKey;
            transport.Respond = _ => Task.FromResult(ControlledHttp.Completion("Compare these two laptops for programming."));
            Invoke("Enhance"); // The same native action now labelled Retry.
            await Until(() => Find<Button>("Enhance").IsEnabled && result.Text == "Compare these two laptops for programming.");
            Require(rough.Text == revised && (string)Find<Button>("Enhance").Content == "Enhance", "Retry must use current input and restore the normal action.");
            result.Text = "Recovered and edited result.";
            await CopyResult();
            Require(await ClipboardFixture.Text() == "Recovered and edited result.", "Recovery must permit copying the edited result.");
            checks.Add("PASS " + scenario.Name + " → retained draft, safe feedback, current-input Retry, edited Copy");
        }
    }

    private async Task OpenEditor(IEnhancementProvider provider, ICredentialStore credentials)
    {
        var previous = editor;
        editor = new EditorWindow(provider, credentials);
        editor.Activate();
        previous?.Close();
        await Until(() => ((Microsoft.UI.Xaml.FrameworkElement)editor.Content).ActualHeight > 0);
        await Task.Delay(50);
        NativeDesktop.Activate(editor);
    }

    private async Task ReplacementWorkflow(ICredentialStore credentials)
    {
        await OpenEditor(new ReplacementProvider(), credentials);
        Find<TextBox>("RoughPrompt").Text = "Improve it for them.";
        Invoke("Enhance");
        await Until(() => Find<Button>("Enhance").IsEnabled && Find<TextBox>("EnhancedPrompt").Text == "Replacement provider result.");
        checks.Add("PASS Replacement provider drives the same WinUI workflow and receives the complete provider-neutral policy");
    }

    private sealed class ReplacementProvider : IEnhancementProvider
    {
        public Task<string> EnhanceAsync(string roughPrompt, string policy, CancellationToken cancellation)
        {
            Require(roughPrompt == "Improve it for them." && policy == EnhancementPolicy.Instructions,
                "Replacement providers must receive the exact input and provider-neutral policy through the existing contract.");
            return Task.FromResult("Replacement provider result.");
        }
    }
}
