using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace PromptEnhancer.Tests
{
    internal static class OpenRouterTests
    {
        public static void Register(List<KeyValuePair<string, Action>> tests)
        {
            tests.Add(new KeyValuePair<string, Action>("OpenRouter sends authenticated policy and exact rough prompt", AuthenticatedRequest));
            tests.Add(new KeyValuePair<string, Action>("OpenRouter authentication failure never exposes key or remote diagnostics", () =>
                SafeFailure(HttpStatusCode.Unauthorized, "{\"error\":{\"message\":\"" + TestRunner.TestKey + " DO-NOT-RENDER-REMOTE-DETAILS\"}}")));
            tests.Add(new KeyValuePair<string, Action>("OpenRouter rate limit never exposes remote diagnostics", () =>
                SafeFailure((HttpStatusCode)429, "DO-NOT-RENDER-REMOTE-DETAILS")));
            tests.Add(new KeyValuePair<string, Action>("OpenRouter refuses malformed JSON", () => SafeFailure(HttpStatusCode.OK, "not JSON")));
            tests.Add(new KeyValuePair<string, Action>("OpenRouter refuses an empty completion", () =>
                SafeFailure(HttpStatusCode.OK, "{\"choices\":[{\"finish_reason\":\"stop\",\"message\":{\"role\":\"assistant\",\"content\":\" \"}}]}")));
            tests.Add(new KeyValuePair<string, Action>("OpenRouter refuses truncated completions", () =>
                SafeFailure(HttpStatusCode.OK, "{\"choices\":[{\"finish_reason\":\"length\",\"message\":{\"role\":\"assistant\",\"content\":\"Partial rewrite\"}}]}")));
            tests.Add(new KeyValuePair<string, Action>("OpenRouter refuses an error inside a successful HTTP response", () =>
                SafeFailure(HttpStatusCode.OK, "{\"error\":{\"message\":\"DO-NOT-RENDER-REMOTE-DETAILS\"},\"choices\":[]}")));
            tests.Add(new KeyValuePair<string, Action>("OpenRouter refuses responses that echo the authentication secret", () =>
                SafeFailure(HttpStatusCode.OK, "{\"choices\":[{\"finish_reason\":\"stop\",\"message\":{\"role\":\"assistant\",\"content\":\"" + TestRunner.TestKey + "\"}}]}")));
            tests.Add(new KeyValuePair<string, Action>("OpenRouter refuses an echoed user message instead of an assistant completion", () =>
                SafeFailure(HttpStatusCode.OK, "{\"choices\":[{\"finish_reason\":\"stop\",\"message\":{\"role\":\"user\",\"content\":\"rough text\"}}]}")));
        }

        private static void SafeFailure(HttpStatusCode status, string body)
        {
            string path = TestRunner.CreateCredentialPath();
            try
            {
                var credentials = new DpapiCredentialStore(path);
                credentials.Save(TestRunner.TestKey);
                using (var transport = new ControlledHttp { Respond = (request, cancellation) => Task.FromResult(TestRunner.Json(status, body)) })
                using (var client = new HttpClient(transport))
                {
                    try
                    {
                        new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel)
                            .EnhanceAsync(TestRunner.RoughPrompt, EnhancementPolicy.Instructions, CancellationToken.None).GetAwaiter().GetResult();
                        throw new Exception("The invalid cloud response must fail rather than becoming an enhanced prompt.");
                    }
                    catch (EnhancementException error)
                    {
                        TestRunner.Require(!error.ToString().Contains(TestRunner.TestKey) &&
                            !error.ToString().Contains("DO-NOT-RENDER-REMOTE-DETAILS") && error.InnerException == null,
                            "Provider failures must expose only safe application-owned messages.");
                    }
                }
            }
            finally { Directory.Delete(Path.GetDirectoryName(path), true); }
        }

        private static void AuthenticatedRequest()
        {
            string path = TestRunner.CreateCredentialPath();
            const string prompt = "compare \"these\" two laptops\nfor programming & caf\u00e9 work \\ research";
            const string policy = "Rewrite in English without inventing facts.";
            try
            {
                var credentials = new DpapiCredentialStore(path);
                credentials.Save(TestRunner.TestKey);
                using (var transport = new ControlledHttp { Respond = async (request, cancellation) => {
                    TestRunner.Require(request.Method == HttpMethod.Post &&
                        request.RequestUri.AbsoluteUri == "https://openrouter.ai/api/v1/chat/completions", "Use the OpenRouter chat completion endpoint.");
                    TestRunner.Require(request.Headers.Authorization.Scheme == "Bearer" &&
                        request.Headers.Authorization.Parameter == TestRunner.TestKey, "Authenticate using the saved key.");
                    TestRunner.Require(request.Content.Headers.ContentType.MediaType == "application/json", "Send JSON content.");
                    using var document = JsonDocument.Parse(await request.Content.ReadAsStringAsync()); var body = document.RootElement;
                    TestRunner.Require(body.GetProperty("model").GetString() == "openai/gpt-4.1" && !body.GetProperty("stream").GetBoolean(), "Use the selected model without streaming.");
                    var messages = body.GetProperty("messages");
                    TestRunner.Require(messages.GetArrayLength() == 2, "Send only the policy and submitted prompt, without history.");
                    var system = messages[0];
                    var user = messages[1];
                    TestRunner.Require(system.GetProperty("role").GetString() == "system" && system.GetProperty("content").GetString() == policy,
                        "The enhancement policy must be a separate system message.");
                    TestRunner.Require(user.GetProperty("role").GetString() == "user" && user.GetProperty("content").GetString() == prompt,
                        "Preserve all submitted text, including quotes, newlines, and Unicode.");
                    TestRunner.Require(!body.TryGetProperty("api_key", out _), "The credential belongs in authentication, not the request body.");
                    return TestRunner.Completion("Compare these two laptops for programming.");
                } })
                using (var client = new HttpClient(transport))
                {
                    string result = new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel)
                        .EnhanceAsync(prompt, policy, CancellationToken.None).GetAwaiter().GetResult();
                    TestRunner.Require(result == "Compare these two laptops for programming.", "Return the cloud's enhanced prompt.");
                }
            }
            finally { Directory.Delete(Path.GetDirectoryName(path), true); }
        }
    }
}
