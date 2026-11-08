# 01: Migrate the enhancement workflow to WinUI 3

Type: task
Status: ready-for-human
Execution: resolved (implemented and verified on 2026-11-08)
Blocked by: None

**What to build:** A working WinUI 3 prompt editor on Windows 11 x64 where the
user can reuse or configure the protected API key, paste or type a rough prompt,
enhance it, review and edit the result, and copy it. Failures retain the draft
and recover through Settings and Retry. The interface adopts the agreed native
Fluent appearance while preserving enhancement behavior. Keep the existing
Windows Forms application runnable throughout this slice.

- [x] Recheck the development environment and provide a supported stable Microsoft build setup, including required tool installation once implementation is explicitly authorized. Record reproducible build, check, and migration-editor launch instructions; do not silently require preview tooling.
- [x] The WinUI editor launches on Windows 11 x64 with native Fluent controls, standard window styling, clear typography and spacing, grouped actions, and the rough prompt above the editable enhanced prompt. Settings is readily accessible.
- [x] The editor and Settings follow the Windows light/dark theme and accent color automatically. Verify keyboard focus/navigation, normal text selection/editing, and display scaling, recording actual conditions checked.
- [x] Settings saves and replaces the API key with the existing Windows DPAPI CurrentUser protection, credential format, and user-local location. Blank Save or Cancel preserves the saved key; the field remains blank when reopened. No plaintext credential settings are introduced.
- [x] The migrated application loads an existing legacy-format protected key without re-entry under the same Windows account. Isolated fixtures verify loading, safe replacement, compatibility with the retained Windows Forms version, and fresh-process reuse. Automated tests do not read or overwrite the production credential.
- [x] Both explicit Paste and normal keyboard paste feed the rough prompt. Enhance submits its current text, and the user can review/edit the returned result and explicitly Copy the current edited text.
- [x] Clipboard access remains explicit: launching, typing, enhancing, and editing do not modify it. Copy is available only for a nonblank result while enhancement is idle. Busy-clipboard feedback allows the action to be retried; clipboard checks restore prior contents afterward.
- [x] The provider-neutral enhancement policy and evaluated OpenRouter model remain unchanged in meaning and configuration: GPT-4.1, temperature 0.2, non-streaming requests, a 2048 output-token ceiling, and a 60-second timeout. The ceiling is not a requested output length.
- [x] Provider authentication, HTTP requests, serialization/parsing, and safe failure mapping remain outside the UI and enhancement policy. A replacement provider can drive the same WinUI workflow through the existing enhancement-provider contract.
- [x] The UI shows progress, prevents duplicate submissions and conflicting edits/actions while pending, removes stale results when enhancement starts, and restores correct action availability when work completes.
- [x] Missing, malformed, and rejected credentials; account-credit/rate limits; connectivity loss; timeouts; provider failures; and empty, malformed, truncated, or secret-echoing responses produce safe application-owned feedback outside the enhanced prompt. The rough prompt survives, and the failed result stays empty and unavailable for Copy.
- [x] Retry uses the current rough prompt and saved credential, including changes made after failure. Actual WinUI checks verify failure, draft retention, credential correction where appropriate, successful Retry, and Copy of the recovered edited result.
- [x] Repeatable tests use actual WinUI editor/Settings interactions with controlled external HTTP responses, real request/response handling, and isolated DPAPI credentials. Retain supporting HTTP checks for exact submitted text, complete policy, authentication, configuration, parsing, and safe errors. Controlled responses are not described as live model-quality evidence.
- [x] Prompt contents remain in memory only; no prompt history, credential exposure, or remote diagnostics are added to storage or errors.
- [x] Application and test builds treat compiler warnings as errors. Relevant workflow, provider, credential-compatibility, and recovery checks pass. The existing Windows Forms application and its normal launch path remain runnable, with no cutover yet.

## Comments

Approved as the first of two merged vertical slices. It combines the original
proposed enhancement, clipboard/review, and failure-recovery tickets, including
the build setup needed to demonstrate the complete path. The existing provider
and credential boundaries allow reuse or a limited port within this slice; no
separate broad prefactoring ticket is needed.

## Answer

Implemented the separate native WinUI 3 enhancement editor and Settings, reusing
the existing provider-neutral policy, OpenRouter provider, and DPAPI store without
changing their source. Stable Microsoft build tooling is installed; reproducible
setup, build, focused/full checks, and migration-editor launch scripts are available.

All 26 WinUI checks and 33 retained Windows Forms checks pass with warnings
treated as errors. Checks include real WinUI controls and native keyboard input,
17 failure/recovery cases, legacy-format and fresh-process credential reuse,
edited Copy and busy-clipboard recovery, and presentation at the actual 150%
display scale. The original clipboard is restored and its text verified.
Independent standards and specification reviews found no actionable issues.

See [verification and actual conditions](../verification.md) and
[build/launch instructions](../../../docs/winui-development.md).
The normal Windows Forms application and launch scripts remain unchanged.
Ticket 02, folder publication, floating/session migration, and cutover remain on hold.

The migration spec and ADRs 0001-0003 govern the boundaries and design. Floating
button startup, window/session controls, publication of the self-contained
folder, and final cutover are ticket 02. The Windows Forms version remains the
normal application during this slice. No new paid model-quality benchmark is
required.
