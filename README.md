# Prompt Enhancer

WinUI migration development is available separately; see
[WinUI build, checks, and editor launch](docs/winui-development.md).
The normal launch command below continues to use Windows Forms during ticket 01.

A native Windows application for rewriting rough English prompts. Tickets 01-05
provide a draggable floating launcher, Settings, a rough-prompt editor, Enhance,
an editable result, explicit Paste and Copy actions, and session controls.

Requires Windows 10/11 with .NET Framework 4.8 or later and Windows PowerShell 5.1.
No package restore, Node/Python runtime, SDK, website, or installer is needed.

From the project directory:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run.ps1
```

The app starts as a small **Prompt Enhancer** button above ordinary application
windows. Click it to open the editor. Drag the button to move it; drag the
editor's title bar to move the expanded window. Both stay above your other
editors while you work. Closing or minimizing the editor returns to the button.
Both current prompts, including result edits, survive closing or minimizing.
Select **Hide** in the editor, or right-click the floating button and choose
**Hide**, to remove both floating windows. To restore, right-click the
**Prompt Enhancer** notification icon in the Windows system tray and choose
**Show widget**, or double-click that icon. Look under **Show hidden icons**
if Windows places it in the tray overflow. Click the restored button to reopen
the same prompts.

Use **Clear** when enhancement is idle to empty both prompts and begin a fresh
task. Select **Quit** in the editor or the button/tray menu to exit and discard
session contents. Alt+F4 on the floating button also exits. Hide and Quit remain
available during enhancement; completed work stays hidden until you restore it,
and Quit cancels pending work. Relaunch starts with empty prompts. Clear and Quit
keep your protected API key available for the next enhancement/session.

Open **Settings**, enter your OpenRouter API key, and select **Save**. Type or
paste a rough prompt (using **Paste** or Ctrl+V), then select **Enhance**. Review
and edit the enhanced prompt, then select **Copy** to put the current edited
result on the clipboard for pasting into another application. Copy is available
for nonblank results while enhancement is idle. If the clipboard is busy, retry
the action. The key is reused after quitting
and relaunching under the same Windows account. Saving a new key replaces it.
The key field stays blank when reopened; blank Save/Cancel keeps the saved key.

If enhancement fails, the rough prompt stays in the editor and the action becomes
**Retry**. Failure feedback appears in the status area; the result stays empty
and Copy is unavailable. For missing or rejected credentials, open **Settings**
and save a corrected key. For a timeout, connectivity problem, or provider error,
recover the connection/account or wait before retrying. You can edit the rough
prompt first: **Retry** sends its current text using the current saved key.

Only Enhance sends text to the cloud. Usage charges may apply to your OpenRouter
account. Clipboard access happens only through explicit paste/copy actions;
opening the app, typing, enhancing, and editing do not change the clipboard.
There is no clipboard monitoring or automatic copy. Input and results exist only
in memory and are discarded on exit.

The API key is stored as a Windows DPAPI `CurrentUser` encrypted blob at
`%LOCALAPPDATA%\PromptEnhancer\openrouter-key.dpapi`, never as plaintext settings.
It is decrypted in memory for authentication. DPAPI does not defend against
other software already running as your Windows account. No application logs,
prompt history, or remote error bodies are written. Session-only local storage
does not imply that the cloud provider has no retention policy.

Build/typecheck (compiler warnings are errors):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

Run all checks, or one confirmed test boundary:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check.ps1 -Only DesktopWorkflow
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check.ps1 -Only OpenRouter
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check.ps1 -Only CloudRecovery
```

