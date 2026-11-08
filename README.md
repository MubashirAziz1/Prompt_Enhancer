# Prompt Enhancer

A native Windows application for rewriting rough English prompts. Ticket 01
provides Settings, a rough-prompt editor, Enhance, and a visible read-only result.
Floating-widget controls, result editing/copying, and session controls belong to
later tickets.

Requires Windows 10/11 with .NET Framework 4.8 or later and Windows PowerShell 5.1.
No package restore, Node/Python runtime, SDK, website, or installer is needed.

From the project directory:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run.ps1
```

Open **Settings**, enter your OpenRouter API key, and select **Save**. Type or
paste a rough prompt, then select **Enhance**. The key is reused after quitting
and relaunching under the same Windows account. Saving a new key replaces it.
The key field stays blank when reopened; blank Save/Cancel keeps the saved key.

Only Enhance sends text to the cloud. Usage charges may apply to your OpenRouter
account. Nothing reads the clipboard automatically. Input and results exist only
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
evaluate live model quality. Test credentials use unique directories under
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
