using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using System.Diagnostics;
using System.Net.Http;

namespace PromptEnhancer.WinUI.Tests;

public partial class App
{
    private async Task FloatingProcess(string path, string mode)
    {
        path = Path.GetFullPath(path);
        Require(path.StartsWith(Path.Combine(AppContext.BaseDirectory, "test-data") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
            "Fresh-process checks must use isolated credentials.");
        var credentials = new DpapiCredentialStore(path);
        var transport = new ControlledHttp { ExpectedKey = credentials.Load() };
        using var client = new HttpClient(transport);
        editor = new EditorWindow(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials);
        var launcher = new LauncherWindow { Title = "Prompt Enhancer fresh " + Environment.ProcessId };
        bool canceled = false, pending = false;
        var experience = new FloatingExperience(editor, launcher, () =>
        {
            File.WriteAllText(path + "." + mode, !pending || canceled ? "PASS" : "FAIL cancellation");
            client.Dispose();
            Exit();
        });
        experience.Start();
        await Until(() => ((FrameworkElement)launcher.Content).ActualHeight > 0);
        Require(!editor.AppWindow.IsVisible, "Each fresh process must start with the floating button.");
        var open = Descendants((FrameworkElement)launcher.Content).OfType<Button>()
            .Single(b => AutomationProperties.GetAutomationId(b) == "OpenEditor");
        ((IInvokeProvider)new ButtonAutomationPeer(open).GetPattern(PatternInterface.Invoke)).Invoke();
        await Until(() => ((FrameworkElement)editor.Content).ActualHeight > 0);
        Require(Find<TextBox>("RoughPrompt").Text == "" && Find<TextBox>("EnhancedPrompt").Text == "" && !Find<Button>("Copy").IsEnabled,
            "Relaunch must discard both prompts and result edits.");
        Find<TextBox>("RoughPrompt").Text = "write a short email";
        Invoke("Enhance");
        await Until(() => Find<Button>("Enhance").IsEnabled);
        Require(Find<TextBox>("EnhancedPrompt").Text == "Write a short email.", "A fresh floating process must reuse the protected key.");
        if (mode == "pending-quit")
        {
            transport.Respond = async cancellation =>
            {
                cancellation.Register(() => canceled = true);
                await Task.Delay(Timeout.Infinite, cancellation);
                return ControlledHttp.Completion("Unreachable.");
            };
            Invoke("Enhance");
            await Until(() => !Find<Button>("Enhance").IsEnabled);
            pending = true;
            await InvokeSession("Quit");
        }
        else if (mode == "launcher-close")
        {
            NativeDesktop.Close(editor);
            await Until(() => launcher.AppWindow.IsVisible);
            NativeDesktop.Close(launcher);
        }
        else
        {
            await InvokeSession("Hide");
            await Until(() => !editor.AppWindow.IsVisible && launcher.AppWindow.IsVisible);
            await TrayCommand("hide", launcher.Title);
            await Until(() => !editor.AppWindow.IsVisible && !launcher.AppWindow.IsVisible);
            await TrayCommand("quit", launcher.Title);
        }
        // Quit's callback exits this application, including its hidden host window.
        await Task.Delay(10000);
        throw new Exception("The floating process must exit completely on Quit.");
    }

    private async Task FreshFloatingProcesses(string path)
    {
        foreach (string mode in new[] { "pending-quit", "launcher-close", "tray-quit" })
        {
            var info = new ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = false };
            info.ArgumentList.Add("--floating-process");
            info.ArgumentList.Add(path);
            info.ArgumentList.Add(mode);
            using var process = Process.Start(info);
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
                await process.WaitForExitAsync(timeout.Token);
                Require(process.ExitCode == 0 && File.ReadAllText(path + "." + mode) == "PASS", "Fresh floating process failed: " + mode);
                await TrayCommand("absent", "Prompt Enhancer fresh " + process.Id);
            }
            finally { if (!process.HasExited) { process.Kill(); await process.WaitForExitAsync(); } }
            checks.Add("PASS Fresh process " + mode + ": empty session, protected-key reuse, complete exit and notification-icon removal");
        }
    }
}
