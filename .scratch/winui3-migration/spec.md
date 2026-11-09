# WinUI 3 migration for Prompt Enhancer

Status: ready-for-agent
State: Consolidated shared understanding confirmed by the user.
Execution: Tickets 01 and 02 resolved. Ticket 03 consolidates the completed WinUI application. Earlier publication-stage hold statements below are historical. See verification.md.

2026-11-09 amendment: The user requested WinUI only. The earlier requirement to
retain a Windows Forms fallback is superseded. The application lives in `src/`,
its WinUI checks in `tests/`, and the default build/check/run commands target
WinUI. See [ticket 03](issues/03-consolidate-winui-only.md).

## Problem Statement

The user relies on Prompt Enhancer while working in other applications, but its
Windows Forms interface lacks the modern native Windows appearance and polish
they want. Replacing the interface must preserve familiar enhancement behavior,
floating access, the current prompt session, and the protected API key.

The user wants this migration documented before implementation begins.

## Solution

Move the existing Windows Forms interface to WinUI 3 to provide a polished,
native Fluent appearance while preserving the established enhancement workflow.
The first migrated build targets the user's Windows 11 x64 machine. Deliver a
self-contained application folder and preserve the working Windows Forms version
until the WinUI replacement passes acceptance, with a documented fallback afterward.

## User Stories

