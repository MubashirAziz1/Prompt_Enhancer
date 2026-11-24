using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using System.Net.Http;

namespace PromptEnhancer.WinUI.Tests;

public partial class App
{
    private async Task EditorCloseWorkflow(ICredentialStore credentials)
    {
        var previousEditor = editor;
        var transport = new ControlledHttp { ExpectedKey = credentials.Load() };
        using var client = new HttpClient(transport);
        editor = new EditorWindow(new OpenRouterProvider(client, credentials), credentials);
        previousEditor?.Close();
        var launcher = new LauncherWindow();
        bool quit = false, editorDestroyed = false, launcherDestroyed = false;
        editor.AppWindow.Destroying += (_, _) => editorDestroyed = true;
        launcher.AppWindow.Destroying += (_, _) => launcherDestroyed = true;
        using var experience = new FloatingExperience(editor, launcher, () => quit = true);
        experience.Start();
        await Until(() => launcher.AppWindow.IsVisible && ((FrameworkElement)launcher.Content).ActualHeight > 0);
        var position = launcher.AppWindow.Position;
        var size = launcher.AppWindow.Size;
        experience.OpenEditor();
        await Until(() => editor.AppWindow.IsVisible && ((FrameworkElement)editor.Content).ActualHeight > 0);
        Find<TextBox>("RoughPrompt").Text = "Keep this rough prompt.";
        Find<TextBox>("EnhancedPrompt").Text = "Keep my reviewed result.";
        Invoke("Clear");
        Invoke("UndoSession");
        for (int cycle = 0; cycle < 3; cycle++)
        {
            Invoke("Collapse");
            await Task.Delay(100);
            Require(!editor.AppWindow.IsVisible && launcher.AppWindow.IsVisible && !quit && !editorDestroyed && !launcherDestroyed,
                "Closing the editor must return to the floating button without quitting or destroying either window.");
            Require(launcher.AppWindow.Position.Equals(position) && launcher.AppWindow.Size.Equals(size),
                "Closing the editor must preserve the floating button's size and position.");
            experience.OpenEditor();
            await Until(() => editor.AppWindow.IsVisible && !launcher.AppWindow.IsVisible);
            Require(Find<TextBox>("RoughPrompt").Text == "Keep this rough prompt." && Find<TextBox>("EnhancedPrompt").Text == "Keep my reviewed result.",
                "Reopening after closing must preserve both prompts and reviewed edits.");
            Require(Find<Button>("UndoSession").IsEnabled && (string)Find<Button>("UndoSession").Content == "Redo",
                "Collapse/reopen must preserve recovery and its direction.");
            checks.Add("PASS Editor close/reopen cycle " + (cycle + 1));
        }
        checks.Add("PASS Editor close returns to the same floating button and preserves the session through repeated reopenings");

        bool canceled = false;
        var pending = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        transport.ExpectedPrompt = "finish while collapsed";
        transport.Respond = cancellation => { cancellation.Register(() => canceled = true); return pending.Task; };
        Find<TextBox>("RoughPrompt").Text = transport.ExpectedPrompt;
        Invoke("Enhance");
        await Until(() => !Find<Button>("Enhance").IsEnabled);
        Invoke("Collapse");
        await Until(() => !editor.AppWindow.IsVisible && launcher.AppWindow.IsVisible);
        Require(!canceled && !quit, "Closing the editor must keep pending enhancement alive.");
        pending.SetResult(ControlledHttp.Completion("Completed while collapsed."));
        await Until(() => Find<Button>("Enhance").IsEnabled);
        Require(!editor.AppWindow.IsVisible && launcher.AppWindow.IsVisible && !canceled,
            "Enhancement completion after closing must leave the floating button visible without reopening the editor.");
        experience.OpenEditor();
        await Until(() => editor.AppWindow.IsVisible);
        Require(Find<TextBox>("RoughPrompt").Text == transport.ExpectedPrompt && Find<TextBox>("EnhancedPrompt").Text == "Completed while collapsed.",
            "Reopening must expose the enhancement completed after closing.");
        checks.Add("PASS Close during enhancement retains pending work and its result without reopening the editor");

        pending = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        transport.Respond = _ => pending.Task;
        Invoke("Enhance");
        await Until(() => !Find<Button>("Enhance").IsEnabled);
        Invoke("Collapse");
        var menu = (MenuFlyout)Descendants((FrameworkElement)launcher.Content).OfType<FloatingButton>().Single().ContextFlyout;
        var hide = menu.Items.OfType<MenuFlyoutItem>().Single(item => item.Text == "Hide");
        ((IInvokeProvider)new MenuFlyoutItemAutomationPeer(hide).GetPattern(PatternInterface.Invoke)).Invoke();
        await Until(() => !editor.AppWindow.IsVisible && !launcher.AppWindow.IsVisible);
        pending.SetException(new HttpRequestException("REMOTE fixture failure"));
        await Until(() => Find<Button>("Enhance").IsEnabled);
        Require(!editor.AppWindow.IsVisible && !launcher.AppWindow.IsVisible &&
            Find<TextBox>("EnhancedPrompt").Text == "Completed while collapsed." &&
            Find<TextBlock>("ResultTitle").Text == "Previous result" && (string)Find<Button>("Enhance").Content == "Retry" &&
            Find<Button>("Copy").IsEnabled && Find<Button>("UndoSession").IsEnabled,
            "Failure while both windows are hidden must retain reviewed work and recovery without activating either window.");
        experience.Restore();
        Require(launcher.AppWindow.IsVisible && !editor.AppWindow.IsVisible, "Restore after hidden failure must show only the floating button.");
        experience.OpenEditor();
        await Until(() => editor.AppWindow.IsVisible);
        checks.Add("PASS Hidden failure retains the result, Retry semantics and recovery without reopening either window");

        await Task.Delay(250);
        await InvokeSession("Quit");
        await Until(() => quit && editorDestroyed && launcherDestroyed);
        editor = null;
        checks.Add("PASS Quit still destroys both windows and invokes shutdown");
    }
}
