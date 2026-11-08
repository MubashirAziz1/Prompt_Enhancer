using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
            tests.Add(new KeyValuePair<string, Action>("Paste -> Enhance -> edit -> Copy changes the clipboard only on Copy", () => PasteEnhanceEditCopy(false)));
            tests.Add(new KeyValuePair<string, Action>("Keyboard paste -> Enhance -> edit -> Copy preserves clipboard control", () => PasteEnhanceEditCopy(true)));
            tests.Add(new KeyValuePair<string, Action>("Floating launcher -> Enhance -> close/minimize -> launcher -> exit", FloatingWidgetWorkflow));
            tests.Add(new KeyValuePair<string, Action>("Floating workflow stays above another active editor while dragging, typing, and enhancing", FloatingWidgetMouse));
        }

        private static void FloatingWidgetMouse()
        {
            string path = TestRunner.CreateCredentialPath();
            try
            {
                var credentials = new DpapiCredentialStore(path);
                credentials.Save(TestRunner.TestKey);
                using (var companion = new CompanionEditor())
                using (var transport = SuccessfulTransport())
                using (var client = new HttpClient(transport))
                using (var widget = new FloatingWidgetContext(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials))
                {
                    var launcher = widget.MainForm;
                    var workArea = Screen.FromControl(launcher).WorkingArea;
                    launcher.Location = new Point(workArea.Left + 60, workArea.Top + 60);
                    launcher.Activate();
                    Application.DoEvents();
                    companion.AssertBelow(launcher);
                    Point before = launcher.Location;
                    DesktopMouse.Stroke(launcher, DesktopMouse.Center(TestRunner.Control<Button>(launcher, "OpenEditor")), new Point(100, 50));
                    TestRunner.Require(launcher.Visible && !Application.OpenForms.OfType<MainForm>().Any(window => window.Visible) &&
                        Math.Abs(launcher.Left - before.X - 100) <= 2 && Math.Abs(launcher.Top - before.Y - 50) <= 2,
                        "Dragging must move the launcher without expanding the editor.");
                    DesktopMouse.Click(TestRunner.Control<Button>(launcher, "OpenEditor"));
                    var editor = Application.OpenForms.OfType<MainForm>().Single();
                    TestRunner.Require(editor.Visible && !launcher.Visible, "A normal click after a drag must expand the editor.");
                    companion.AssertBelow(editor);
                    before = editor.Location;
                    DesktopMouse.Stroke(editor, new Point(editor.Left + 120, editor.Top + SystemInformation.FrameBorderSize.Height +
                        SystemInformation.CaptionHeight / 2), new Point(70, 30));
                    TestRunner.Require(Math.Abs(editor.Left - before.X - 70) <= 2 && Math.Abs(editor.Top - before.Y - 30) <= 2,
                        "Dragging the editor title bar must move the expanded window.");
                    var input = TestRunner.Control<TextBox>(editor, "RoughPrompt");
                    input.Text = "select these words with the mouse";
                    before = editor.Location;
                    DesktopMouse.Stroke(editor, input.PointToScreen(new Point(5, 10)), new Point(100, 0));
                    TestRunner.Require(input.SelectionLength > 0 && editor.Location == before,
                        "Dragging in the rough prompt must select text without moving the editor.");
                    input.Focus();
                    input.SelectAll();
                    SendKeys.SendWait("typed rough prompt");
                    TestRunner.Require(input.Text == "typed rough prompt" && editor.Location == before,
                        "Typing must remain available after window and text dragging.");
                    DesktopMouse.Click(TestRunner.Control<Button>(editor, "Enhance"));
                    TestRunner.PumpUntil(() => TestRunner.Control<Button>(editor, "Enhance").Enabled);
                    var output = TestRunner.Control<TextBox>(editor, "EnhancedPrompt");
                    TestRunner.Require(output.Text == TestRunner.EnhancedPrompt,
                        "The real Enhance control must display the controlled cloud result in the floating editor.");
                    companion.AssertBelow(editor);
                    editor.Activate();
                    SendKeys.SendWait("%{F4}");
                    TestRunner.PumpUntil(() => launcher.Visible && !editor.Visible);
                    companion.AssertBelow(launcher);
                }
            }
            finally { Directory.Delete(Path.GetDirectoryName(path), true); }
        }

        private static void FloatingWidgetWorkflow()
        {
            string path = TestRunner.CreateCredentialPath();
            try
            {
                var credentials = new DpapiCredentialStore(path);
                credentials.Save(TestRunner.TestKey);
                using (var transport = SuccessfulTransport())
                using (var client = new HttpClient(transport))
                using (var widget = new FloatingWidgetContext(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials))
                {
                    Application.DoEvents();
                    var launcher = widget.MainForm;
                    var workArea = Screen.FromControl(launcher).WorkingArea;
                    TestRunner.Require(launcher.Visible && launcher.TopMost && launcher.Width < workArea.Width / 4 && launcher.Height < workArea.Height / 4 &&
                        !Application.OpenForms.OfType<MainForm>().Any(window => window.Visible),
                        "Startup must show only a small topmost launcher. Visible: " + launcher.Visible +
                        ", topmost: " + launcher.TopMost + ", size: " + launcher.Size + ", visible editors: " +
                        Application.OpenForms.OfType<MainForm>().Count(window => window.Visible));
                    var open = TestRunner.Control<Button>(launcher, "OpenEditor");
                    open.PerformClick();
                    Application.DoEvents();
                    var editor = Application.OpenForms.OfType<MainForm>().Single();
                    TestRunner.Require(editor.Visible && editor.TopMost && !launcher.Visible,
                        "Opening the launcher must show the existing topmost editor and hide the button.");
                    EnhanceThroughDesktop(editor);
                    Size expandedSize = editor.Size;
                    editor.Close();
                    Application.DoEvents();
                    TestRunner.Require(!editor.IsDisposed && !editor.Visible && launcher.Visible,
                        "Closing the editor must return to a working launcher without exiting.");
                    open.PerformClick();
                    Application.DoEvents();
                    editor.WindowState = FormWindowState.Minimized;
                    TestRunner.PumpUntil(() => launcher.Visible && !editor.Visible);
                    TestRunner.Require(!editor.IsDisposed && !editor.Visible && launcher.Visible,
                        "Minimizing the editor must return to the launcher.");
                    open.PerformClick();
                    Application.DoEvents();
                    TestRunner.Require(editor.Visible && editor.WindowState == FormWindowState.Normal && editor.Size == expandedSize,
                        "The editor must expand normally again after minimization. Editor visible: " + editor.Visible +
                        ", state: " + editor.WindowState + ", launcher visible: " + launcher.Visible +
                        ", open enabled: " + open.Enabled + ", size: " + editor.Size + ", expected: " + expandedSize);
                    editor.Close();
                    bool exited = false;
                    widget.ThreadExit += delegate { exited = true; };
                    launcher.Close();
                    Application.DoEvents();
                    TestRunner.Require(exited && launcher.IsDisposed && editor.IsDisposed,
                        "Closing the launcher must end application lifetime and dispose the editor.");
                }
            }
            finally { Directory.Delete(Path.GetDirectoryName(path), true); }
        }

        private static void PasteEnhanceEditCopy(bool keyboardPaste)
        {
            string path = TestRunner.CreateCredentialPath();
            IDataObject originalClipboard = Clipboard.GetDataObject();
            try
            {
                Clipboard.SetText(TestRunner.RoughPrompt);
                var credentials = new DpapiCredentialStore(path);
                credentials.Save(TestRunner.TestKey);
                var response = new TaskCompletionSource<HttpResponseMessage>();
                using (var transport = new ControlledHttp { Respond = (request, cancellation) => response.Task })
                using (var client = new HttpClient(transport))
                using (var window = new MainForm(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials))
                {
                    window.Show();
                    Application.DoEvents();
                    var input = TestRunner.Control<TextBox>(window, "RoughPrompt");
                    var output = TestRunner.Control<TextBox>(window, "EnhancedPrompt");
                    TestRunner.Require(input.Text == "" && output.Text == "" && Clipboard.GetText() == TestRunner.RoughPrompt,
                        "Opening the editor must not insert or overwrite clipboard text.");
                    input.SelectedText = "typed draft";
                    TestRunner.Require(input.Text == "typed draft" && Clipboard.GetText() == TestRunner.RoughPrompt,
                        "Typing must remain available without changing the clipboard.");
                    input.SelectAll();
                    if (keyboardPaste)
                    {
                        window.Activate();
                        input.Focus();
                        TestRunner.PumpUntil(() => input.Focused);
                        SendKeys.SendWait("^v");
                        Application.DoEvents();
                    }
                    else TestRunner.Control<Button>(window, "Paste").PerformClick();
                    TestRunner.Require(input.Text == TestRunner.RoughPrompt && Clipboard.GetText() == TestRunner.RoughPrompt,
                        "Explicit Paste must insert clipboard text at the current selection without changing the clipboard.");
                    var copy = TestRunner.Control<Button>(window, "Copy");
                    TestRunner.Require(!copy.Enabled, "Copy must be unavailable before there is a result.");
                    TestRunner.Control<Button>(window, "Enhance").PerformClick();
                    TestRunner.Require(!copy.Enabled && !output.Enabled && Clipboard.GetText() == TestRunner.RoughPrompt,
                        "Waiting for enhancement must not expose an unfinished result or change the clipboard.");
                    response.SetResult(TestRunner.Completion(TestRunner.EnhancedPrompt));
                    TestRunner.PumpUntil(() => TestRunner.Control<Button>(window, "Enhance").Enabled);
                    TestRunner.Require(output.Text == TestRunner.EnhancedPrompt && output.Enabled && !output.ReadOnly && copy.Enabled &&
                        Clipboard.GetText() == TestRunner.RoughPrompt,
                        "The enhanced prompt must be editable and ready for explicit Copy without automatic copying. " +
                        "Result matches: " + (output.Text == TestRunner.EnhancedPrompt) + ", editor enabled: " + output.Enabled +
                        ", read-only: " + output.ReadOnly + ", Copy enabled: " + copy.Enabled +
                        ", clipboard unchanged: " + (Clipboard.GetText() == TestRunner.RoughPrompt));
                    const string reviewed = "Write a short email to my manager.\r\nRequest leave tomorrow for a personal appointment.\r\nUse a polite tone \u2014 thank you.";
                    output.SelectAll();
                    output.SelectedText = reviewed;
                    TestRunner.Require(output.Text == reviewed && Clipboard.GetText() == TestRunner.RoughPrompt,
                        "Review edits must remain in the editor until Copy is selected.");
                    copy.PerformClick();
                    TestRunner.Require(Clipboard.GetText() == reviewed,
                        "Copy must write the current edited result, preserving line breaks and Unicode. Status: " +
                        TestRunner.Control<Label>(window, "Status").Text);
                    output.Clear();
                    TestRunner.Require(!copy.Enabled && Clipboard.GetText() == reviewed,
                        "Clearing the result must disable Copy without clearing the clipboard.");
                    output.SelectedText = "   ";
                    TestRunner.Require(!copy.Enabled, "Whitespace-only results must not enable Copy.");
                    window.Close();
                }
            }
            finally
            {
                if (originalClipboard == null) Clipboard.Clear();
                else Clipboard.SetDataObject(originalClipboard, true, 10, 100);
                Directory.Delete(Path.GetDirectoryName(path), true);
            }
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
                        !TestRunner.Control<Button>(window, "Copy").Enabled &&
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
