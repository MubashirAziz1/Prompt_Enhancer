# Windows desktop and provider boundary

Date: 2026-11-08
Status: Accepted

Ticket 01 establishes a native Windows application with no website, installer,
or deployment. Use C# 5 and Windows Forms on .NET Framework 4.8. Windows includes
the framework and compiler, so this first slice builds without an SDK, package
restore, or third-party runtime. The tradeoff is the older language/toolchain;
a future framework migration can keep the provider and workflow interfaces.

The desktop workflow owns Settings, input, busy state, and visible results.
`IEnhancementProvider` accepts the rough prompt and a provider-neutral enhancement
policy. The OpenRouter adapter owns authentication, HTTP/JSON, model selection,
and safe error translation. Another provider can replace it at composition time.

Store only a DPAPI-protected credential blob, using `ProtectedData` with
`DataProtectionScope.CurrentUser`. The production path is
`%LOCALAPPDATA%\PromptEnhancer\openrouter-key.dpapi`. No plaintext settings or
prompt files are written. Reopening the app decrypts the key under the same
Windows account. DPAPI protects data at rest, not against software running as
that account. Save replaces the encrypted file atomically.

Initial OpenRouter configuration: `openai/gpt-4.1-mini`, non-streaming chat
completions, temperature 0.2, at most 2048 output tokens, 60-second timeout.
The model is listed in OpenRouter's catalog at implementation time. Its listing
supports choosing it as an initial low-latency option; no live quality benchmark
has been performed. Representative quality evaluation remains ticket 06.

Only explicitly submitted text goes to OpenRouter. Prompts exist in memory only.
Provider response bodies and exception details never become user-facing errors
or logs. Invalid, empty, or truncated completions are failures, not results.

References:

- [Windows Forms](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/)
- [ProtectedData and DPAPI](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata?view=netframework-4.8)
- [Initial model](https://openrouter.ai/openai/gpt-4.1-mini)
- [Chat completions](https://openrouter.ai/docs/api/api-reference/chat/create-a-chat-completion)
