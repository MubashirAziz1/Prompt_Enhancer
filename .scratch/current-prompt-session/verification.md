# Current prompt session implementation verification

Date: 2026-11-24

## Implementation

`src/CurrentPromptSession.cs` owns prompt edits, the single recovery pair,
semantic Enhance/Retry and result classification, enhancement workflow and
terminal cancellation. Its immutable state supplies action availability to the
editor. Recovery stores prompts and semantic action/result values, without
controls, captions or credential feedback.

The editor forwards edits and actions, suppresses edit feedback during rendering,
and detaches notifications before ending the session on final destruction.
Collapse, minimize, Hide and Restore retain the same module. Clipboard, Settings,
focus, theme, dragging, layout and native window ownership remain in their
existing adapters. Provider routing, authentication and completion validation
remain unchanged; the session also rejects blank replacement-provider results.

## Checks

- `scripts/check.ps1 -Only CurrentPromptSession`: passed all seven workflow
  checks without WinUI, credentials, clipboard or network access. Each slice was
  run failing before implementation, then passing. Checks cover fresh state,
  New/Undo/Redo, edited recovery, exact submission and policy, blank input,
  duplicate/pending guards, safe failures, revised-input Retry, semantic recovery,
  invalid completions, retained recovery and direction, cancellation, and ignored
  late success/failure.
- `scripts/build.ps1 -Tests`: passed with compiler warnings treated as errors,
  including both WinUI projects and the independent desktop/credential fixtures.
- An isolated export of the staged index also passed that build and all seven
  window-free checks. The commit therefore compiles without the unrelated
  unstaged launcher appearance changes. Related baseline session/editor changes
  are included; the launcher appearance code, assets and checks remain unstaged.
- Workflow: all workflow assertions passed, including explicit Paste, native
  Ctrl+V, retained pending result, idle restoration, edited Copy and clipboard
  contention. That run exited 1 during cleanup: `ClipboardFixture.Text()` threw
  `OutOfMemoryException` while `Restore()` checked restored clipboard text.
  It is not reported as a green run.
- Recovery: passed all 17 controlled error/recovery scenarios and clipboard
  restoration. Undo now explicitly checks Retry, Previous result and Copy;
  Redo checks the current-result label and Enhance action.
- Hide: passed repeated collapse/reopen with retained recovery, completion while
  collapsed, failure while both windows are hidden, and final destruction on
  Quit. Hidden failure retained reviewed work, Retry semantics and recovery
  without showing either window.
- Appearance: passed replacement-provider integration, native keyboard editing
  and focus, editable Copy, Settings, empty-state editing and compact/expanded
  layout checks at the available 150% display scale. Clipboard restoration passed.
- Session: New/Undo/Redo, repeated collapse and pending completion passed. The
  run then exited 1 in the unchanged native foreground activation path:
  `NativeDesktop.Activate` reported `Keyboard input must target only the WinUI
  test window` from `AssertAboveCompanion(launcher, companion)`. Subsequent
  dragging/tray checks in that run were not reached.
- FreshProcess: passed all three modes (`pending-quit`, `launcher-close`,
  `tray-quit`). Each process first checks empty prompts and absent/disabled
  Undo/Redo, enhances with the isolated protected key, establishes recovery and
  verifies it before quitting. The following process checks recovery absence
  after relaunch. Cancellation, complete exit and notification-icon removal
  passed, as did clipboard restoration.

Focused desktop groups after the Workflow command were run sequentially from the
rebuilt WinUI executable with `--only <group>`; reports are retained locally in
`build/current-prompt-session-reports/`. No live provider requests or production
credentials were used.

`scripts/check.ps1` (full suite): exited 1 after 61 passing desktop check entries.
The seven window-free checks and
warnings-as-errors build passed. The desktop run passed the 20 provider checks,
Settings and credential compatibility, New/Undo/Redo, Workflow, all Recovery
scenarios, Appearance, collapse/hidden failure, native launcher dragging, editor
movement, text selection and stacking, and initial native tray Hide/Restore and
double-click restoration. It then stopped at `SessionChecks.cs:138` when the
shell automation probe reported `Native tray hide: Shell element unavailable:
Hide`. Clipboard restoration passed in this run. Later pending tray completion,
pending Quit and fresh-process checks in that run were not reached; focused
FreshProcess and window-free cancellation checks passed separately as recorded
above. This is an actual incomplete/failing suite, not a green acceptance claim.

## Review

The implement skill's required code review used separate fresh-context reviewers
for Standards and Spec. The fixed comparison was the preserved initial dirty
worktree, based on HEAD `2d109803e74bf15105f491ac24473329b66c615e`; unrelated
pre-existing appearance changes were excluded from the task diff.

### Standards

Documented standards: no findings. The extraction preserves the ADR provider
boundary, memory-only prompts, protected credentials, and application-owned
error guidance.

Smell baseline: no actionable findings. Session transitions and recovery now
have one owner; the editor retains presentation responsibilities without
introducing unnecessary interfaces or framework abstractions.

### Spec

No findings. The shared workflow, semantic recovery state, terminal cancellation
and guarded rendering meet the spec. Both identified coverage gaps have focused
assertions. The FreshProcess selector supports the required relaunch checks.

Review totals: Standards 0; Spec 0. Reviewers inspected code only; check results
above come from actual execution.