1. As a Prompt Enhancer user, I want a modern native Fluent interface, so that the application feels consistent with Windows.
2. As a Prompt Enhancer user, I want clearer typography, spacing, and grouped actions, so that I can use the application comfortably.
3. As a Prompt Enhancer user, I want the rough prompt above the enhanced prompt, so that the familiar input-to-result flow stays easy to follow.
4. As a Prompt Enhancer user, I want the interface to follow Windows' light/dark theme, so that it matches my desktop automatically.
5. As a Prompt Enhancer user, I want the interface to use the Windows accent color appropriately, so that it feels native without extra appearance settings.
6. As a Prompt Enhancer user, I want the first migrated build to run on Windows 11 x64, so that I can use it on my current machine.
7. As a Prompt Enhancer user, I want the application to start as a small floating button, so that it stays available without occupying the full editor window.
8. As a Prompt Enhancer user, I want to drag the floating button, so that I can position it beside my work.
9. As a Prompt Enhancer user, I want dragging to remain distinct from clicking, so that moving the floating button does not accidentally open the editor.
10. As a Prompt Enhancer user, I want clicking the floating button to open the prompt editor, so that I can begin or resume an enhancement.
11. As a Prompt Enhancer user, I want both the floating button and prompt editor to stay above ordinary application windows, so that I can reach them while working elsewhere.
12. As a Prompt Enhancer user, I want to move the prompt editor using its title bar, so that arranging the window does not interfere with text selection.
13. As a Prompt Enhancer user, I want to type a rough prompt directly, so that I can express my idea in the application.
14. As a Prompt Enhancer user, I want an explicit Paste action and normal keyboard paste, so that I can bring a rough prompt from another application.
15. As a Prompt Enhancer user, I want Enhance to submit my current rough prompt, so that the result reflects what I entered.
16. As a Prompt Enhancer user, I want the existing concise English enhancement behavior, so that visual changes do not alter enhancement quality or intent.
17. As a Prompt Enhancer user, I want stated constraints and unresolved context preserved, so that enhancement does not invent facts or ask clarification questions.
18. As a Prompt Enhancer user, I want short requests to remain short and essential additions to stay task-relevant, so that results avoid unnecessary requirements.
19. As a Prompt Enhancer user, I want visible progress and duplicate-submission prevention, so that I know when enhancement is pending.
20. As a Prompt Enhancer user, I want stale results removed when a new enhancement starts, so that I do not confuse an earlier result with the current one.
21. As a Prompt Enhancer user, I want to review and edit the enhanced prompt, so that I can refine it before copying.
22. As a Prompt Enhancer user, I want Copy to use my current edited result and be available only for nonblank results while idle, so that I copy the intended text.
23. As a Prompt Enhancer user, I want clipboard access limited to explicit paste and copy actions, so that the application does not unexpectedly read or replace clipboard contents.
24. As a Prompt Enhancer user, I want safe feedback when a clipboard action cannot complete, so that I can retry it.
25. As a Prompt Enhancer user, I want Settings to remain easy to reach, so that I can configure or correct my API key.
26. As a Prompt Enhancer user, I want my existing protected API key reused, so that migration does not require entering it again.
27. As a Prompt Enhancer user, I want replacement keys stored with Windows credential protection, so that my key is not saved as ordinary plaintext settings.
28. As a Prompt Enhancer user, I want blank Save or Cancel to preserve the saved key and a reopened key field to stay blank, so that the existing Settings behavior carries over.
29. As a Prompt Enhancer user, I want the saved key to remain usable after Quit and relaunch, so that my configured account remains available.
30. As a Prompt Enhancer user, I want failures to retain my rough prompt and keep errors outside the copyable result, so that I can recover without losing my draft or copying an error.
31. As a Prompt Enhancer user, I want Retry to use the current rough prompt and saved key, so that edits and credential corrections take effect.
32. As a Prompt Enhancer user, I want safe guidance for credential, account, connectivity, timeout, and provider failures, so that I can recover without seeing secrets or remote diagnostics.
33. As a Prompt Enhancer user, I want closing or minimizing the prompt editor to return to the floating button, so that I can reduce the interface without ending my work.
34. As a Prompt Enhancer user, I want both prompts and result edits retained after closing or minimizing the editor, so that reopening resumes the current prompt session.
35. As a Prompt Enhancer user, I want Hide to remove both floating windows, so that I can temporarily clear my workspace.
36. As a Prompt Enhancer user, I want the tray menu and tray double-click to restore the floating button, so that I can return to my retained prompt session after Hide.
37. As a Prompt Enhancer user, I want an enhancement that finishes while hidden to stay hidden, so that completion does not interrupt my work.
38. As a Prompt Enhancer user, I want Clear to empty both prompts while idle, so that I can start a fresh task with the saved key.
39. As a Prompt Enhancer user, I want Quit to exit completely, remove the tray icon, and cancel pending work, so that the application stops when requested.
40. As a Prompt Enhancer user, I want a late completion after Quit to leave the application closed, so that canceled work cannot reopen it.
41. As a Prompt Enhancer user, I want relaunch to start with empty prompts while retaining the protected key, so that prompt contents stay limited to the current prompt session.
42. As a Prompt Enhancer user, I want keyboard navigation and normal text selection and editing to remain usable, so that the new interface supports my existing input habits.
43. As a Prompt Enhancer user, I want readable text and usable controls at different display scales, so that the interface works with my Windows display settings.
44. As a Prompt Enhancer user, I want to launch the published application from an ordinary folder with runtime dependencies bundled, so that routine use requires minimal runtime setup.
45. As a Prompt Enhancer maintainer, I want supported build-tool setup and repeatable build, check, publish, and run instructions, so that the WinUI version can be reproduced and maintained.
46. As a Prompt Enhancer user, I want the Windows Forms version runnable throughout migration, so that I have a working application until the replacement is verified.
47. As a Prompt Enhancer user, I want normal launching switched to WinUI only after acceptance passes and a fallback retained, so that cutover has verified behavior and a recovery path.
48. As a Prompt Enhancer maintainer, I want repeatable checks through real WinUI windows and existing provider and credential boundaries, so that regressions are caught through observable behavior.
49. As a Prompt Enhancer user, I want specification publication to preserve my implementation hold, so that application changes and tool installation wait for my instruction.

## Implementation Decisions

### Interface

- Use native Fluent controls, standard window styling, clear typography, improved
  spacing, and grouped actions.
- Keep the rough prompt above the editable enhanced prompt.
- Follow Windows light/dark theme and accent color automatically.
- Keep Settings readily accessible and present progress/failure feedback outside
  the copyable enhanced prompt.
