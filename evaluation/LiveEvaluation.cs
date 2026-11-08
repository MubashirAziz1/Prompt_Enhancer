using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using PromptEnhancer;

// Manual, paid evaluation only. Never registered with the controlled test suite.
internal static class LiveEvaluation
{
    [STAThread]
    private static int Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        var credentials = new DpapiCredentialStore(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PromptEnhancer", "openrouter-key.dpapi"));
        string[,] examples = {
            { "writing", "write email boss tomorrow leave personal work short" },
            { "comparison", "compare these two laptops for programming" },
            { "coding", "python function remove duplicate list keep order no external library explain briefly" },
            { "research", "research remote work productivity pros cons use reliable sources separate evidence opinion" },
            { "everyday", "plan easy dinners next week vegetarian cheap 20 minutes no mushrooms" },
            { "ambiguous", "make it better for them" },
            { "complex", "plan migrate team wiki to new platform compare options migration steps risks rollback checklist keep links working no vendor chosen" },
            { "constraints", "rewrite announcement friendly keep exact text \"Launch: TBD\" under 40 words no emojis" },
            { "unresolved-context", "help fix this issue for them keep short" },
            { "restricted-comparison", "compare these two laptops only RAM and storage no recommendations" }
        };
        try
        {
            using (var transport = new UsageTransport())
            using (var client = new HttpClient(transport) { Timeout = TimeSpan.FromSeconds(60), MaxResponseContentBufferSize = 1024 * 1024 })
            using (var form = new MainForm(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials))
            {
                form.Show();
                Application.DoEvents();
                var input = Find<TextBox>(form, "RoughPrompt");
                var enhance = Find<Button>(form, "Enhance");
                var result = Find<TextBox>(form, "EnhancedPrompt");
                var status = Find<Label>(form, "Status");
                Console.WriteLine("Live OpenRouter evaluation: " + OpenRouterProvider.InitialModel);
                Console.WriteLine("Non-sensitive fixed examples; results vary. No clipboard actions or credential writes.");
                for (int index = 0; index < examples.GetLength(0); index++)
                {
                    input.Text = examples[index, 1];
                    transport.Usage = null;
                    var clock = Stopwatch.StartNew();
                    enhance.PerformClick();
                    while (!enhance.Enabled)
                    {
                        if (clock.Elapsed > TimeSpan.FromSeconds(65))
                            throw new Exception("Desktop evaluation exceeded the request timeout.");
                        Application.DoEvents();
                        Thread.Sleep(5);
                    }
                    clock.Stop();
                    Application.DoEvents();
                    // The provider has already validated this text and rejected credential echoes.
                    Console.WriteLine(new JavaScriptSerializer().Serialize(new {
                        category = examples[index, 0], roughPrompt = input.Text,
                        enhancedPrompt = result.Text, elapsedMs = clock.ElapsedMilliseconds,
                        usage = transport.Usage, feedback = status.Text
                    }));
                    if (String.IsNullOrWhiteSpace(result.Text)) return 1;
                }
                form.Close();
            }
            return 0;
        }
        catch
        {
            Console.Error.WriteLine("Live evaluation could not finish. Check the configured account and connectivity; no remote diagnostics are printed.");
            return 1;
        }
    }

    private static T Find<T>(Control form, string name) where T : Control
    {
        return (T)form.Controls.Find(name, true)[0];
    }

    // Observe numeric usage only at the external HTTP boundary; production parsing stays real.
    private sealed class UsageTransport : DelegatingHandler
    {
        public Dictionary<string, object> Usage;

        public UsageTransport() : base(new HttpClientHandler { AllowAutoRedirect = false }) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
        {
            var response = await base.SendAsync(request, cancellation).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var body = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(
                        await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                    object value;
                    if (body.TryGetValue("usage", out value))
                    {
                        var usage = value as Dictionary<string, object>;
                        Usage = new Dictionary<string, object>();
                        foreach (string name in new[] { "prompt_tokens", "completion_tokens", "total_tokens", "cost" })
                            if (usage != null && usage.TryGetValue(name, out value) &&
                                (value is int || value is long || value is decimal || value is double)) Usage[name] = value;
                    }
                }
                catch { Usage = null; } // Missing telemetry must not change enhancement behavior.
            }
            return response;
        }
    }
}
