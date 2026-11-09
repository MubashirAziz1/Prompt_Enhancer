# Ticket 01 verification

Date: 2026-11-08. Implementation was explicitly authorized by the user's
`implement` invocation for ticket 01. Ticket 02 remains on hold.

## Environment and reproducibility

- Windows 11 Pro 25H2, x64, build 26200; unlocked interactive desktop.
- Stable Visual Studio Build Tools 2026 18.11.3; MSBuild 18.11.1.51303.
- Stable .NET SDK 10.0.403; .NET runtime 10.0.14.
- Stable Windows SDK build tools 10.0.26100.9457 and Windows App SDK 2.6.5.
- These tools were installed for this slice. No preview tool or winapp CLI is
  required. The managed XAML build skips its unused C++ metadata probe; see
  [build details](../../docs/winui-development.md).
- Reproduce using `scripts/build-winui.ps1`, `scripts/check-winui.ps1`, and
  `scripts/run-winui.ps1`. `check-winui.ps1 -Only Settings`, `Workflow`,
  `Recovery`, or `Appearance` selects focused checks.

## Application, HTTP, and credential checks

The real WinUI editor and Settings dialog are driven through native automation
peers and actual Windows keyboard input, with isolated credential fixtures and
controlled HTTP responses. The provider, policy, and credential source files are
linked unchanged from the retained application.

Checks cover exact current rough text (quotes, Unicode, line breaks, backslashes),
the complete provider-neutral policy, bearer authentication, GPT-4.1,
temperature 0.2, non-streaming requests, and the 2048 output-token ceiling.
Production startup retains the 60-second timeout and redirect prohibition.
The timeout scenario uses a short test-only HttpClient timeout and a transport
that waits for cancellation, exercising real timeout/error handling.

The 17 failure/recovery cases cover missing/malformed/rejected/forbidden keys,
corrupt protected blobs, credits, rate limits, connectivity, timeout, provider
failure, empty/malformed/truncated/secret-echo responses, provider error envelopes,
absent choices, and unexpected message roles. Each retains the draft, leaves no
copyable error, checks safe guidance, and retries with revised text and the
current saved key. Credential corrections go through actual Settings. Recovered
results are edited and copied.

Clipboard checks cover explicit Paste, native Ctrl+V, no automatic clipboard
writes during launch/enhancement/editing, whitespace Copy availability, progress,
duplicate prevention, conflicting actions, stale-result removal, edited Copy,
and an externally held clipboard followed by recovery. Tests restore the prior
OLE clipboard object and check its original text. Test-owned clipboard setup
retries transient Windows contention.

A .NET Framework process writes the legacy DPAPI fixture. WinUI loads it without
re-entry, replaces it in Settings, and independent Framework and WinUI processes
reuse the replacement. Blank Save, Cancel, validation, and an unsuccessful atomic
replacement preserve the prior key. No automated check uses the production path.
A replacement provider also drives the unchanged WinUI workflow through
`IEnhancementProvider`.

Controlled completions are integration evidence, not live model-quality evidence.
No paid request, prompt history, remote diagnostic, or plaintext credential file
is introduced.

## Actual presentation conditions

- Actual monitor scale: **150%**, Windows text scale **1.00**.
- System-default application theme checked against Windows UISettings.
- Light and dark editor/Settings rendering checked by applying native element
  theme overrides in the test host; no user theme setting was changed.
- Primary action color checked against the current Windows accent palette;
  standard title-bar theme follows the editor's actual theme.
- Native Tab moves from rough prompt to Paste; Ctrl+A selects the result;
  ordinary Unicode keyboard input edits it; Copy returns the edited text.
- A 540×580 logical-pixel compact window at the actual 150% scale retains
  readable editors and scrollable actions. Window size/position are restored.
- Rendered light/dark editor and Settings snapshots were visually inspected.
  The test host regenerates them in its ignored `snapshots` output directory.
- Other monitor DPI values and live OS theme/accent setting changes were not
  exercised. The app uses native theme resources and actual-theme notifications.

## Final regression results

- `scripts/check-winui.ps1`: **26 PASS results**, no failures. Application and
  test compilation passed with zero warnings and warnings treated as errors.
- `scripts/check.ps1`: **33/33 passed**, including retained floating/window,
  session, credential, clipboard, provider, and recovery workflows.
- The normal `scripts/run.ps1` and all Windows Forms source files are unchanged.
  The independent migration-editor command is `scripts/run-winui.ps1`.
- Runtime launch in automated checks uses the WinUI test host with isolated
  credentials. The production composition root is not launched by automated
  checks, so they never open the production credential.

## Standards review

Independent review of `4c53375...dad121d`: no actionable findings.

The diff follows the documented provider boundary, DPAPI credential storage,
in-memory prompt lifecycle, domain terminology, local issue layout, and ticket 01
scope. ADR 0003 explains the departure from ADR 0001's framework choice. Windows
Forms remains the normal launch path; deployment and cutover remain deferred.
No baseline code smell warrants a change request; small repeated test sequences
remain readable in their individual scenarios.

## Spec review

Independent review of the same diff: no findings. Ticket 01 requirements appear
implemented within the agreed scope. Ticket 02 behavior remains deferred, and
verification limitations are documented.

