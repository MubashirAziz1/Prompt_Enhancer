using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace PromptEnhancer.Tests
{
    internal static class CloudRecoveryWorkflowTests
    {
        public static void Register(List<KeyValuePair<string, Action>> tests)
        {
            tests.Add(new KeyValuePair<string, Action>("Missing credential -> Settings -> retry uses the current rough prompt", MissingCredentialRecovery));
            tests.Add(new KeyValuePair<string, Action>("Rejected credential -> Settings -> retry authenticates with the corrected key", () =>
                FailureThenRecovery("rejected-test-key", cancellation => Task.FromResult(TestRunner.Json(HttpStatusCode.Unauthorized,
                    "{\"error\":{\"message\":\"" + TestRunner.TestKey + " " + TestRunner.RoughPrompt + "\"}}")), true)));
            tests.Add(new KeyValuePair<string, Action>("Malformed saved credential -> Settings -> successful retry", () =>
                FailureThenRecovery("invalid\r\ncredential", cancellation => {
                    throw new Exception("Malformed credentials must be rejected before HTTP submission.");
                }, true)));
            tests.Add(new KeyValuePair<string, Action>("Connectivity loss and timeout retain input and recover with distinct feedback", ConnectivityAndTimeoutRecovery));
            tests.Add(new KeyValuePair<string, Action>("Forbidden credential -> Settings -> successful retry", () =>
                FailureThenRecovery("forbidden-test-key", cancellation => Task.FromResult(TestRunner.Json(HttpStatusCode.Forbidden,
                    "REMOTE-DETAILS " + TestRunner.TestKey)), true)));
            tests.Add(new KeyValuePair<string, Action>("Provider error -> successful retry retains the current rough prompt", () =>
                FailureThenRecovery(TestRunner.TestKey, cancellation => Task.FromResult(TestRunner.Json(HttpStatusCode.ServiceUnavailable,
                    "REMOTE-DETAILS " + TestRunner.TestKey + " " + TestRunner.RoughPrompt)), false)));
            tests.Add(new KeyValuePair<string, Action>("Insufficient credits -> successful retry", () =>
                FailureThenRecovery(TestRunner.TestKey, cancellation => Task.FromResult(TestRunner.Json(HttpStatusCode.PaymentRequired,
                    "REMOTE-DETAILS")), false)));
            tests.Add(new KeyValuePair<string, Action>("Rate limit -> successful retry", () =>
                FailureThenRecovery(TestRunner.TestKey, cancellation => Task.FromResult(TestRunner.Json((HttpStatusCode)429,
                    "REMOTE-DETAILS")), false)));
            tests.Add(new KeyValuePair<string, Action>("Empty completion -> successful retry without a copyable error", () =>
                FailureThenRecovery(TestRunner.TestKey, cancellation => Task.FromResult(TestRunner.Completion("   ")), false)));
            tests.Add(new KeyValuePair<string, Action>("Malformed response -> successful retry without a copyable error", () =>
                FailureThenRecovery(TestRunner.TestKey, cancellation => Task.FromResult(TestRunner.Json(HttpStatusCode.OK,
                    "REMOTE-DETAILS " + TestRunner.TestKey + " " + TestRunner.RoughPrompt)), false)));
            tests.Add(new KeyValuePair<string, Action>("Truncated completion -> successful retry without a partial result", () =>
                FailureThenRecovery(TestRunner.TestKey, cancellation => Task.FromResult(TestRunner.Json(HttpStatusCode.OK,
                    "{\"choices\":[{\"finish_reason\":\"length\",\"message\":{\"role\":\"assistant\",\"content\":\"Unfinished rewrite\"}}]}")), false)));
            tests.Add(new KeyValuePair<string, Action>("Error in successful HTTP response -> successful retry", () =>
                FailureThenRecovery(TestRunner.TestKey, cancellation => Task.FromResult(TestRunner.Json(HttpStatusCode.OK,
                    "{\"error\":{\"message\":\"REMOTE-DETAILS\"},\"choices\":[]}")), false)));
            tests.Add(new KeyValuePair<string, Action>("Credential echo -> successful retry without exposing the secret", () =>
                FailureThenRecovery(TestRunner.TestKey, cancellation => Task.FromResult(TestRunner.Completion(TestRunner.TestKey)), false)));
        }

        private static void ConnectivityAndTimeoutRecovery()
        {
            string disconnected = FailureThenRecovery(TestRunner.TestKey, cancellation => {
                throw new HttpRequestException("REMOTE-DETAILS " + TestRunner.TestKey + " " + TestRunner.RoughPrompt);
            }, false);
            string timedOut = FailureThenRecovery(TestRunner.TestKey, async cancellation => {
                await Task.Delay(Timeout.Infinite, cancellation);
                return TestRunner.Completion("Unreachable after timeout");
            }, false);
            TestRunner.Require(disconnected != timedOut,
                "A timeout must be explained distinctly from a connectivity failure without depending on exact wording.");
        }

        private static string FailureThenRecovery(string savedKey,
            Func<CancellationToken, Task<HttpResponseMessage>> failure, bool credentialFailure)
        {
            string path = TestRunner.CreateCredentialPath();
            const string revised = "compare two laptops for programming";
            const string result = "Compare the two laptops for programming.";
            bool recovering = false;
            bool retried = false;
            try
            {
                var credentials = new DpapiCredentialStore(path);
                if (savedKey != null) credentials.Save(savedKey);
                using (var transport = new ControlledHttp { Respond = async (request, cancellation) => {
                    if (!recovering) return await failure(cancellation);
                    retried = true;
                    TestRunner.Require(request.Headers.Authorization.Parameter == TestRunner.TestKey,
                        "Recovery must authenticate with the current saved credential.");
                    var body = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(await request.Content.ReadAsStringAsync());
                    var messages = (System.Collections.ArrayList)body["messages"];
                    TestRunner.Require((string)((Dictionary<string, object>)messages[1])["content"] == revised,
                        "Retry must send the current rough prompt.");
                    return TestRunner.Completion(result);
                } })
                using (var client = new HttpClient(transport) { Timeout = TimeSpan.FromMilliseconds(100) })
                using (var window = new MainForm(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials))
                {
                    window.Show();
                    Application.DoEvents();
                    var input = TestRunner.Control<TextBox>(window, "RoughPrompt");
                    var output = TestRunner.Control<TextBox>(window, "EnhancedPrompt");
                    var enhance = TestRunner.Control<Button>(window, "Enhance");
                    var status = TestRunner.Control<Label>(window, "Status");
                    input.Text = TestRunner.RoughPrompt;
                    enhance.PerformClick();
                    TestRunner.PumpUntil(() => enhance.Enabled);
                    TestRunner.Require(input.Text == TestRunner.RoughPrompt && input.Enabled && output.Text == "" &&
                        !TestRunner.Control<Button>(window, "Copy").Enabled && enhance.Text == "Retry",
                        "Failure must retain the editable rough prompt, offer Retry, and leave no copyable error result.");
                    TestRunner.Require(status.Text.Length > 0 && (savedKey == null || !status.Text.Contains(savedKey)) &&
                        !status.Text.Contains(TestRunner.TestKey) && !status.Text.Contains(TestRunner.RoughPrompt) &&
                        !status.Text.Contains("REMOTE-DETAILS"),
                        "Failure feedback must be separate from results and omit secrets, prompt contents, and remote diagnostics.");
                    TestRunner.Require(TestRunner.Control<Button>(window, "Settings").Enabled,
                        "Settings must remain available after failure.");
                    string failureFeedback = status.Text;
                    if (credentialFailure)
                    {
                        TestRunner.Require(status.Text.Contains("Settings"), "Credential failure must direct the user to Settings.");
                        DesktopWorkflowTests.SaveThroughSettings(window, TestRunner.TestKey);
                        TestRunner.Require(input.Text == TestRunner.RoughPrompt, "Correcting credentials must preserve the rough prompt.");
                    }
                    recovering = true;
                    input.Text = revised;
                    enhance.PerformClick();
                    TestRunner.PumpUntil(() => enhance.Enabled);
                    TestRunner.Require(retried && input.Text == revised && output.Text == result &&
                        TestRunner.Control<Button>(window, "Copy").Enabled && enhance.Text == "Enhance",
                        "Retry must display the successful enhanced prompt after recovery.");
                    return failureFeedback;
                }
            }
            finally { Directory.Delete(Path.GetDirectoryName(path), true); }
        }

        private static void MissingCredentialRecovery()
        {
            bool submitted = false;
            FailureThenRecovery(null, cancellation => {
                submitted = true;
                throw new Exception("Missing credentials must not be submitted.");
            }, true);
            TestRunner.Require(!submitted, "Missing credentials must be rejected before any HTTP request.");
        }
    }
}
