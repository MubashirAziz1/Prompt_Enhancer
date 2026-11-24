using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            foreach (var status in new[] { HttpStatusCode.NotFound, HttpStatusCode.RequestTimeout, (HttpStatusCode)422,
                (HttpStatusCode)429, HttpStatusCode.BadGateway, HttpStatusCode.ServiceUnavailable })
                tests.Add(new KeyValuePair<string, Action>("OpenRouter respects the three-model limit and preserves all free priorities after HTTP " + (int)status,
                    () => BoundedFreeModelFallback(status)));
            tests.Add(new KeyValuePair<string, Action>("OpenRouter shares the timeout budget across fallback groups", FallbackTimeout));
            tests.Add(new KeyValuePair<string, Action>("OpenRouter cancellation stops fallback and preserves the caller token", CanceledFallback));
            tests.Add(new KeyValuePair<string, Action>("OpenRouter sends prioritized free models with authenticated policy and exact rough prompt", AuthenticatedRequest));
            tests.Add(new KeyValuePair<string, Action>("OpenRouter authentication failure never exposes key or remote diagnostics", () =>
                SafeFailure(HttpStatusCode.Unauthorized, "{\"error\":{\"message\":\"" + TestRunner.TestKey + " DO-NOT-RENDER-REMOTE-DETAILS\"}}")));
            tests.Add(new KeyValuePair<string, Action>("OpenRouter rate limit never exposes remote diagnostics", () =>
                SafeFailure((HttpStatusCode)429, "DO-NOT-RENDER-REMOTE-DETAILS")));
            foreach (var status in new[] { HttpStatusCode.Forbidden, HttpStatusCode.PaymentRequired, HttpStatusCode.BadRequest })
                tests.Add(new KeyValuePair<string, Action>("OpenRouter stops safely without fallback after HTTP " + (int)status,
                    () => SafeFailure(status, "DO-NOT-RENDER-REMOTE-DETAILS")));
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

        private static void BoundedFreeModelFallback(HttpStatusCode firstFailure)
        {
            string path = TestRunner.CreateCredentialPath();
            var requests = new List<JsonElement>();
            var authenticated = new List<bool>();
            try
            {
                var credentials = new DpapiCredentialStore(path);
                credentials.Save(TestRunner.TestKey);
                using var transport = new ControlledHttp { Respond = async (request, cancellation) => {
                    using var document = JsonDocument.Parse(await request.Content.ReadAsStringAsync(cancellation));
                    var body = document.RootElement;
                    requests.Add(body.Clone());
                    authenticated.Add(request.Headers.Authorization?.Scheme == "Bearer" &&
                        request.Headers.Authorization.Parameter == TestRunner.TestKey);
                    if (body.GetProperty("models").GetArrayLength() > 3)
                        return TestRunner.Json(HttpStatusCode.BadRequest, "{\"error\":{\"code\":400,\"message\":\"models array must have no more than 3 items\"}}");
                    return requests.Count == 1
                        ? TestRunner.Json(firstFailure, "DO-NOT-RENDER-REMOTE-DETAILS")
                        : TestRunner.Completion("Write a short email to my manager requesting leave tomorrow for personal reasons.");
                } };
                using var client = new HttpClient(transport);
                string result = new OpenRouterProvider(client, credentials)
                    .EnhanceAsync(TestRunner.RoughPrompt, EnhancementPolicy.Instructions, CancellationToken.None).GetAwaiter().GetResult();
                TestRunner.Require(result == "Write a short email to my manager requesting leave tomorrow for personal reasons.",
                    "A bounded fallback request must return the valid enhanced prompt.");
                TestRunner.Require(requests.Count == 2 && authenticated.All(value => value),
                    "Both ordered groups must use the saved credential.");
                TestRunner.Require(TestRunner.HasFirstFreeModelGroup(requests[0]) && TestRunner.HasFinalFreeModelGroup(requests[1]),
                    "Preserve all six priorities across two requests of at most three candidates.");
                foreach (var body in requests)
                    TestRunner.Require(!body.TryGetProperty("model", out _) && body.GetProperty("messages")[0].GetProperty("content").GetString() == EnhancementPolicy.Instructions &&
                        body.GetProperty("messages")[1].GetProperty("content").GetString() == TestRunner.RoughPrompt,
                        "Every group must preserve the exact prompt and policy without a single-model override.");
            }
            finally { Directory.Delete(Path.GetDirectoryName(path), true); }
        }

        private static void FallbackTimeout()
        {
            string path = TestRunner.CreateCredentialPath();
            int requests = 0;
            try
            {
                var credentials = new DpapiCredentialStore(path);
                credentials.Save(TestRunner.TestKey);
                using var transport = new ControlledHttp { Respond = async (request, cancellation) => {
                    int attempt = ++requests;
                    await Task.Delay(300, cancellation);
                    return attempt == 1 ? TestRunner.Json(HttpStatusCode.ServiceUnavailable, "REMOTE")
                        : TestRunner.Completion("This would arrive after the total timeout budget.");
                } };
                using var client = new HttpClient(transport) { Timeout = TimeSpan.FromMilliseconds(500) };
                try
                {
                    new OpenRouterProvider(client, credentials).EnhanceAsync(TestRunner.RoughPrompt, EnhancementPolicy.Instructions,
                        CancellationToken.None).GetAwaiter().GetResult();
                    throw new Exception("Fallback must not restart the timeout budget.");
                }
                catch (EnhancementException failure)
                {
                    TestRunner.Require(requests == 2 && failure.Message == new EnhancementException(EnhancementFailure.Timeout).Message,
                        "The combined duration must time out even when each request would fit its own budget.");
                }
            }
            finally { Directory.Delete(Path.GetDirectoryName(path), true); }
        }

        private static void CanceledFallback()
        {
            string path = TestRunner.CreateCredentialPath();
            int requests = 0;
            try
            {
                var credentials = new DpapiCredentialStore(path);
                credentials.Save(TestRunner.TestKey);
                using var stop = new CancellationTokenSource();
                using var transport = new ControlledHttp { Respond = (request, cancellation) => {
                    requests++;
                    stop.Cancel();
                    return Task.FromResult(TestRunner.Json(HttpStatusCode.ServiceUnavailable, "REMOTE"));
                } };
                using var client = new HttpClient(transport);
                try
                {
                    new OpenRouterProvider(client, credentials).EnhanceAsync(TestRunner.RoughPrompt, EnhancementPolicy.Instructions,
                        stop.Token).GetAwaiter().GetResult();
                    throw new Exception("A canceled enhancement must not reach another routing group.");
                }
                catch (OperationCanceledException failure)
                {
                    TestRunner.Require(requests == 1 && failure.CancellationToken == stop.Token,
                        "Cancellation must stop fallback and preserve the caller's token.");
                }
            }
            finally { Directory.Delete(Path.GetDirectoryName(path), true); }
        }

        private static void SafeFailure(HttpStatusCode status, string body)
        {
            string path = TestRunner.CreateCredentialPath();
            try
            {
                var credentials = new DpapiCredentialStore(path);
                credentials.Save(TestRunner.TestKey);
                int requests = 0;
                using (var transport = new ControlledHttp { Respond = (request, cancellation) => {
                    requests++;
                    return Task.FromResult(TestRunner.Json(status, body));
                } })
                using (var client = new HttpClient(transport))
                {
                    try
                    {
                        new OpenRouterProvider(client, credentials)
                            .EnhanceAsync(TestRunner.RoughPrompt, EnhancementPolicy.Instructions, CancellationToken.None).GetAwaiter().GetResult();
                        throw new Exception("The invalid cloud response must fail rather than becoming an enhanced prompt.");
                    }
                    catch (EnhancementException error)
                    {
                        TestRunner.Require(requests == (status == (HttpStatusCode)429 ? 2 : 1),
                            "Only routing or capacity errors may advance to the remaining candidates.");
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
                    TestRunner.Require(TestRunner.HasFirstFreeModelGroup(body),
                        "Send the first three free preferences in priority order without a single-model override.");
                    TestRunner.Require(!body.GetProperty("stream").GetBoolean(), "Use non-streaming completions.");
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
                    string result = new OpenRouterProvider(client, credentials)
                        .EnhanceAsync(prompt, policy, CancellationToken.None).GetAwaiter().GetResult();
                    TestRunner.Require(result == "Compare these two laptops for programming.", "Return the cloud's enhanced prompt.");
                }
            }
            finally { Directory.Delete(Path.GetDirectoryName(path), true); }
        }
    }
}
