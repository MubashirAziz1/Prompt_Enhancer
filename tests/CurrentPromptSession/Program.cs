using PromptEnhancer;

await Check("Fresh session starts empty without recovery", FreshSession);
await Check("New and Undo/Redo swap edited prompts with one recovery pair", NewAndRecovery);
await Check("Enhancement submits exact input once and protects pending work", EnhancementWorkflow);
await Check("Failure retains reviewed work and Retry recovery restores semantic state", FailureAndRetry);
await Check("Unusable completions and credential errors retain prompts and recovery", InvalidCompletions);
await Check("First success preserves an existing recovery pair and its swap direction", FirstSuccessRecovery);
await Check("End cancels pending work, discards prompts and ignores late completion", EndSession);

static async Task EndSession()
{
    foreach (bool failLate in new[] { false, true })
    {
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken submittedToken = default;
        var provider = new ControlledProvider { Respond = (_, _, cancellation) => { submittedToken = cancellation; return completion.Task; } };
        var session = new CurrentPromptSession(provider);
        session.EditRoughPrompt("Recoverable task.");
        session.EditEnhancedPrompt("Recoverable reviewed result.");
        session.New();
        session.SwapRecovery();
        var operation = session.EnhanceAsync();
        session.End();
        session.End();
        Require(submittedToken.IsCancellationRequested && session.State.Phase == SessionPhase.Ended &&
            session.State.RoughPrompt == "" && session.State.EnhancedPrompt == "" && session.State.RecoveryAction == RecoveryAction.None &&
            !session.State.IsIdle && !session.State.CanCopy && !session.State.CanRecover,
            "Ending must be idempotent, request provider cancellation and discard accessible prompts and recovery.");
        Require(!session.EditRoughPrompt("Revive") && !session.EditEnhancedPrompt("Revive") && !session.New() &&
            !session.SwapRecovery() && !await session.EnhanceAsync() && provider.Submissions.Count == 1,
            "Ended sessions must reject all subsequent actions.");
        if (failLate) completion.SetException(new Exception("Late remote failure."));
        else completion.SetResult("Late remote success.");
        Require(!await operation && session.State.Phase == SessionPhase.Ended && session.State.RoughPrompt == "" &&
            session.State.EnhancedPrompt == "" && session.State.Guidance == "" && !session.State.CanRecover,
            "Late success or failure cannot revive an ended session or publish failure guidance.");
        var fresh = new CurrentPromptSession(provider);
        Require(fresh.State.IsIdle && fresh.State.RoughPrompt == "" && fresh.State.EnhancedPrompt == "" && !fresh.State.CanRecover,
            "The next application lifetime starts with no prompts or recovery.");
        fresh.End();
    }
}

static async Task FirstSuccessRecovery()
{
    var provider = new ControlledProvider { Respond = (_, _, _) => Task.FromResult("First result.") };
    var session = new CurrentPromptSession(provider);
    session.EditRoughPrompt("Saved task.");
    session.EditEnhancedPrompt("Saved reviewed result.");
    session.New();
    session.EditRoughPrompt("New draft.");
    await session.EnhanceAsync();
    session.SwapRecovery();
    Require(session.State.RoughPrompt == "Saved task." && session.State.EnhancedPrompt == "Saved reviewed result.",
        "First success with no prior result must not overwrite the pair left by New.");
    session.EditEnhancedPrompt("");
    Require(session.State.RecoveryAction == RecoveryAction.Redo, "Edits must preserve the current swap direction.");
    await session.EnhanceAsync();
    Require(session.State.RecoveryAction == RecoveryAction.Redo, "First success without a nonblank prior result must also preserve Redo.");
    session.SwapRecovery();
    Require(session.State.RoughPrompt == "New draft." && session.State.EnhancedPrompt == "First result.", "The preserved alternate must remain recoverable.");
    session.End();
}

static async Task InvalidCompletions()
{
    foreach (var error in new Exception[] { null, new CredentialException() })
    {
        foreach (string result in new[] { null, "", " \t\r\n" })
        {
            var provider = new ControlledProvider { Respond = (_, _, _) => error == null ? Task.FromResult(result) : throw error };
            var session = new CurrentPromptSession(provider);
            session.EditRoughPrompt("Original task.");
            session.New();
            session.EditRoughPrompt("Current task.");
            session.EditEnhancedPrompt("My reviewed result.");
            Require(!await session.EnhanceAsync() && session.State.IsIdle && session.State.CanCopy &&
                session.State.EnhancedPrompt == "My reviewed result." && session.State.RoughPrompt == "Current task." &&
                session.State.ResultKind == ResultKind.Previous && session.State.EnhancementAction == EnhancementAction.Retry &&
                session.State.Guidance.Contains(error == null ? "complete enhanced prompt" : "Settings"),
                "Null, empty, whitespace completions and protected-key failures must remain failures with retained work and safe guidance.");
            session.SwapRecovery();
            Require(session.State.RoughPrompt == "Original task." && session.State.EnhancedPrompt == "", "Failure cannot consume prior recovery.");
            session.End();
        }
    }
}