- Retain the small draggable floating button and always-on-top prompt editor.
  Launcher clicking opens the editor; dragging moves it without opening it.
- Keep normal text selection/editing separate from window dragging.

### Behavioral parity

- Support explicit Paste and normal keyboard paste, typed rough prompts,
  Enhance, editable results, and Copy of the current edited result.
- Preserve the existing busy state, duplicate-submission prevention, Copy
  availability, stale-result removal, safe failure feedback, and Retry using
  current input and credentials.
- Closing or minimizing the editor returns to the floating button and preserves
  the current prompt session.
- Hide removes both windows; tray menu/double-click restoration brings back the
  floating button and permits reopening the retained session.
- Clear empties both prompts while idle and permits a fresh enhancement.
- Quit exits the application, removes the tray icon, discards the session, and
  cancels pending enhancement work. A later launch starts with empty prompts.
- Completing enhancement while hidden keeps the app hidden; a late completion
  after Quit must not restore a window.
- Clipboard reads/writes remain explicit; no monitoring or automatic copying.
- Prompts remain in memory only; no history or prompt logging is introduced.

These are existing product behaviors to preserve, not new feature work.

### Enhancement and credentials

- Preserve the evaluated provider-neutral enhancement policy and GPT-4.1 model
  choice recorded in ADR 0002. Retain existing OpenRouter request settings and
  safe response/failure handling while porting any runtime-specific code.
  These settings include temperature 0.2, non-streaming requests, the 2048
  output-token ceiling, and the 60-second timeout. The ceiling is not a required
  prompt length.
- Keep provider-specific HTTP, authentication, and parsing outside the desktop
  workflow and enhancement policy. A replacement provider must still drive the UI.
- Keep Settings behavior, including blank Save/Cancel preserving the key, a blank
  key field when reopened, protected key reuse after relaunch, and safe replacement.
- Preserve the existing user-local credential location, DPAPI CurrentUser
  scope, and credential file format so the same Windows account can
  reuse its saved key without re-entry.
- Prove credential compatibility with an isolated legacy-format fixture, including
  loading and replacing the key. Do not overwrite the production key during tests.

### Build and delivery

- Include supported stable Microsoft build-tool setup in the implementation work.
  The current machine has runtime packages but lacks the necessary SDK/toolchain.
- Use modern C#/.NET and WinUI XAML with the Windows App SDK. Verify supported
  stable SDK/package versions and install commands when implementation begins.
  Do not silently make optional preview tooling a prerequisite.
- Deliver an unpackaged self-contained folder, bundling .NET and Windows App SDK
  runtime dependencies. The folder may be larger than the current Framework app.
- Launch the published application from its executable folder; do not promise a
  single-file binary. Update build, check, publish, and run instructions/scripts
  to match the supported workflow.
- Document bundled-runtime servicing as part of future application updates.

### Cutover and fallback

- Keep the existing WinForms application and its launch path runnable while
  implementing and verifying WinUI.
- Switch the normal launch command to WinUI only after the agreed acceptance
  checks pass.
- Retain a documented way to run the previous WinForms version as a fallback.
- Both versions use the same protected-key location under the same account;
  preserve key compatibility rather than introducing a separate settings store.

## Testing Decisions

- The user confirmed these boundaries in Q9 and the consolidated design in Q10.
  Publication reuses that agreement; no new test seam or interview is required.
- Good tests verify observable behavior through public boundaries, use independent
  expectations, and survive internal refactoring. Do not test private handlers or
  treat policy wording assertions as evidence of live enhancement quality.
- Prefer the highest existing boundary: the running application and actual WinUI
  windows. Window lifecycle, presentation, clipboard, and keyboard scenarios are
  checks at this application boundary. Retain the external HTTP and credential
  boundaries as supporting coverage rather than adding internal seams.

The agreed checks cover:

1. Actual WinUI windows: exercise Settings, input, Enhance, progress/busy state,
   editing, explicit clipboard actions, Copy availability, Clear, and failure/Retry.
   Verify duplicate-submission prevention, stale-result removal, and retry with
   current input and credentials. Include recovery after credential correction.
   Exercise explicit Paste and keyboard paste through editing and Copy, restoring
   test-modified clipboard contents afterward.
