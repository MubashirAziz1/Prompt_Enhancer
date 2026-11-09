# Prompt Enhancer

A native Windows 11 x64 WinUI 3 application for rewriting rough English prompts.
It provides a draggable floating launcher, Settings, a rough-prompt editor,
Enhance, an editable result, explicit Paste and Copy actions, and session controls.
Starting the application a second time just brings back the existing floating
button; there is never more than one button or notification icon.

Building and running from source needs the stable WinUI toolchain described in
[WinUI build, checks, and publish](docs/winui-development.md). A
self-contained folder (`scripts\publish-winui.ps1`) runs without the .NET SDK.

From the project directory (builds, then launches the WinUI application):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run.ps1
```

There is one application: WinUI 3. Its source is in `src/`, and its test host is
in `tests/`. The earlier Windows Forms fallback has been removed.

The app starts as a small **Prompt Enhancer** button above ordinary application
windows. Click it to open the editor. Drag the button to move it; drag the
editor's central toolbar title to move the expanded window. Both stay above your other
editors while you work. Closing or minimizing the editor returns to the button.
Both current prompts, including result edits, survive closing or minimizing.
The dark floating editor opens at **420 × 530 logical pixels**, matching the
compact reference proportions. Windows scales it for your display DPI. Review
and edit the enhanced prompt above the bottom rough-prompt composer; use the
toolbar **+** to clear both prompts. Resize the window for more room; smaller
windows scroll to keep the controls available.
Select **Hide** in the editor's **…** menu to return to the small floating button,
keeping both current prompts. Right-click the floating button and choose
**Hide** to remove both floating windows. To restore, right-click the
**Prompt Enhancer** notification icon in the Windows system tray and choose
**Show widget**, or double-click that icon. Look under **Show hidden icons**
if Windows places it in the tray overflow. Click the restored button to reopen
the same prompts.

Use **+ (Clear prompts)** when enhancement is idle to empty both prompts and begin a fresh
task. Select **Quit** in the editor's **…** menu or the button/tray menu to exit and discard
session contents. Alt+F4 on the floating button also exits. Hide and Quit remain
available during enhancement; editor Hide leaves the floating button visible,
and completed work stays in the editor until you reopen it,
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
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check.ps1 -Only Workflow
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check.ps1 -Only OpenRouter
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check.ps1 -Only Recovery
```

Checks use the real WinUI controls and real DPAPI storage, with controlled
HTTP responses and a non-sensitive test credential. A fresh child process verifies
credential reuse after relaunch. A replacement integration exercises the unchanged
desktop with the baseline enhancement policy. HTTP checks cover the exact submitted
text, authentication, invalid/truncated responses, and safe errors. They do not
evaluate live model quality. Desktop checks also exercise both the Paste button
and actual Ctrl+V through Enhance, editing, and Copy, checking that the clipboard
changes only on Copy and contains the edited result. These checks temporarily
use the Windows clipboard and restore its original contents afterward.
Widget checks start the real floating experience and an isolated editor
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
Automation references come from the installed .NET Framework.
Recovery checks exercise missing, malformed, and rejected credentials through
Settings, connectivity loss, an actual HttpClient timeout, provider errors,
empty/malformed/truncated responses, and secret echoes. Every failure is followed
by a successful retry with a revised rough prompt, checking input retention,
safe feedback, and Copy availability. `Recovery` selects these checks alone;
they are also included in the full suite.
The companion process is closed and the mouse position restored after checks.
Run desktop checks on an unlocked interactive Windows desktop and leave mouse
and keyboard idle while they run.
Test credentials use unique directories under
`build\test-data` and are deleted after each check; the production credential path
is never used. The WinUI projects restore their pinned dependencies during build.

The selected configuration is `openai/gpt-4.1`, temperature 0.2, 2048 maximum
output tokens, and a 60-second timeout using non-streaming OpenRouter chat
completions. Enhancement rewrites the request rather than carrying it out,
preserves stated constraints and unresolved references, and adds only essential
task details. Simple requests stay short; structure follows complexity, without
mandatory headings or added length limits. Instructions live in the provider-neutral
`EnhancementPolicy`; model and HTTP configuration remain in `OpenRouterProvider`
and `App.xaml.cs`. The token ceiling is a transport limit, not a requested output length.

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
in `App.xaml.cs` to use the same desktop workflow and enhancement policy with another
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