static async Task FailureAndRetry()
{
    var provider = new ControlledProvider { Respond = (_, _, _) => throw new EnhancementException(EnhancementFailure.Authentication) };
    var session = new CurrentPromptSession(provider);
    session.EditRoughPrompt("Earlier task.");
    session.EditEnhancedPrompt("Earlier reviewed result.");
    session.New();
    session.EditRoughPrompt("Keep this draft.");
    Require(!await session.EnhanceAsync() && session.State.IsIdle && !session.State.CanCopy && session.State.EnhancedPrompt == "" &&
        session.State.EnhancementAction == EnhancementAction.Retry && session.State.Guidance.Contains("Settings"),
        "Failure without a result must retain input, offer safe recovery and keep failure text out of the result.");
    session.EditEnhancedPrompt("Reviewed result retained after failure.");
    var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    provider.Respond = (_, _, _) => completion.Task;
    var operation = session.EnhanceAsync();
    completion.SetException(new Exception("REMOTE diagnostic non-sensitive-secret"));
    Require(!await operation && session.State.RoughPrompt == "Keep this draft." && session.State.CanCopy &&
        session.State.EnhancedPrompt == "Reviewed result retained after failure." && session.State.ResultKind == ResultKind.Previous &&
        session.State.Guidance == "Enhancement failed. Enhancement could not finish. Try again. Your previous result is kept.",
        "Unexpected asynchronous failures must retain reviewed work and expose only application-owned guidance.");
    session.SwapRecovery();
    Require(session.State.RoughPrompt == "Earlier task." && session.State.EnhancedPrompt == "Earlier reviewed result.",
        "Failures and ordinary edits must preserve the earlier recovery pair.");
    session.SwapRecovery();
    session.EditRoughPrompt("Revised draft for Retry.");
    provider.Respond = (_, _, _) => Task.FromResult("Successful Retry result.");
    Require(await session.EnhanceAsync() && provider.Submissions.Last() == ("Revised draft for Retry.", EnhancementPolicy.Instructions),
        "Retry must submit revised input through the existing provider.");
    session.SwapRecovery();
    Require(session.State.RoughPrompt == "Revised draft for Retry." && session.State.EnhancedPrompt == "Reviewed result retained after failure." &&
        session.State.EnhancementAction == EnhancementAction.Retry && session.State.ResultKind == ResultKind.Previous && session.State.CanCopy,
        "Undo successful Retry must restore Retry and previous-result semantics with the reviewed prompts.");
    session.SwapRecovery();
    Require(session.State.EnhancedPrompt == "Successful Retry result." && session.State.EnhancementAction == EnhancementAction.Enhance &&
        session.State.ResultKind == ResultKind.Current, "Redo successful Retry must restore Enhance/current-result semantics.");
    session.SwapRecovery();
    session.New();
    session.SwapRecovery();
    Require(session.State.EnhancementAction == EnhancementAction.Retry && session.State.ResultKind == ResultKind.Previous,
        "Undo New must also restore Retry and previous-result semantics.");
    session.End();
}

static async Task EnhancementWorkflow()
{
    var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    var provider = new ControlledProvider { Respond = (_, _, _) => completion.Task };
    var session = new CurrentPromptSession(provider);
    session.EditRoughPrompt("Recoverable old draft.");
    session.New();
    session.EditRoughPrompt(" \t\r\n");
    Require(!await session.EnhanceAsync() && provider.Submissions.Count == 0 && session.State.Guidance == "Enter a rough prompt first." &&
        session.State.RoughPrompt == " \t\r\n" && session.State.CanRecover, "Blank submission must offer guidance without sending text or consuming recovery.");
    const string rough = "  compare \"these\" laptops\r\nfor café work \\ research  ";
    session.EditRoughPrompt(rough);
    session.EditEnhancedPrompt("Reviewed prior result.");
    var operation = session.EnhanceAsync();
    Require(provider.Submissions.Count == 1 && provider.Submissions[0] == (rough, EnhancementPolicy.Instructions),
        "Enhancement must submit exact rough text and the complete provider-neutral policy.");
    Require(session.State.Phase == SessionPhase.Pending && !session.State.IsIdle && !session.State.CanCopy && !session.State.CanRecover &&
        session.State.EnhancedPrompt == "Reviewed prior result." && session.State.Guidance.Contains("Enhancing"),
        "Pending state must expose progress, retain the prior result and disable conflicting actions.");
    Require(!session.EditRoughPrompt("Conflicting input.") && !session.EditEnhancedPrompt("Conflicting result.") &&
        !session.New() && !session.SwapRecovery() && !await session.EnhanceAsync() && provider.Submissions.Count == 1,
        "The session boundary must reject edits, New, recovery and duplicate enhancement while pending.");
    completion.SetResult("Complete replacement.");
    Require(await operation && session.State.IsIdle && session.State.CanCopy && session.State.CanRecover &&
        session.State.RoughPrompt == rough && session.State.EnhancedPrompt == "Complete replacement." &&
        session.State.EnhancementAction == EnhancementAction.Enhance && session.State.ResultKind == ResultKind.Current,
        "Success replaces the result, restores idle actions and offers recovery.");
    session.EditEnhancedPrompt("Reviewed replacement.");
    session.SwapRecovery();
    Require(session.State.RoughPrompt == rough && session.State.EnhancedPrompt == "Reviewed prior result.", "Undo must restore the pre-request pair.");
    session.SwapRecovery();
    Require(session.State.EnhancedPrompt == "Reviewed replacement.", "Redo must restore reviewed edits made to the replacement.");
    session.End();
}

