# Deepen the current prompt session

Date: 2026-11-24
Status: ready-for-human
Execution: resolved
Type: specification
Scope: architecture candidate 1

## Problem Statement

As Prompt Enhancer gains New, Undo/Redo, Retry, and retained-result behavior,
maintainers must understand prompt editor event handlers, mutable flags, control
values, and button labels together to change the current prompt session safely.
Even the recovery snapshot includes presentation text. A change to the editor
can therefore disturb prompt recovery or action availability.

Users need their rough prompt and reviewed enhanced prompt to survive failed
enhancement, temporary hiding, and accidental New actions predictably. Existing
desktop checks cover many of these behaviors, but exercising the session rules
requires creating WinUI controls. Two observed coverage gaps concern restored
Retry/result-label state after Undo and absence of recovery state after relaunch;
these are verification gaps, not demonstrated product defects.

## Solution

Give the current prompt session one module that owns its prompt contents,
single recovery pair, enhancement lifecycle, and semantic action state. The
prompt editor uses this module and renders its state, so users retain the
existing New, Undo/Redo, Enhance/Retry, result review, and Quit experience.

Concentrate these rules behind one interface shared by the prompt editor and
behavioral tests. This gives maintainers locality for session changes and
leverage from testing complete workflows without constructing a window.
Retain actual desktop checks for native interaction and presentation.

## User Stories

1. As a user, I want a new application session to contain empty prompts and no recoverable prior pair, so that an earlier task cannot reappear after relaunch.
2. As a user, I want to type or explicitly paste a rough prompt, so that I control the text submitted for enhancement.
3. As a user, I want Enhance to submit the current rough prompt with the existing enhancement policy, so that my current intent is preserved.
4. As a user, I want a blank or whitespace-only rough prompt to receive guidance without starting enhancement, so that an empty request is not sent.
5. As a user, I want only one enhancement active at a time, so that rapid repeated actions cannot create conflicting results.
6. As a user, I want visible progress while enhancement is pending, so that I know work is underway.
7. As a user, I want conflicting editing, Paste, Copy, Settings, New, and Undo/Redo actions unavailable while enhancement is pending, so that the submitted task remains consistent.
8. As a user, I want my previous enhanced prompt to remain visible while its replacement is pending, so that reviewed work is not lost before a replacement arrives.
9. As a user, I want a successful enhancement to display an editable enhanced prompt, so that I can review and refine it before copying.
10. As a user, I want an empty or unusable completion to remain a failure, so that invalid output cannot replace my reviewed work.
11. As a user, I want enhancement failure to retain my rough prompt, so that I can recover without retyping it.
12. As a user, I want enhancement failure to retain my existing result edits as a previous result, so that reviewed work survives a failed attempt.
13. As a user, I want failure guidance separate from the enhanced prompt, so that error text is never mistaken for usable output.
14. As a user, I want an existing nonblank previous result to become copyable again after failure, so that I can continue using it while recovering.
15. As a user, I want Copy unavailable when no nonblank result exists, so that an empty result is not presented as usable work.
16. As a user, I want Retry to submit my revised rough prompt and use the current saved credential, so that corrections take effect on the next attempt.
17. As a user, I want to correct missing or rejected credentials through Settings when idle, so that I can recover through the existing workflow.
18. As a user, I want successful enhancement to return the action to Enhance and remove the previous-result label, so that completion is clear.
19. As a user, I want Undo after successful replacement to restore my previous reviewed result and the associated rough prompt, so that I can reject the replacement.
20. As a user, I want Undo after successful Retry to restore the prior Retry and previous-result state with the prompts, so that recovery is coherent.
21. As a user, I want Redo to restore the pair and action state I just left, so that I can compare the two recoverable states.
22. As a user, I want New to empty both prompts while retaining one recoverable pair, so that I can start a different task without immediately losing the previous one.
23. As a user, I want Undo after New to restore both prompts including result edits, so that an accidental New is recoverable.
24. As a user, I want Redo after undoing New to restore the empty pair I left, so that I can continue the new task.
25. As a user, I want New on an already empty pair to preserve available recovery, so that repeated clicks do not erase the prior task.
26. As a user, I want prompt edits to participate in the next session swap without creating an unbounded history, so that recovery stays limited to the existing one-pair behavior.
27. As a user, I want failed requests to preserve the available recovery pair, so that a transient failure does not consume recovery.
28. As a user, I want closing or minimizing the prompt editor to return to the floating button without discarding prompts or recovery, so that I can resume the same task.
29. As a user, I want Hide and Restore to retain the current prompt session, so that temporarily removing the windows does not end my work.
30. As a user, I want enhancement to finish while collapsed or hidden without reopening a window, so that background completion does not interrupt other work.
31. As a user, I want Close and Quit available during enhancement, so that I remain in control while waiting.
32. As a user, I want Quit to cancel pending work and discard prompts and recovery, so that late completion cannot revive an ended session.
33. As a user, I want New and Quit to preserve the separately protected credential, so that I can enhance a new task without configuring it again.
34. As a user, I want clipboard contents to change only through explicit clipboard actions, so that session transitions never silently replace my clipboard.
35. As a user, I want safe failure guidance without credentials or remote diagnostics, so that session recovery preserves the existing privacy protections.
36. As a maintainer, I want complete session workflows tested through the same interface used by the prompt editor, so that changing presentation does not require rediscovering session rules.
37. As a maintainer, I want both controlled completion and failure from the existing provider seam, so that session behavior is verified without live accounts or network calls.
38. As a maintainer, I want real desktop acceptance checks to remain in place, so that module checks cannot conceal broken focus, window, clipboard, or shutdown integration.

