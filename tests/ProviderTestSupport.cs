using System.Net;
using System.Net.Http;
using System.Text;

namespace PromptEnhancer.Tests;

internal static class TestRunner
{
    public const string TestKey = "non-sensitive-test-key-no-account";
    public const string RoughPrompt = "write email boss tomorrow leave personal work short";

    public static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static string CreateCredentialPath()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "test-key.dpapi");
    }

    public static HttpResponseMessage Completion(string text) =>
        WinUI.Tests.ControlledHttp.Completion(text);

    public static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}

internal sealed class ControlledHttp : HttpMessageHandler
{
    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Respond;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation) =>
        Respond(request, cancellation);
}