Standards: 0 actionable findings. Spec: 0 actionable findings.


# Ticket 02 verification

Date: 2026-11-08. Implementation was authorized by the user's request to fix all
review findings and deliver a working application end to end.

## Changes made after the independent review

- **Startup robustness:** `App.OnLaunched` is wrapped, so startup failures show a
  message box and exit; a last-resort `UnhandledException` handler does the same.
  The notification icon is added with retry (Explorer not ready) and re-added with
  retry after `TaskbarCreated`; failures there no longer crash the process.
- **Single instance:** a named mutex prevents a second button/tray icon. A second
  launch signals the first (named event) to bring back its floating button.
- **Paste cap:** Paste is limited to the rough prompt's 32,000-character maximum
  (surrogate-safe) and says so in the status line; a new check covers it.
- **No show-then-resize flash:** launcher and editor are sized and placed before
  first show, using the monitor DPI; placement is re-clamped after DPI changes.
- **Launcher size:** `WM_GETMINMAXINFO` lowers the system minimum so the button is
  the intended 84×64 logical pixels (previously about 132 wide); checked at 150%.
- **Focus:** after Enhance, focus returns to the result (success) or Retry (failure).
- **Tray:** double-click no longer hides an editor that is open and in use; the menu
  opens at the notification anchor (works from the keyboard); own application icon
  (`winui/app.ico`) in the tray and executable.
- **Context menu:** opens at the pointer; drag threshold is DPI-aware.
- **Interop:** `SetLastError = true` on the `DllImport`s that throw `Win32Exception`.
- **Quit with Settings open:** the dialog is hidden as the editor closes.
- **Cutover:** `scripts/run.ps1` builds and launches WinUI; `run-winforms.ps1` is the
  fallback; README and development guide updated.

## Final results

- `scripts/build-winui.ps1`: warnings treated as errors, 0 warnings, 0 errors.
- `scripts/check-published-winui.ps1` (clean test build): **40 PASS results, 0
  failures** from the published test host: Settings/credential compatibility,
  17 recovery cases, explicit Paste/Ctrl+V/Copy (including the oversized-paste cap),
  appearance, floating drag/click, title-bar movement, stacking over a companion
  editor, close/minimize collapse, Hide, real tray menu/double-click (through
  overflow), hidden completion, Quit cancellation, and three fresh-process
  Quit/relaunch scenarios. The production executable then started from the
  published folder with app-local `coreclr.dll`, `Microsoft.ui.xaml.dll`, and
  `Microsoft.WindowsAppRuntime.dll`; a second launch handed off and exited; the
  floating startup and complete Quit check passed.
- `scripts/check.ps1` (retained Windows Forms fallback): **33/33 passed**. The
  Windows Forms source files are unchanged.
- The production application was launched with `scripts/run.ps1`; its floating
  button was visible, topmost, at the top-right of the 1920×1080 (150%) display,
  and the notification icon appeared in the tray overflow.

## Conditions and limitations

- Actual display: 1920×1080 at 150%, text scale 1.00, Windows 11 build 26200.
  Other monitor DPIs and live theme/accent changes were not exercised.
- The native-input checks drive the real mouse and keyboard and can fail
  spuriously if the desktop is touched while they run. During this work a few runs
  failed on a single input step (drag, right-click, tray probe, or a Windows Forms
  desktop step) and passed on rerun without code changes; the right-click check now
  waits for the asynchronous flyout. A stale `App.xbf` in an old test output
  directory once failed the identical-asset comparison; a clean test build fixed it.
- Touch long-press does not open the floating button's context menu; keyboard
  (Menu key / Shift+F10), mouse right-click, and the tray menu do.
- The VC++ v14 x64 redistributable remains a native prerequisite, as documented.
- Controlled responses prove workflow and transport behavior, not live model quality.
- The work is not committed; it remains in the working tree for review.

## Ticket 03 verification

2026-11-09: The user requested WinUI only, superseding the retained fallback.
The sole application is now in `src/`, and its WinUI host and provider checks
are in `tests/`. The prior `winui/`, `winui-tests/`, and `evaluation/` source roots,
Windows Forms application, duplicate entry scripts, and old Framework application
executables have been removed. Test-only Framework probes live in
`tests/fixtures/`. Existing in-progress WinUI work was preserved; the provider,
enhancement policy, and credential core were not modified.

- `scripts/build.ps1 -Tests`: passed with warnings treated as errors.
- `scripts/check-published-winui.ps1`: passed the full controlled WinUI suite,
  provider contract checks, identical published application assembly and markup
  comparison, app-local .NET/WinUI runtime checks, second-launch handoff,
  production floating startup, and full Quit.
- The first full run failed to acquire the clipboard for the busy-clipboard test
  and could not verify restoration. The complete suite passed on retry without
  changes to the clipboard test, including original clipboard restoration.
- Manual paid evaluation was ported to the WinUI test host and remains opt-in
  through `scripts/evaluate.ps1 -Live`; no paid evaluation or production-key
  access was performed during this cleanup.

Earlier fallback procedures and source-path references in this file record
historical verification; current commands are in README and the development guide.
