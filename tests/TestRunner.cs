using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PromptEnhancer.Tests
{
    internal static class TestRunner
    {
        public const string TestKey = "non-sensitive-test-key-no-account";
        public const string RoughPrompt = "write email boss tomorrow leave personal work short";
        public const string EnhancedPrompt = "Write a short email to my manager requesting leave tomorrow for personal reasons.";

        [STAThread]
        public static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 1 && args[0] == "--companion-editor")
            {
                CompanionEditor.Run();
                return 0;
            }
            if (args.Length == 2 && args[0] == "--relaunch-workflow")
            {
                try { DesktopWorkflowTests.UseSavedKeyAfterRelaunch(args[1]); return 0; }
                catch { Console.Error.WriteLine("Relaunched workflow failed."); return 1; }
            }
            string suite = args.Length == 0 ? "All" : args[0];
            var tests = new List<KeyValuePair<string, Action>>();
            if (suite == "All" || suite == "DesktopWorkflow") DesktopWorkflowTests.Register(tests);
            if (suite == "All" || suite == "OpenRouter") OpenRouterTests.Register(tests);
            if (tests.Count == 0) { Console.Error.WriteLine("Unknown test suite."); return 1; }
            int failed = 0;
            foreach (var test in tests)
            {
                try { test.Value(); Console.WriteLine("PASS " + test.Key); }
                catch (Exception error) { failed++; Console.Error.WriteLine("FAIL " + test.Key + ": " + error.Message); }
            }
            Console.WriteLine("Checks: " + tests.Count + ", passed: " + (tests.Count - failed) + ", failed: " + failed);
            return failed == 0 ? 0 : 1;
        }

        public static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        public static T Control<T>(Control root, string name) where T : Control
        {
            var matches = root.Controls.Find(name, true);
            Require(matches.Length == 1, "Expected one visible control named " + name);
            return (T)matches[0];
        }

        public static void PumpUntil(Func<bool> finished)
        {
            var clock = Stopwatch.StartNew();
            while (!finished())
            {
                if (clock.Elapsed > TimeSpan.FromSeconds(8)) throw new Exception("Timed out waiting for desktop workflow.");
                Application.DoEvents();
                Thread.Sleep(5);
            }
            Application.DoEvents();
        }

        public static string CreateCredentialPath()
        {
            string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, "test-key.dpapi");
        }

        public static HttpResponseMessage Completion(string text)
        {
            return Json(HttpStatusCode.OK, "{\"choices\":[{\"finish_reason\":\"stop\",\"message\":{\"role\":\"assistant\",\"content\":\"" + text + "\"}}]}");
        }

        public static HttpResponseMessage Json(HttpStatusCode status, string body)
        {
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    // The only HTTP double is the external transport. Production serialization and parsing stay real.
    internal sealed class ControlledHttp : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Respond;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
        {
            return Respond(request, cancellation);
        }
    }
}