2. Window/application lifecycle: native dragging, click-versus-drag, stacking over
   another editor, close/minimize collapse, Hide/tray restore, retained prompts,
   fresh launch, pending enhancement while hidden, and Quit cancellation/cleanup.
3. External HTTP boundary: carry over controlled authentication, submitted prompt
   and complete policy, model/settings, successful parsing, invalid/truncated
   responses, timeouts, and safe errors. These checks prove integration behavior,
   not live model quality.
   Preserve exact submitted text and complete policy, reject secret echoes, and
   keep failure feedback safe. Verify a replacement provider still drives the
   same desktop workflow through the existing enhancement-provider contract.
4. Credential boundary: legacy-format key loading, safe replacement, and relaunch
   reuse under the same Windows account using isolated fixtures.
   Verify that replacement preserves compatibility with the retained fallback;
   do not overwrite the production credential.
5. Presentation: keyboard focus/navigation and text interaction, light/dark themes,
   system accent integration, and display scaling on the Windows 11 x64 target.
6. Delivery: warnings-as-errors build/check workflow and startup from the published
   self-contained folder; document what was verified and any prerequisite found.

Existing WinForms UI tests cannot validate WinUI controls and must be adapted or
replaced at the agreed boundaries. Retain portable provider checks where practical.
Prior art includes Settings-to-enhancement and fresh-process key reuse;
Paste/keyboard-paste-to-edited-Copy workflows; native dragging, stacking, and tray
checks; current prompt session and pending-work lifecycle checks; cloud-failure
and Retry workflows; and controlled OpenRouter HTTP checks. Use controlled
external responses with real serialization/parsing and isolated DPAPI storage.

Before cutover, run targeted checks and the full migration regression suite,
compile with warnings treated as errors, and smoke-test the self-contained
published folder. Document the actual conditions verified and any limitations.

No new live model-quality benchmark is required because enhancement semantics and
model selection carry over; controlled results must not be described as live evidence.

## Out of Scope

Windows 10/ARM64 builds, new product features, saved history, provider/model
changes, a theme-settings screen, MSIX/Store publication, installers, and automatic
updates are outside this migration. Personal daily use remains the product context.
Publication does not include application changes, build-tool installation,
clipboard monitoring, automatic copying, global shortcuts, or automatic startup.

## Further Notes

The user explicitly instructed "Not implement yet." `Status: ready-for-agent`
means the migration is specified sufficiently for an agent; it does not override
the hold. This invocation authorizes publication in the local Markdown tracker
only, with no implementation-ticket breakdown or remote issue publication needed.

This document replaces the earlier interview-format spec with the required
to-spec structure. The agreed design and acceptance scope remain the same.
The current application continues to use Windows Forms until migration is
implemented and verified. Recheck the machine's build-tool state before setup.

The saved key currently resides at
`%LOCALAPPDATA%\PromptEnhancer\openrouter-key.dpapi`. Preserving that location and
format is a compatibility requirement, not permission to overwrite it in tests.

Decisions and sources:

- [Design map](map.md): interview answers and environment facts.
- [ADR 0003](../../docs/adr/0003-winui3-interface-and-folder-deployment.md): framework and deployment decision.
- [ADR 0001](../../docs/adr/0001-windows-desktop-and-provider-boundary.md): existing provider and credential boundaries; the framework/build choice is being revisited.
- [ADR 0002](../../docs/adr/0002-intent-preserving-enhancement-model.md): evaluated enhancement configuration.
- [Microsoft WinUI setup](https://learn.microsoft.com/en-us/windows/apps/get-started/start-here).
- [Microsoft self-contained deployment](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/self-contained-deploy/deploy-self-contained-apps).

The user confirmed the consolidated shared understanding and explicitly instructed
"Not implement yet." The interview is complete. Implementation and tooling
installation remain on hold until the user explicitly instructs them to begin.
