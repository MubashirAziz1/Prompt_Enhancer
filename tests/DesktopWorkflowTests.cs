using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace PromptEnhancer.Tests
{
    internal static class DesktopWorkflowTests
    {
        public static void Register(List<KeyValuePair<string, Action>> tests)
        {
            tests.Add(new KeyValuePair<string, Action>("Settings -> Enhance -> relaunch reuses protected credential", SettingsToEnhancedPrompt));
            tests.Add(new KeyValuePair<string, Action>("A replacement provider drives the unchanged desktop workflow", ReplacementProvider));
            tests.Add(new KeyValuePair<string, Action>("A failed enhancement preserves the draft without showing a secret or stale result", SafeDesktopFailure));
        }

        private static void SafeDesktopFailure()
        {
            string path = TestRunner.CreateCredentialPath();
            int requests = 0;
            try
            {
                var credentials = new DpapiCredentialStore(path);
                credentials.Save(TestRunner.TestKey);
                using (var transport = new ControlledHttp { Respond = (request, cancellation) => Task.FromResult(++requests == 1
                    ? TestRunner.Completion(TestRunner.EnhancedPrompt)
                    : TestRunner.Json(HttpStatusCode.Unauthorized, "{\"error\":{\"message\":\"" + TestRunner.TestKey + "\"}}")) })
                using (var client = new HttpClient(transport))
                using (var window = new MainForm(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials))
                {
                    window.Show();
                    Application.DoEvents();
                    EnhanceThroughDesktop(window);
                    TestRunner.Control<Button>(window, "Enhance").PerformClick();
                    TestRunner.PumpUntil(() => TestRunner.Control<Button>(window, "Enhance").Enabled);
                    TestRunner.Require(TestRunner.Control<TextBox>(window, "EnhancedPrompt").Text == "" &&
                        !TestRunner.Control<Label>(window, "Status").Text.Contains(TestRunner.TestKey) &&
                        TestRunner.Control<Label>(window, "Status").Text.Contains("API key was rejected"),
                        "A failure must show a safe message and remove a stale successful result.");
                    TestRunner.Require(TestRunner.Control<TextBox>(window, "RoughPrompt").Text == TestRunner.RoughPrompt &&
                        TestRunner.Control<Button>(window, "Settings").Enabled,
                        "The user's draft and ability to fix Settings must remain available.");
                    window.Close();
                }
            }
            finally { Directory.Delete(Path.GetDirectoryName(path), true); }
        }

        private static void ReplacementProvider()
        {
            string path = TestRunner.CreateCredentialPath();
            const string rough = "compare these two laptops for programming";
            const string enhanced = "Compare these two laptops for programming, considering processor performance, RAM, storage, and compatibility with development tools.";
            var replacement = new ReplacementIntegration();
            try
            {
                using (var window = new MainForm(replacement, new DpapiCredentialStore(path)))
                {
                    window.Show();
                    Application.DoEvents();
                    TestRunner.Control<TextBox>(window, "RoughPrompt").Text = rough;
                    TestRunner.Control<Button>(window, "Enhance").PerformClick();
                    TestRunner.Require(replacement.SubmittedPrompt == rough && replacement.Policy.Contains("Never ask clarification questions") &&
                        replacement.Policy.Contains("preserving intent") && replacement.Policy.Contains("essential supporting details"),
                        "A replacement integration must receive the submitted prompt and baseline policy without desktop changes.");
                    TestRunner.Require(!TestRunner.Control<Button>(window, "Enhance").Enabled &&
                        !TestRunner.Control<Button>(window, "Settings").Enabled &&
                        TestRunner.Control<Label>(window, "Status").Text == "Enhancing...",
                        "The desktop must show progress and prevent duplicate submissions while waiting.");
                    replacement.Completion.SetResult(enhanced);
                    TestRunner.PumpUntil(() => TestRunner.Control<Button>(window, "Enhance").Enabled);
                    TestRunner.Require(TestRunner.Control<TextBox>(window, "EnhancedPrompt").Text == enhanced,
                        "A replacement provider's response must appear in the same result editor.");
                    window.Close();
                }
            }
            finally { Directory.Delete(Path.GetDirectoryName(path), true); }
        }

        private sealed class ReplacementIntegration : IEnhancementProvider
        {
            public readonly TaskCompletionSource<string> Completion = new TaskCompletionSource<string>();
            public string SubmittedPrompt;
            public string Policy;

            public Task<string> EnhanceAsync(string roughPrompt, string policy, CancellationToken cancellation)
            {
                SubmittedPrompt = roughPrompt;
                Policy = policy;
                return Completion.Task;
            }
        }

        private static void SettingsToEnhancedPrompt()
        {
            string path = TestRunner.CreateCredentialPath();
            try
            {
                var credentials = new DpapiCredentialStore(path);
                using (var transport = SuccessfulTransport())
                using (var client = new HttpClient(transport))
                using (var window = new MainForm(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials))
                {
                    window.Show();
                    Application.DoEvents();
                    SaveThroughSettings(window, TestRunner.TestKey);
                    EnhanceThroughDesktop(window);
                    window.Close();
                }
                byte[] stored = File.ReadAllBytes(path);
                TestRunner.Require(Encoding.UTF8.GetString(stored).IndexOf(TestRunner.TestKey, StringComparison.Ordinal) < 0,
                    "Credential file must not contain the plaintext test key.");
                TestRunner.Require(Directory.GetFiles(Path.GetDirectoryName(path)).Length == 1,
                    "Only the protected credential should persist; no prompts, logs, or settings.");
                // A fresh executable process must use the saved credential without another Settings save.
                var start = new ProcessStartInfo {
                    FileName = System.Reflection.Assembly.GetExecutingAssembly().Location,
                    Arguments = "--relaunch-workflow \"" + path + "\"",
                    UseShellExecute = false, CreateNoWindow = true
                };
                using (var process = Process.Start(start))
                {
                    if (!process.WaitForExit(15000)) { process.Kill(); throw new Exception("Relaunch timed out."); }
                    TestRunner.Require(process.ExitCode == 0, "Saved credential was not reused successfully after process relaunch.");
                }
            }
            finally { Directory.Delete(Path.GetDirectoryName(path), true); }
        }

        public static void UseSavedKeyAfterRelaunch(string path)
        {
            var credentials = new DpapiCredentialStore(path);
            using (var transport = SuccessfulTransport())
            using (var client = new HttpClient(transport))
            using (var window = new MainForm(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials))
            {
                window.Show();
                Application.DoEvents();
                TestRunner.Require(TestRunner.Control<Label>(window, "Status").Text.Contains("Saved API key"),
                    "Relaunch must recognize the saved credential.");
                EnhanceThroughDesktop(window);
                window.Close();
            }
        }

        private static ControlledHttp SuccessfulTransport()
        {
            return new ControlledHttp { Respond = (request, cancellation) => {
                TestRunner.Require(request.Headers.Authorization != null && request.Headers.Authorization.Scheme == "Bearer" &&
                    request.Headers.Authorization.Parameter == TestRunner.TestKey, "Saved key must authenticate the enhancement request.");
                return Task.FromResult(TestRunner.Completion(TestRunner.EnhancedPrompt));
            } };
        }

        public static void SaveThroughSettings(MainForm window, string key)
        {
            Exception failure = null;
            window.BeginInvoke(new Action(() => {
                var settings = Application.OpenForms.OfType<SettingsForm>().Single();
                try
                {
                    var field = TestRunner.Control<TextBox>(settings, "ApiKey");
                    TestRunner.Require(field.Visible && field.UseSystemPasswordChar && field.Text.Length == 0,
                        "Settings must mask the key and never prepopulate it.");
                    field.Text = key;
                    TestRunner.Control<Button>(settings, "SaveKey").PerformClick();
                    TestRunner.Require(settings.DialogResult == DialogResult.OK, "Settings must report a successful save.");
                }
                catch (Exception error) { failure = error; settings.Close(); }
            }));
            TestRunner.Control<Button>(window, "Settings").PerformClick();
            if (failure != null) throw failure;
        }

        private static void EnhanceThroughDesktop(MainForm window)
        {
            var input = TestRunner.Control<TextBox>(window, "RoughPrompt");
            var output = TestRunner.Control<TextBox>(window, "EnhancedPrompt");
            TestRunner.Require(input.Visible && output.Visible, "The rough editor and result must be visible.");
            input.Text = TestRunner.RoughPrompt;
            TestRunner.Control<Button>(window, "Enhance").PerformClick();
            TestRunner.PumpUntil(() => TestRunner.Control<Button>(window, "Enhance").Enabled);
            TestRunner.Require(output.Text == TestRunner.EnhancedPrompt, "Enhance must display the controlled cloud result.");
            TestRunner.Require(input.Text == TestRunner.RoughPrompt, "Enhance must preserve the rough prompt.");
        }
    }
}
