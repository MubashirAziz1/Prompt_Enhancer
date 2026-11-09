using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using System.Net.Http;
using System.Diagnostics;
using Windows.Graphics;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace PromptEnhancer.WinUI.Tests;

public partial class App
{
    private async Task FloatingSession(ICredentialStore credentials)
    {
        var previousEditor = editor;
        var transport = new ControlledHttp { ExpectedKey = credentials.Load() };
        using var client = new HttpClient(transport);
        editor = new EditorWindow(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials);
        string trayName = "Prompt Enhancer acceptance " + Environment.ProcessId;
        var launcher = new LauncherWindow { Title = trayName };
        previousEditor?.Close();
        using var experience = new FloatingExperience(editor, launcher, () => { });
        experience.Start();
        await Until(() => launcher.AppWindow.IsVisible && ((FrameworkElement)launcher.Content).ActualHeight > 0);
        Require(!editor.AppWindow.IsVisible, "Startup must show only the floating button.");
        var launcherRoot = (FrameworkElement)launcher.Content;
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            launcherRoot.RequestedTheme = theme;
            await Until(() => launcherRoot.ActualTheme == theme);
            await Task.Delay(100);
            Require(launcherRoot.ActualWidth is >= 91 and <= 97 && launcherRoot.ActualHeight is >= 91 and <= 97,
                "The floating button must retain usable logical dimensions at the actual display scale.");
            await Snapshot(launcherRoot, "floating-" + theme + ".png");
        }
        launcherRoot.RequestedTheme = ElementTheme.Default;
        checks.Add("PASS Light/dark floating button rendering at " + (launcherRoot.XamlRoot.RasterizationScale * 100).ToString("0") + "% display scale");
        using var companion = Process.Start(new ProcessStartInfo(DesktopProbePath()) { Arguments = "companion", UseShellExecute = false, CreateNoWindow = true });
        try
        {
            await Until(() => { companion.Refresh(); return companion.MainWindowHandle != 0; });
            await NativeDesktop.AssertAboveCompanion(launcher, companion);
            var origin = launcher.AppWindow.Position;
            await NativeDesktop.Stroke(launcher, NativeDesktop.Center(launcher), new PointInt32(-160, 50));
            Require(launcher.AppWindow.Position.X < origin.X - 100 && launcher.AppWindow.Position.Y > origin.Y + 20 && !editor.AppWindow.IsVisible,
                $"Dragging the floating button must move it without opening the editor. Before={origin.X},{origin.Y}; after={launcher.AppWindow.Position.X},{launcher.AppWindow.Position.Y}; editor visible={editor.AppWindow.IsVisible}.");
            await NativeDesktop.Stroke(launcher, NativeDesktop.Center(launcher), new PointInt32());
            await Until(() => editor.AppWindow.IsVisible && !launcher.AppWindow.IsVisible && ((FrameworkElement)editor.Content).ActualHeight > 0);
            await NativeDesktop.AssertAboveCompanion(editor, companion);
            origin = editor.AppWindow.Position;
            double scale = ((FrameworkElement)editor.Content).XamlRoot.RasterizationScale;
            await NativeDesktop.Stroke(editor, new PointInt32(origin.X + 180, origin.Y + (int)(16 * scale)), new PointInt32(-70, 25));
            Require(editor.AppWindow.Position.X < origin.X - 40, "Native title-bar dragging must move the prompt editor.");
            Find<TextBox>("RoughPrompt").Text = "Retain this rough prompt.";
            Find<TextBox>("EnhancedPrompt").Text = "Retain my result edits.";
            var result = Find<TextBox>("EnhancedPrompt");
            origin = editor.AppWindow.Position;
            // Client coordinates include the native title bar; use ClientToScreen for a real text drag.
            await SelectTextWithMouse(result, scale);
            Require(result.SelectedText.Length > 0 && editor.AppWindow.Position.Equals(origin), "Mouse text selection must not move the editor.");
            checks.Add("PASS Native launcher drag/click, title-bar movement, normal text selection, and actual stacking over an active companion editor");
            // The visible close control must collapse and retain the current session.
            Invoke("Collapse");
            await Until(() => launcher.AppWindow.IsVisible && !editor.AppWindow.IsVisible);
            NativeDesktop.Press(launcher, 0x20);
            await Until(() => editor.AppWindow.IsVisible);
            Require(Find<TextBox>("RoughPrompt").Text == "Retain this rough prompt." && Find<TextBox>("EnhancedPrompt").Text == "Retain my result edits.",
                "Closing and reopening must preserve the current prompt session.");
            ((OverlappedPresenter)editor.AppWindow.Presenter).Minimize();
            await Until(() => launcher.AppWindow.IsVisible && !editor.AppWindow.IsVisible);
            experience.OpenEditor();
            await Until(() => editor.AppWindow.IsVisible);
            Require(Find<TextBox>("EnhancedPrompt").Text == "Retain my result edits.", "Minimize must retain result edits.");
            NativeDesktop.Close(editor);
            await Until(() => launcher.AppWindow.IsVisible);
            var openButton = Descendants(launcherRoot).OfType<Button>().Single(b => b is FloatingButton);
            var menu = (MenuFlyout)openButton.ContextFlyout;
            await NativeDesktop.Stroke(launcher, NativeDesktop.Center(launcher), new PointInt32(), true);
            Require(launcher.AppWindow.IsVisible && !editor.AppWindow.IsVisible, "Right-click must leave the floating button visible for its context menu.");
            try { await Until(() => menu.IsOpen); }
            catch (Exception) { throw new Exception("Native right-click must open the launcher's context menu."); }
            ((IInvokeProvider)new MenuFlyoutItemAutomationPeer(menu.Items.OfType<MenuFlyoutItem>().Single(item => item.Text == "Hide"))
                .GetPattern(PatternInterface.Invoke)).Invoke();
            await Until(() => !editor.AppWindow.IsVisible && !launcher.AppWindow.IsVisible);
            await TrayCommand("restore", trayName);
            experience.OpenEditor();
            await Until(() => editor.AppWindow.IsVisible);
            checks.Add("PASS Native launcher context menu Hide removes both windows and retains the session");
            await InvokeSession("Hide");
            await Task.Delay(100);
            Require(!editor.AppWindow.IsVisible && launcher.AppWindow.IsVisible,
                "Editor Hide must return to the floating button without quitting or hiding the launcher.");
            Require(Find<TextBox>("RoughPrompt").Text == "Retain this rough prompt." && result.Text == "Retain my result edits.",
                "Editor Hide must retain the rough prompt and reviewed result.");
            await TrayCommand("hide", trayName);
            await Until(() => !editor.AppWindow.IsVisible && !launcher.AppWindow.IsVisible);
            await TrayCommand("restore", trayName);
            await Until(() => launcher.AppWindow.IsVisible && !editor.AppWindow.IsVisible);
            await NativeDesktop.Stroke(launcher, NativeDesktop.Center(launcher), new PointInt32());
            await Until(() => editor.AppWindow.IsVisible);
            Require(result.Text == "Retain my result edits.", "The real tray menu must restore the retained session.");
            await InvokeSession("Hide");
            await Until(() => !editor.AppWindow.IsVisible && launcher.AppWindow.IsVisible);
            await TrayCommand("hide", trayName);
            await Until(() => !editor.AppWindow.IsVisible && !launcher.AppWindow.IsVisible);
            await TrayCommand("double", trayName);
            await Until(() => launcher.AppWindow.IsVisible && !editor.AppWindow.IsVisible);
            experience.OpenEditor();
            await Until(() => editor.AppWindow.IsVisible);
            checks.Add("PASS Floating startup, close/minimize collapse, retained rough/result edits, and Hide");
            checks.Add("PASS Real notification-area menu and double-click restore the floating button and retained session");

            var pending = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            transport.ExpectedPrompt = "finish while hidden";
            transport.Respond = _ => pending.Task;
            Find<TextBox>("RoughPrompt").Text = transport.ExpectedPrompt;
            Invoke("Enhance");
            await Until(() => !Find<Button>("Enhance").IsEnabled);
            await OpenSessionMenu();
            Require(!Find<Button>("Clear").IsEnabled && Find<Button>("Hide").IsEnabled && Find<Button>("Quit").IsEnabled,
                "Hide and Quit must remain available during enhancement; Clear must wait until idle.");
            await InvokeSession("Hide");
            await Until(() => !editor.AppWindow.IsVisible && launcher.AppWindow.IsVisible);
            await TrayCommand("hide", trayName);
            await Until(() => !editor.AppWindow.IsVisible && !launcher.AppWindow.IsVisible);
            pending.SetResult(ControlledHttp.Completion("Available when restored."));
            await Until(() => Find<Button>("Enhance").IsEnabled);
            Require(!editor.AppWindow.IsVisible && !launcher.AppWindow.IsVisible && result.Text == "Available when restored.",
                "Completion while hidden must retain the result without showing either window.");
            await TrayCommand("restore", trayName);
            experience.OpenEditor();
            await Until(() => editor.AppWindow.IsVisible);
            Require(result.Text == "Available when restored.", "Restoration must expose a completion received while hidden.");
            checks.Add("PASS Hide remains available during enhancement; tray-hidden completion stays hidden and is retained");

            bool canceled = false;
            pending = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            transport.Respond = cancellation => { cancellation.Register(() => canceled = true); return pending.Task; };
            Invoke("Enhance");
            await Until(() => !Find<Button>("Enhance").IsEnabled);
            var editorWindow = editor.AppWindow;
            var launcherWindow = launcher.AppWindow;
            bool editorDestroyed = false, launcherDestroyed = false;
            editorWindow.Destroying += (_, _) => editorDestroyed = true;
            launcherWindow.Destroying += (_, _) => launcherDestroyed = true;
            await InvokeSession("Quit");
            await Until(() => editorDestroyed && launcherDestroyed && canceled);
            pending.SetResult(ControlledHttp.Completion("Late completion must be discarded."));
            await Task.Delay(200);
            await TrayCommand("absent", trayName);
            editor = null;
            checks.Add("PASS Quit cancels work, destroys both windows, removes the tray icon, and ignores late completion");
        }
        finally
        {
            if (!companion.HasExited)
            {
                companion.CloseMainWindow();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try { await companion.WaitForExitAsync(timeout.Token); }
                catch (OperationCanceledException) { companion.Kill(); }
            }
        }
    }

    private async Task OpenSessionMenu()
    {
        if (Elements().OfType<Button>().Any(button => AutomationProperties.GetAutomationId(button) == "Hide")) return;
        Invoke("SessionMenu");
        await Until(() => Elements().OfType<Button>().Any(button => AutomationProperties.GetAutomationId(button) == "Hide"));
    }

    private async Task InvokeSession(string command)
    {
        await OpenSessionMenu();
        Invoke(command);
    }
    private static string DesktopProbePath() => FixtureExecutable("DesktopProbe.exe");

    private static string FixtureExecutable(string name)
    {
        string published = Path.Combine(AppContext.BaseDirectory, "fixtures", name);
        return File.Exists(published) ? published : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", name));
    }

    private async Task TrayCommand(string command, string name)
    {
        var info = new ProcessStartInfo(DesktopProbePath()) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        info.ArgumentList.Add(command);
        info.ArgumentList.Add(name);
        using var process = Process.Start(info);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await process.WaitForExitAsync(timeout.Token);
        string output = await process.StandardOutput.ReadToEndAsync();
        Require(process.ExitCode == 0, "Native tray " + command + ": " + await process.StandardError.ReadToEndAsync());
        if (output.Contains("overflow")) checks.Add("PASS Tray " + command + " reached the actual notification icon through shell overflow");
    }

    private async Task SelectTextWithMouse(TextBox box, double scale)
    {
        NativeDesktop.Activate(editor);
        box.Focus(FocusState.Pointer);
        await Task.Delay(100);
        var point = box.TransformToVisual((FrameworkElement)editor.Content).TransformPoint(new Windows.Foundation.Point(18, 18));
        var start = NativeDesktop.ClientPoint(editor, (int)(point.X * scale), (int)(point.Y * scale));
        await NativeDesktop.Stroke(editor, start, new PointInt32((int)(130 * scale), 0));
    }

    private async Task ClearWorkflow(ControlledHttp transport)
    {
        Find<TextBox>("EnhancedPrompt").Text = "Reviewed result.";
        Invoke("Clear");
        Require(Find<TextBox>("RoughPrompt").Text == "" && Find<TextBox>("EnhancedPrompt").Text == "" && !Find<Button>("Copy").IsEnabled,
            "Clear must empty both current prompts and disable Copy.");
        transport.ExpectedPrompt = "start a fresh task";
        Find<TextBox>("RoughPrompt").Text = transport.ExpectedPrompt;
        transport.Respond = _ => Task.FromResult(ControlledHttp.Completion("Start a fresh task."));
        Invoke("Enhance");
        await Until(() => Find<Button>("Enhance").IsEnabled);
        Require(Find<TextBox>("EnhancedPrompt").Text == "Start a fresh task.", "Clear must retain the saved key for a fresh enhancement.");
        checks.Add("PASS Clear discards both prompts and permits fresh enhancement with the protected key");
        transport.ExpectedPrompt = "write a short email";
        transport.Respond = _ => Task.FromResult(ControlledHttp.Completion("Write a short email."));
    }
}