Checks use the real Windows Forms controls and real DPAPI storage, with controlled
HTTP responses and a non-sensitive test credential. A fresh child process verifies
credential reuse after relaunch. A replacement integration exercises the unchanged
desktop with the baseline enhancement policy. HTTP checks cover the exact submitted
text, authentication, invalid/truncated responses, and safe errors. They do not
evaluate live model quality. Desktop checks also exercise both the Paste button
and actual Ctrl+V through Enhance, editing, and Copy, checking that the clipboard
changes only on Copy and contains the edited result. These checks temporarily
use the Windows clipboard and restore its original contents afterward.
Widget checks start the real floating application context and an isolated editor
in a separate process. Native Windows mouse input verifies launcher dragging,
click-to-expand, title-bar movement, and text selection. The companion editor
accepts typing while window hit-testing verifies the widget stays above it;
controlled enhancement, Alt+F4 collapse, minimize/reopen, and exit are also checked.
Session checks preserve reviewed results through collapse/reopen and Hide/Restore.
Windows UI Automation locates the real notification icon (including tray overflow)
and native mouse input opens its restore menu. Clear is followed by a fresh
enhancement using the saved key. Two separate application processes exercise Quit
and relaunch: prompts start empty and the protected key still authenticates.
Pending work is checked through Hide/Restore and Quit as well. Test-only UI
Automation references come from the installed .NET Framework; no extra download
is required.
Recovery checks exercise missing, malformed, and rejected credentials through
Settings, connectivity loss, an actual HttpClient timeout, provider errors,
empty/malformed/truncated responses, and secret echoes. Every failure is followed
by a successful retry with a revised rough prompt, checking input retention,
safe feedback, and Copy availability. `CloudRecovery` selects these checks alone;
they are also included in `DesktopWorkflow` and the full suite.
The companion process is closed and the mouse position restored after checks.
Run desktop checks on an unlocked interactive Windows desktop and leave mouse
and keyboard idle while they run.
Test credentials use unique directories under
`build\test-data` and are deleted after each check; the production credential path
is never used. No external packages or test framework downloads are needed.

The selected configuration is `openai/gpt-4.1`, temperature 0.2, 2048 maximum
output tokens, and a 60-second timeout using non-streaming OpenRouter chat
completions. Enhancement rewrites the request rather than carrying it out,
preserves stated constraints and unresolved references, and adds only essential
task details. Simple requests stay short; structure follows complexity, without
mandatory headings or added length limits. Instructions live in the provider-neutral
`EnhancementPolicy`; model and HTTP configuration remain in `OpenRouterProvider`
and `Program`. The token ceiling is a transport limit, not a requested output length.

Ticket 06 evaluated ten non-sensitive examples through the actual editor and
Enhance action with a configured account. GPT-4.1 preserved intent in all ten
observed results, with 1.45-2.44 seconds end-to-end latency (1.68-second median)
and $0.00854 total API-reported usage cost. GPT-4.1 Mini was cheaper but altered
background facts and quoted text in the comparison run. This small sample does
not guarantee future outputs. See the [evaluation and limitations](.scratch/prompt-enhancer/enhancement-evaluation.md)
and [model decision](docs/adr/0002-intent-preserving-enhancement-model.md).

The desktop receives an `IEnhancementProvider` and an `ICredentialStore` through
its constructor. Provider authentication, HTTP requests, JSON responses, and
failure mapping are isolated in `OpenRouterProvider`. Replace that integration
in `Program` to use the same desktop workflow and enhancement policy with another
provider. See [the framework and security decision](docs/adr/0001-windows-desktop-and-provider-boundary.md).

A live check uses your own account configured in Settings. Submit a non-sensitive
prompt, for example `write email boss tomorrow leave personal work short`.
It should produce a concise request for an email, without writing the email itself.
To repeat the fixed ten-example evaluation with numeric token/cost telemetry:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\evaluate.ps1 -Live
```

This manual command opens the real editor and makes ten paid requests using the
saved credential. It prints the fixed examples, results, latency, and numeric
usage metadata to the console; it does not write a prompt history or change the
credential or clipboard. It refuses to run without `-Live`. Leave the evaluation
editor alone while it runs. Missing usage metadata is reported as null or omitted,
not as zero cost. No credential is supplied by the project, and automated
`check.ps1` checks never call OpenRouter or incur charges.
