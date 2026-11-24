# Deepen the current prompt session

## Notes

- The user requested a spec for architecture candidate 1 on 2026-11-24.
- [Specification](spec.md), implemented and ready for human review. See
  [verification](verification.md) for results and desktop-environment limitations.
- This effort covers the current prompt session only; work-area placement and
  general test-session ownership are separate architecture candidates.
- The current working tree contains pre-existing UI, session, documentation and
  test changes. They were read as the behavior baseline and were not modified
  by this specification task.
- The initial specification task did not implement or run checks. The user
  subsequently requested implementation with the implement skill.

## Decisions so far

- Deepen one current prompt session module: prompt values, one recovery pair,
  semantic action/result state, enhancement lifecycle, and terminal shutdown.
- The prompt editor is its presentation adapter; actual windows, focus,
  clipboard, Settings, geometry and tray ownership stay in their existing roles.
- The user confirmed the testing scope on 2026-11-24: one shared workflow
  interface for New, Undo/Redo, Enhance/Retry, result retention and Quit, plus
  the existing real desktop and provider integration checks.
- Prefer existing provider and credential seams; do not add mock-only adapters
  or split session operations into separately exposed shallow modules.
- Preserve the current one-pair recovery behavior, including restoration of
  Retry and previous-result semantics, and persistence of recovery across
  no-op New and failed requests.
- [The spec](spec.md#testing-decisions) requires closing the review's two
  coverage gaps: restored semantic state after Undo of successful Retry, and
  no recoverable pair after Quit/relaunch.
- Existing ADRs remain applicable; no new ADR or glossary term is required.
- Implemented `CurrentPromptSession` as the one shared workflow module, with
  immutable observable state and the existing enhancement provider dependency.
  The editor renders that state and retains clipboard, focus, Settings and native
  window responsibilities.
- Window-free workflow checks run through `check.ps1 -Only CurrentPromptSession`;
  `-Only FreshProcess` makes the existing relaunch checks independently selectable.
- [Implementation verification](verification.md) records actual check results and
  the separate standards/spec reviews.

## Fog

- The implementation uses `CurrentPromptSession`, immutable `PromptSessionState`
  and a `Changed` event. The editor detaches rendering before terminal `End`.
- The full suite is not green: native tray automation could not find Hide after
  earlier native tray interactions passed. Focused Session also encountered a
  native foreground activation failure; focused Workflow encountered a clipboard
  restoration exception. Other focused groups passed; see actual evidence in
  [verification](verification.md).
- Implementation must preserve prompt edits and recovery semantics already
  present in uncommitted changes.
