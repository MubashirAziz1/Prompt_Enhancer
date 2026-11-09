using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Net.Http;
using System.Runtime.InteropServices;

namespace PromptEnhancer.WinUI.Tests;

public partial class App
{
    private async Task ClipboardWorkflow(ControlledHttp transport)
    {
        var rough = Find<TextBox>("RoughPrompt");
        var result = Find<TextBox>("EnhancedPrompt");
        await ClipboardFixture.SetText("compare these two laptops");
        rough.Text = "";
        Invoke("Paste");
        await Until(() => rough.Text == "compare these two laptops");
        Require(await ClipboardFixture.Text() == "compare these two laptops", "Explicit Paste must not write the clipboard.");

        const string pasted = "compare \"these\" two laptops for café work \\ research";
        await ClipboardFixture.SetText(pasted);
        editor.Activate();
        rough.Focus(FocusState.Keyboard);
        await Task.Delay(100);
        NativeDesktop.Chord(editor, 0x41); // Ctrl+A through Windows input.
        NativeDesktop.Chord(editor, 0x56); // Ctrl+V through Windows input.
        await Until(() => rough.Text == pasted);
        transport.ExpectedPrompt = pasted;
        var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        transport.Respond = cancellation => response.Task.WaitAsync(cancellation);
        int previous = transport.Requests;
        Invoke("Enhance");
        // A rapid second invocation may be queued before the first event, or rejected
        // by the native peer after it disables. Either way only one request is allowed.
        try { Invoke("Enhance"); }
        catch (COMException error) when (error.HResult == unchecked((int)0x80040200)) { }
        await Until(() => transport.Requests > previous);
        Require(transport.Requests == previous + 1 && !Find<Button>("Enhance").IsEnabled &&
            !Find<Button>("Settings").IsEnabled && !Find<Button>("Paste").IsEnabled && !rough.IsEnabled && !result.IsEnabled &&
            !Find<Button>("Copy").IsEnabled && result.Text == "" && Find<ProgressBar>("Progress").Visibility == Visibility.Visible,
            "Pending enhancement must prevent duplicates/conflicts, remove stale results, and show progress.");
        Require(await ClipboardFixture.Text() == pasted, "Typing and enhancement must not change the clipboard.");
        response.SetResult(ControlledHttp.Completion("Compare these two laptops for programming."));
        await Until(() => Find<Button>("Enhance").IsEnabled);
        Require(result.Text == "Compare these two laptops for programming." && rough.IsEnabled && result.IsEnabled &&
            Find<Button>("Settings").IsEnabled && Find<Button>("Paste").IsEnabled &&
            Find<ProgressBar>("Progress").Visibility == Visibility.Collapsed, "Completion must restore all idle actions.");
        result.Text = "   ";
        Require(!Find<Button>("Copy").IsEnabled, "Whitespace is not a copyable result.");
        result.Text = "Edited result for copying.";
        Require(Find<Button>("Copy").IsEnabled && await ClipboardFixture.Text() == pasted,
            "Editing must enable Copy without automatically copying.");
        await CopyResult();
        Require(await ClipboardFixture.Text() == "Edited result for copying.", "Copy must use the current edited result.");

        using (await ClipboardLock.Acquire())
        {
            Invoke("Copy");
            await Until(() => Find<TextBlock>("Status").Text.Contains("busy"));
            Require(Find<Button>("Copy").IsEnabled, "A busy clipboard must allow Copy to be retried.");
        }
        await CopyResult();
        Require(await ClipboardFixture.Text() == "Edited result for copying.", "Copy must succeed after the clipboard is released.");

        // An oversized clipboard must obey the editor's limit rather than reaching the provider unchecked.
        string oversized = new string('x', rough.MaxLength + 500);
        await ClipboardFixture.SetText(oversized);
        rough.Text = "keep";
        Invoke("Paste");
        await Until(() => Find<TextBlock>("Status").Text.Contains("limited"));
        Require(rough.Text.Length == rough.MaxLength,
            "Paste must be capped at the rough prompt's maximum length.");
        Require(await ClipboardFixture.Text() == oversized, "A capped Paste must not write the clipboard.");
        await ClipboardFixture.SetText("Edited result for copying.");
    }

    private async Task CopyResult()
    {
        NativeDesktop.Activate(editor);
        string expected = Find<TextBox>("EnhancedPrompt").Text;
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Invoke("Copy");
            await Task.Delay(50);
            string status = Find<TextBlock>("Status").Text;
            if (status.Contains("copied") && await ClipboardFixture.Text() == expected) return;
            Require(status.Contains("busy") || status.Contains("copied"), "An unsuccessful Copy must explain retryable clipboard contention.");
        }
        throw new Exception("Copy did not recover from clipboard contention.");
    }
}
