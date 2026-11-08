# Prompt Enhancer

A native Windows application for rewriting rough English prompts. Tickets 01-03
provide a draggable floating launcher, Settings, a rough-prompt editor, Enhance,
an editable result, and explicit Paste and Copy actions. Session controls belong
to ticket 04.

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
To exit, focus the floating button and press **Alt+F4**. Hide, Clear, and a
dedicated Quit control belong to ticket 04.

Open **Settings**, enter your OpenRouter API key, and select **Save**. Type or
paste a rough prompt (using **Paste** or Ctrl+V), then select **Enhance**. Review
and edit the enhanced prompt, then select **Copy** to put the current edited
result on the clipboard for pasting into another application. Copy is available
for nonblank results while enhancement is idle. If the clipboard is busy, retry
the action. The key is reused after quitting
and relaunching under the same Windows account. Saving a new key replaces it.
The key field stays blank when reopened; blank Save/Cancel keeps the saved key.

Only Enhance sends text to the cloud. Usage charges may apply to your OpenRouter
account. Clipboard access happens only through explicit paste/copy actions;
opening the app, typing, enhancing, and editing do not change the clipboard.
There is no clipboard monitoring or automatic copy. Input and results exist only
in memory and are discarded on exit.

The API key is stored as a Windows DPAPI `CurrentUser` encrypted blob at
`%LOCALAPPDATA%\PromptEnhancer\openrouter-key.dpapi`, never as plaintext settings.
It is decrypted in memory for authentication. DPAPI does not defend against
other software already running as your Windows account. No application logs,
prompt history, or remote error bodies are written.

Build/typecheck (compiler warnings are errors):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

Run all checks, or one confirmed test boundary:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check.ps1 -Only DesktopWorkflow
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check.ps1 -Only OpenRouter
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
The companion process is closed and the mouse position restored after checks.
Run desktop checks on an unlocked interactive Windows desktop and leave mouse
and keyboard idle while they run.
Test credentials use unique directories under
`build\test-data` and are deleted after each check; the production credential path
is never used. No external packages or test framework downloads are needed.

The initial configuration is `openai/gpt-4.1-mini`, temperature 0.2, 2048 output
tokens, and a 60-second timeout using non-streaming OpenRouter chat completions.
This is an initial supported model, not a completed quality/latency evaluation;
ticket 06 owns that work. Model configuration lives in `OpenRouterProvider` and
`Program`; the baseline English rewrite instructions live in `EnhancementPolicy`.

The desktop receives an `IEnhancementProvider` and an `ICredentialStore` through
its constructor. Provider authentication, HTTP requests, JSON responses, and
failure mapping are isolated in `OpenRouterProvider`. Replace that integration
in `Program` to use the same desktop workflow and enhancement policy with another
provider. See [the framework and security decision](docs/adr/0001-windows-desktop-and-provider-boundary.md).

A live check is optional: configure your own account in Settings and submit a
non-sensitive prompt, for example `write email boss tomorrow leave personal work short`.
It should produce a concise request for an email, without writing the email itself.
No real credential is supplied by the project, and automated checks must not call
OpenRouter or incur charges.
