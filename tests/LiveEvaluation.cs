using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace PromptEnhancer.WinUI.Tests;

public partial class App
{
    // Manual, paid evaluation only. Never included in the controlled checks.
    private async Task<int> LiveEvaluation()
    {
        var credentials = new DpapiCredentialStore(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PromptEnhancer", "openrouter-key.dpapi"));
        (string Category, string Prompt)[] examples =
        [
            ("writing", "write email boss tomorrow leave personal work short"),
            ("comparison", "compare these two laptops for programming"),
            ("coding", "python function remove duplicate list keep order no external library explain briefly"),
            ("research", "research remote work productivity pros cons use reliable sources separate evidence opinion"),
            ("everyday", "plan easy dinners next week vegetarian cheap 20 minutes no mushrooms"),
            ("ambiguous", "make it better for them"),
            ("complex", "plan migrate team wiki to new platform compare options migration steps risks rollback checklist keep links working no vendor chosen"),
            ("constraints", "rewrite announcement friendly keep exact text \"Launch: TBD\" under 40 words no emojis"),
            ("unresolved-context", "help fix this issue for them keep short"),
            ("restricted-comparison", "compare these two laptops only RAM and storage no recommendations")
        ];
        try
        {
            using var transport = new UsageTransport();
            using var client = new HttpClient(transport) { Timeout = TimeSpan.FromSeconds(60), MaxResponseContentBufferSize = 1024 * 1024 };
            editor = new EditorWindow(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials);
            editor.Activate();
            await Task.Delay(400);
            Console.WriteLine("Live OpenRouter evaluation: " + OpenRouterProvider.InitialModel);
            Console.WriteLine("Non-sensitive fixed examples; results vary. No clipboard actions or credential writes.");
            foreach (var example in examples)
            {
                Find<TextBox>("RoughPrompt").Text = example.Prompt;
                transport.Usage = null;
                var clock = Stopwatch.StartNew();
                Invoke("Enhance");
                while (!Find<Button>("Enhance").IsEnabled)
                {
                    if (clock.Elapsed > TimeSpan.FromSeconds(65)) throw new TimeoutException();
                    await Task.Delay(20);
                }
                clock.Stop();
                var result = Find<TextBox>("EnhancedPrompt").Text;
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    category = example.Category, roughPrompt = example.Prompt,
                    enhancedPrompt = result, elapsedMs = clock.ElapsedMilliseconds,
                    usage = transport.Usage, feedback = Find<TextBlock>("Status").Text
                }));
                if (string.IsNullOrWhiteSpace(result)) return 1;
            }
            return 0;
        }
        catch
        {
            Console.Error.WriteLine("Live evaluation could not finish. Check the configured account and connectivity; no remote diagnostics are printed.");
            return 1;
        }
        finally { editor?.Close(); }
    }

    private sealed class UsageTransport() : DelegatingHandler(new HttpClientHandler { AllowAutoRedirect = false })
    {
        public Dictionary<string, JsonElement> Usage;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
        {
            var response = await base.SendAsync(request, cancellation).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false));
                    if (body.RootElement.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
                    {
                        Usage = [];
                        foreach (string name in new[] { "prompt_tokens", "completion_tokens", "total_tokens", "cost" })
                            if (usage.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number)
                                Usage[name] = value.Clone();
                    }
                }
                catch { Usage = null; }
            }
            return response;
        }
    }
}