## Implementation Decisions

- Introduce one concrete current prompt session module for one application
  lifetime. It owns rough and enhanced prompt values, idle/pending/ended state,
  semantic Enhance/Retry state, previous-result classification, and the single
  recoverable prompt pair with its Undo/Redo direction.
- Prefer one high workflow seam over separately exposed snapshot, reducer,
  request-runner, or action-policy modules. The deletion test is whether removing
  this module would put transition knowledge back into multiple prompt editor
  handlers; a collection of pass-through getters does not satisfy the objective.
- The module interface supports prompt edits, New, the current Undo/Redo swap,
  enhancement through the existing provider, and ending the session. It exposes
  enough observable state for rendering and tests without exposing its mutable
  implementation. Exact method names, notification mechanism, and state type
  names are implementation choices.
- State uses semantic values rather than button captions, control references,
  visibility values, or translated display strings. Recoverable state contains
  the prompts and their relevant semantic action/result state. Copy availability
  and other session-derived action rules have one owner.
- The WinUI prompt editor remains the concrete presentation adapter. It forwards
  actions and edits, renders session state, and owns focus, clipboard access,
  dialogs, theme, layout, native dragging, and presentation-only visibility.
  Credential-setup feedback and Settings dialog lifetime remain in the editor;
  they are not added to prompt recovery snapshots.
- Keep window visibility and tray ownership in the existing floating experience.
  Closing, minimizing, hiding, or restoring windows does not create, replace,
  reset, or terminate the session module. Actual Quit or final editor destruction
  ends it.
- Accept the existing enhancement provider dependency. The current prompt session
  owns the enhancement workflow and its lifetime cancellation; the existing
  provider adapter continues to own authentication, credential loading, routing,
  transport, timeout, completion validation, and safe provider errors. Retain the
  provider-neutral enhancement policy.
- Capture the exact current rough prompt at enhancement start. Reject a
  blank/whitespace-only submission without invoking the provider or overwriting
  recovery. Accept only one pending operation; reject conflicting actions at the
  module interface as well as disabling them in presentation.
- Keep a prior result throughout pending work. Success replaces it with a
  nonblank enhanced prompt and returns semantic state to Enhance/current result.
  Failure retains both prompts, exposes Retry and safe guidance, and marks a
  retained nonblank result as previous. An unexpected failure receives existing
  application-owned generic guidance; raw exception details never become output.
- New on a pair containing any text captures that pair for recovery, clears both
  prompts, and returns semantic state to Enhance/current result. New on two empty
  strings is a no-op that preserves the recovery pair. Whitespace-only text still
  counts as content for New, matching the current behavior.
