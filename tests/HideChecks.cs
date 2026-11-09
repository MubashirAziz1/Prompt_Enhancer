using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Net.Http;

namespace PromptEnhancer.WinUI.Tests;

public partial class App
{
    private async Task EditorHideWorkflow(ICredentialStore credentials)
    {
        var previousEditor = editor;
        var transport = new ControlledHttp { ExpectedKey = credentials.Load() };
        using var client = new HttpClient(transport);
        editor = new EditorWindow(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials);
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
        for (int cycle = 0; cycle < 3; cycle++)
        {
            // Allow the previous flyout's close animation to finish before opening it again.
            await Task.Delay(250);
            await InvokeSession("Hide");
            await Task.Delay(100);
            Require(!editor.AppWindow.IsVisible && launcher.AppWindow.IsVisible && !quit && !editorDestroyed && !launcherDestroyed,
                "Editor Hide must return to the floating button without quitting or destroying either window.");
            Require(launcher.AppWindow.Position.Equals(position) && launcher.AppWindow.Size.Equals(size),
                "Editor Hide must preserve the floating button's size and position.");
            experience.OpenEditor();
            await Until(() => editor.AppWindow.IsVisible && !launcher.AppWindow.IsVisible);
            Require(Find<TextBox>("RoughPrompt").Text == "Keep this rough prompt." && Find<TextBox>("EnhancedPrompt").Text == "Keep my reviewed result.",
                "Reopening after Hide must preserve both prompts and reviewed edits.");
            checks.Add("PASS Editor Hide/reopen cycle " + (cycle + 1));
        }
        checks.Add("PASS Editor Hide returns to the same floating button and preserves the session through repeated reopenings");

        bool canceled = false;
        var pending = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        transport.ExpectedPrompt = "finish while collapsed";
        transport.Respond = cancellation => { cancellation.Register(() => canceled = true); return pending.Task; };
        Find<TextBox>("RoughPrompt").Text = transport.ExpectedPrompt;
        Invoke("Enhance");
        await Until(() => !Find<Button>("Enhance").IsEnabled);
        await Task.Delay(250);
        await InvokeSession("Hide");
        await Until(() => !editor.AppWindow.IsVisible && launcher.AppWindow.IsVisible);
        Require(!canceled && !quit, "Editor Hide must keep pending enhancement alive.");
        pending.SetResult(ControlledHttp.Completion("Completed while collapsed."));
        await Until(() => Find<Button>("Enhance").IsEnabled);
        Require(!editor.AppWindow.IsVisible && launcher.AppWindow.IsVisible && !canceled,
            "Enhancement completion after Hide must leave the floating button visible without reopening the editor.");
        experience.OpenEditor();
        await Until(() => editor.AppWindow.IsVisible);
        Require(Find<TextBox>("RoughPrompt").Text == transport.ExpectedPrompt && Find<TextBox>("EnhancedPrompt").Text == "Completed while collapsed.",
            "Reopening must expose the enhancement completed after Hide.");
        checks.Add("PASS Hide during enhancement retains pending work and its result without reopening the editor");

        await Task.Delay(250);
        await InvokeSession("Quit");
        await Until(() => quit && editorDestroyed && launcherDestroyed);
        editor = null;
        checks.Add("PASS Quit still destroys both windows and invokes shutdown");
    }
}