static Task NewAndRecovery()
{
    var session = new CurrentPromptSession(new ControlledProvider());
    session.EditRoughPrompt("First rough prompt.");
    session.EditEnhancedPrompt("Reviewed first result.");
    Require(!session.State.CanRecover && session.State.CanCopy, "Ordinary edits enable Copy without creating recovery.");
    Require(session.New() && session.State.RoughPrompt == "" && session.State.EnhancedPrompt == "" &&
        session.State.RecoveryAction == RecoveryAction.Undo && !session.State.CanCopy,
        "New clears the prompts and offers Undo.");
    Require(!session.New() && session.State.CanRecover, "Repeated New on an empty pair must preserve recovery.");
    session.SwapRecovery();
    Require(session.State.RoughPrompt == "First rough prompt." && session.State.EnhancedPrompt == "Reviewed first result." &&
        session.State.RecoveryAction == RecoveryAction.Redo, "Undo must restore both reviewed prompts and offer Redo.");
    session.EditEnhancedPrompt("Edited after Undo.");
    session.SwapRecovery();
    Require(session.State.RoughPrompt == "" && session.State.EnhancedPrompt == "", "Redo must restore the empty pair.");
    session.SwapRecovery();
    Require(session.State.EnhancedPrompt == "Edited after Undo.", "A swap must save edits made after restoration.");
    session.EditRoughPrompt("Second rough prompt.");
    session.EditEnhancedPrompt("Second result.");
    session.New();
    session.SwapRecovery();
    session.SwapRecovery();
    session.SwapRecovery();
    Require(session.State.RoughPrompt == "Second rough prompt." && session.State.EnhancedPrompt == "Second result.",
        "Later New must replace the single alternate; repeated swaps cannot reach the first task.");
    session.EditRoughPrompt(" \t");
    session.EditEnhancedPrompt("\r\n ");
    Require(!session.State.CanCopy && session.New(), "Whitespace cannot be copied but still counts as content for New.");
    session.SwapRecovery();
    Require(session.State.RoughPrompt == " \t" && session.State.EnhancedPrompt == "\r\n ", "Recovery preserves exact whitespace.");
    session.End();
    return Task.CompletedTask;
}

static Task FreshSession()
{
    var session = new CurrentPromptSession(new ControlledProvider());
    Require(session.State.RoughPrompt == "" && session.State.EnhancedPrompt == "" && session.State.IsIdle &&
        session.State.EnhancementAction == EnhancementAction.Enhance && session.State.ResultKind == ResultKind.Current &&
        session.State.RecoveryAction == RecoveryAction.None && !session.State.CanRecover && !session.State.CanCopy,
        "A new session must expose empty prompts, idle Enhance/current-result semantics and no recovery or Copy.");
    session.End();
    return Task.CompletedTask;
}

static async Task Check(string name, Func<Task> check)
{
    await check();
    Console.WriteLine("PASS " + name);
}

static void Require(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

internal sealed class ControlledProvider : IEnhancementProvider
{
    public readonly List<(string Rough, string Policy)> Submissions = [];
    public Func<string, string, CancellationToken, Task<string>> Respond = (_, _, _) =>
        throw new Exception("This workflow must not invoke the provider.");

    public Task<string> EnhanceAsync(string roughPrompt, string policy, CancellationToken cancellation)
    {
        Submissions.Add((roughPrompt, policy));
        return Respond(roughPrompt, policy, cancellation);
    }
}