- Successful replacement establishes a recovery pair only when the prior
  enhanced prompt was nonblank. It captures the pre-request prompts and semantic
  state, including Retry and previous-result classification where applicable.
  First success without a prior nonblank result does not overwrite an existing
  recovery pair left by New.
- A failure, blank submission, or ordinary edit does not overwrite recovery.
  Ordinary edits do not create recovery entries or turn this feature into typing
  history. An Undo/Redo swap saves the current edited pair before restoring the
  alternate one, preserving the current session-swap behavior.
- Recovery is limited to one alternate prompt pair. New with content or
  successful replacement of a nonblank result replaces that alternate and resets
  the action to Undo. Undo/Redo is unavailable when no alternate exists and while
  enhancement is pending or the session has ended.
- Restoring recovery restores semantic action and previous-result state along
  with prompt values. For example, after a failed request followed by successful
  Retry, Undo restores the pre-Retry reviewed result with Retry/previous-result
  semantics; Redo restores the successful result with Enhance/current-result
  semantics. Status guidance may describe the swap rather than replay an old
  failure sentence.
- Ending a session is terminal and idempotent. It requests cancellation, clears
  accessible prompt and recovery state, and ignores late success or failure.
  This is ordinary in-memory disposal, not a claim of secure erasure of managed
  strings. Subsequent application launch creates an empty session.
- Rendering and notifications respect the WinUI thread and adapter lifetime.
  Completion may update a live hidden session but must not activate its windows.
  Once the adapter is destroyed, a late notification cannot touch its controls.
- Keep explicit Paste and Copy semantics, input length limits, empty-result
  editing behavior, and focus restoration. Prompt edits from typing and Paste
  must reach the session; rendering the session must not create duplicate edits
  or recovery transitions.
- Do not add a new substitutable adapter interface merely to mock this module.
  The existing provider seam already has OpenRouter and replacement adapters;
  use it for controlled asynchronous behavior.
- Respect the applicable decisions in ADRs 0001–0004: memory-only prompts,
  protected credentials, safe failures, WinUI-only delivery, and provider-owned
  free-model routing. This refactor does not reopen model selection or desktop
  presentation decisions.

## Testing Decisions

- A good test drives an action sequence through a module's interface and checks
  externally observable prompt contents, semantic state, available actions,
  submitted text, or cancellation. Do not assert private flags, helper calls,
  collection layout, notification counts, or snapshot representation.
- Use one new test seam: the complete current prompt session workflow shared by
  the prompt editor and behavioral tests. Exercise it without constructing WinUI
  windows, accessing the clipboard, writing credentials, or making network calls.
  This is one additional production seam, not one new seam per operation.
- Reuse the existing enhancement provider seam with controlled success, failure,
  and pending completion. Prefer completion sources over sleeps for operation
  ordering. A replacement provider is an existing adapter pattern in this
  codebase, not a new generic mocking abstraction.
- Retain the existing prompt editor-to-provider, controlled HTTP, real WinUI,
  native desktop, and fresh-process checks for their integration responsibilities.
  Session tests do not replace transport validation or actual desktop acceptance.
- Use the following acceptance matrix for the session module and adapter:

  | Scenario | Observable acceptance |
  | --- | --- |
  | Fresh session | Empty prompts, idle Enhance state, no recovery, Copy unavailable. |
  | Blank submission | Guidance, no provider invocation, no prompt or recovery change. |
  | Successful enhancement | Exact submitted rough prompt and policy; complete editable result; idle actions restored. |
  | Pending request | One provider invocation; conflicting actions rejected; prior result retained; progress represented. |
  | Failure without result | Rough prompt retained; Retry available; no copyable failure text. |
  | Failure with reviewed result | Both prompts retained; previous-result classification; Copy available after return to idle. |
  | Current-input Retry | Revised rough prompt submitted; successful result replaces old result; existing saved-key integration still works. |
  | New / Undo / Redo | Both prompts, reviewed edits, semantic action and result classification swap coherently. |
  | Repeated New while empty | Available recovery remains unchanged. |
  | Successful replacement / Undo / Redo | Prior reviewed result and pre-request rough prompt restored; new result recoverable by Redo. |
  | Successful Retry / Undo / Redo | Retry/previous-result state restored by Undo; Enhance/current-result state restored by Redo. |
  | Recovery retention | Failures, edits and first success without prior result preserve any existing recovery pair. |
  | Single recovery pair | Later New or successful replacement replaces earlier recovery; no multi-step history accumulates. |
  | Whitespace and edits | Whitespace-only results cannot be copied; ordinary edits are reflected without creating recovery entries. |
  | Collapse / minimize / Hide / Restore | Prompts and recovery remain available; no new session is constructed. |
  | Hidden completion | Success/failure retained without activating either floating window. |
  | Quit during pending work | Cancellation requested; prompts and recovery discarded; late completion/failure ignored. |
  | Relaunch after recovery existed | Empty prompts and no Undo/Redo recovery, while the protected credential still works. |
  | Explicit clipboard and focus | Existing native Paste, editable result, Copy, focus, and contention checks remain valid. |

