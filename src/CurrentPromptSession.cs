namespace PromptEnhancer;

public enum SessionPhase { Idle, Pending, Ended }
public enum EnhancementAction { Enhance, Retry }
public enum ResultKind { Current, Previous }
public enum RecoveryAction { None, Undo, Redo }

public sealed record PromptSessionState(
    string RoughPrompt,
    string EnhancedPrompt,
    SessionPhase Phase,
    EnhancementAction EnhancementAction,
    ResultKind ResultKind,
    RecoveryAction RecoveryAction,
    string Guidance)
{
    public bool IsIdle => Phase == SessionPhase.Idle;
    public bool HasResult => !string.IsNullOrWhiteSpace(EnhancedPrompt);
    public bool CanCopy => IsIdle && HasResult;
    public bool CanRecover => IsIdle && RecoveryAction != RecoveryAction.None;
}

// One in-memory session. Call actions on the owning context (the UI thread in WinUI).
public sealed class CurrentPromptSession
{
    private readonly IEnhancementProvider provider;
    private readonly CancellationTokenSource lifetime = new();

    public CurrentPromptSession(IEnhancementProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        this.provider = provider;
    }

    private sealed record PromptPair(string Rough, string Enhanced, EnhancementAction Action, ResultKind Result);
    private PromptPair recovery;
    public event Action Changed;

    public PromptSessionState State { get; private set; } = new(
        "", "", SessionPhase.Idle, EnhancementAction.Enhance, ResultKind.Current, RecoveryAction.None, "");

    public bool EditRoughPrompt(string text)
    {
        if (!State.IsIdle) return false;
        SetState(State with { RoughPrompt = text });
        return true;
    }

    public bool EditEnhancedPrompt(string text)
    {
        if (!State.IsIdle) return false;
        SetState(State with { EnhancedPrompt = text });
        return true;
    }

    public bool New()
    {
        if (!State.IsIdle || (State.RoughPrompt.Length == 0 && State.EnhancedPrompt.Length == 0)) return false;
        recovery = CurrentPair();
        SetState(new("", "", SessionPhase.Idle, EnhancementAction.Enhance, ResultKind.Current, RecoveryAction.Undo,
            "New prompt. Undo restores your previous prompts."));
        return true;
    }

    public bool SwapRecovery()
    {
        if (!State.CanRecover) return false;
        var restore = recovery;
        recovery = CurrentPair();
        bool redo = State.RecoveryAction == RecoveryAction.Undo;
        SetState(new(restore.Rough, restore.Enhanced, SessionPhase.Idle, restore.Action, restore.Result,
            redo ? RecoveryAction.Redo : RecoveryAction.Undo,
            redo ? "Previous prompts restored. Redo brings back the prompts you just left." : "Prompts restored. Undo is available."));
        return true;
    }

    public async Task<bool> EnhanceAsync()
    {
        if (!State.IsIdle) return false;
        if (string.IsNullOrWhiteSpace(State.RoughPrompt))
        {
            SetState(State with { Guidance = "Enter a rough prompt first." });
            return false;
        }
        var before = CurrentPair();
        SetState(State with
        {
            Phase = SessionPhase.Pending,
            EnhancementAction = EnhancementAction.Enhance,
            ResultKind = State.HasResult ? ResultKind.Previous : ResultKind.Current,
            Guidance = State.HasResult ? "Enhancing… Your previous result is kept until the new one is ready." : "Enhancing…"
        });
        try
        {
            string result = await provider.EnhanceAsync(before.Rough, EnhancementPolicy.Instructions, lifetime.Token);
            if (State.Phase == SessionPhase.Ended) return false;
            if (string.IsNullOrWhiteSpace(result)) throw new EnhancementException(EnhancementFailure.InvalidResponse);
            if (!string.IsNullOrWhiteSpace(before.Enhanced)) recovery = before;
            SetState(State with
            {
                EnhancedPrompt = result,
                Phase = SessionPhase.Idle,
                EnhancementAction = EnhancementAction.Enhance,
                ResultKind = ResultKind.Current,
                RecoveryAction = !string.IsNullOrWhiteSpace(before.Enhanced) ? RecoveryAction.Undo : State.RecoveryAction,
                Guidance = "Enhanced prompt ready. Review, edit, then Copy."
            });
            return true;
        }
        catch (EnhancementException error) { ShowFailure(error.Message); }
        catch (CredentialException error) { ShowFailure(error.Message); }
        catch { ShowFailure("Enhancement could not finish. Try again."); }
        return false;
    }

    private void ShowFailure(string message)
    {
        if (State.Phase == SessionPhase.Ended) return;
        SetState(State with
        {
            Phase = SessionPhase.Idle,
            EnhancementAction = EnhancementAction.Retry,
            Guidance = "Enhancement failed. " + message + (State.HasResult ? " Your previous result is kept." : "")
        });
    }

    public void End()
    {
        if (State.Phase == SessionPhase.Ended) return;
        recovery = null;
        SetState(new("", "", SessionPhase.Ended, EnhancementAction.Enhance, ResultKind.Current, RecoveryAction.None, ""));
        try { lifetime.Cancel(); }
        finally { lifetime.Dispose(); }
    }

    private PromptPair CurrentPair() => new(State.RoughPrompt, State.EnhancedPrompt, State.EnhancementAction, State.ResultKind);

    private void SetState(PromptSessionState state)
    {
        State = state;
        Changed?.Invoke();
    }
}