- Close the two review gaps explicitly. Check restored Retry and previous-result
  semantics after Undo, not just restored text; create recoverable state before
  Quit and verify Undo/Redo absence after a fresh launch.
- Prior art includes the current New/Undo/Redo workflow, the controlled Recovery
  cases followed by revised-input Retry and result recovery, the replacement
  provider workflow, the pending completion/cancellation checks, and the
  fresh-process protected-key/session checks. Extend these assertions where
  appropriate rather than duplicating every provider error case at every seam.
- Keep substantive regression coverage at the shared interface when private
  helpers move. Retain desktop tests that verify rendering, edit propagation,
  focus, clipboard, close/hide/restore, tray behavior and final process exit.
- Build with compiler warnings treated as errors. Run the new session checks,
  then the affected Workflow, Recovery, Session, Hide and Appearance checks and
  the full existing suite. Report unrelated desktop-environment failures
  separately with their actual evidence; do not call an uncompleted suite green.
- Automated checks use controlled adapters and isolated non-sensitive
  credentials. Live provider calls, saved production credentials, and live
  semantic-quality evaluation are not necessary to accept this refactor.

## Out of Scope

- Architecture candidate 2: consolidating monitor selection, work-area clamping,
  window sizing, or DPI placement.
- Architecture candidate 3: restructuring the shared test application's general
  session fixture, runner, or test ordering beyond changes necessary for the
  focused session assertions.
- Changes to the floating button appearance, prompt editor layout, themes,
  dimensions, keyboard conventions, tray interactions, or delivery format.
- Additional history entries, persistence, autosave, cross-launch recovery,
  multi-session navigation, or per-keystroke Undo/Redo.
- Changes to model order, fallback eligibility, request grouping, HTTP
  configuration, credential storage, enhancement policy, or model-quality claims.
- A generic state-machine framework, event bus, repository abstraction, or new
  dependencies introduced solely for this extraction.
- Production implementation, deployment, or unrelated cleanup as part of writing
  this spec.

## Further Notes

- The user selected architecture candidate 1 for specification on 2026-11-24.
  Selecting it did not approve candidates 2 or 3 or request implementation.
- This spec treats the current working tree as the behavior baseline, including
  its uncommitted New/Undo/Redo, retained-result, and UI changes. Preserve those
  edits during implementation; older resolved tickets describe earlier behavior.
- Testing scope: confirmed by the user on 2026-11-24: one shared current prompt session workflow interface, retaining existing desktop and provider integration checks.
- The glossary already names the current prompt session; no new domain term or
  ADR is required merely to write this spec. The proposed ownership change is
  compatible with the applicable existing ADRs.
- Earlier routing notes record native tray automation failures in full-suite
  runs. That is historical context, not a fresh result from this specification
  task. Reassess the actual environment during implementation.
- This specification task inspects source and existing checks; it does not
  establish that the current application or test suite passes.

References:
- [Domain glossary](../../GLOSSARY.md)
- [Desktop and provider decision](../../docs/adr/0001-windows-desktop-and-provider-boundary.md)
- [Enhancement policy decision](../../docs/adr/0002-intent-preserving-enhancement-model.md)
- [WinUI and delivery decision](../../docs/adr/0003-winui3-interface-and-folder-deployment.md)
- [Current routing decision](../../docs/adr/0004-prioritized-free-model-routing.md)
- [Effort map](map.md)
