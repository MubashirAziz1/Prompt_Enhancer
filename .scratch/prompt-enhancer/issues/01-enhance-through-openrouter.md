# 01: Enhance a rough prompt through OpenRouter

Type: task
Status: ready-for-human
Execution: resolved
Blocked by: None (dependency-free)

**What to build:** A working Windows application where the user configures an OpenRouter API key, types a rough prompt, requests enhancement, and sees an enhanced prompt. Keep provider-specific behavior replaceable from the first working flow.

**Execution:** Implementation explicitly authorized by the user's implement request on 2026-11-08.

- [x] The application launches on Windows with a rough-prompt editor, an explicit Enhance action, and a visible result.
- [x] Settings accepts the user's OpenRouter API key and stores it using Windows credential protection, without saving it in plaintext settings or exposing it in logs or errors.
- [x] The saved credential can be used after relaunch without re-entry.
- [x] Enhance sends the submitted rough prompt through OpenRouter using a supported initial model and displays the returned enhanced prompt. Final model-quality evaluation belongs to ticket 06.
- [x] The baseline enhancement policy corrects and organizes English, preserves intent, permits essential additions, avoids unnecessary verbosity and invented facts, and does not ask clarification questions.
- [x] Provider-specific authentication, requests, responses, and failures are separate from the desktop workflow and enhancement policy; a controlled replacement integration can drive the same workflow without rewriting it.
- [x] Verify the complete Settings-to-enhanced-prompt workflow with controlled cloud responses and non-sensitive test credentials. A live OpenRouter check can be performed separately with an explicitly configured account.
- [x] Record the chosen Windows framework, credential-protection mechanism, initial model configuration, and local run/check instructions. Website, installer, and deployment work are excluded.

## Comments

Approved as the first vertical slice. No existing application or test suite was found, so this ticket establishes the minimum runnable application and its first observable workflow rather than creating separate foundation-only tickets.

## Answer

Implemented on 2026-11-08. The native Windows Forms application uses C# 5 and
.NET Framework 4.8, with Windows DPAPI `CurrentUser` credential protection.
OpenRouter's initial configuration is `openai/gpt-4.1-mini`, temperature 0.2,
2048 output tokens, and a 60-second timeout. The provider adapter owns HTTP,
authentication, JSON, and safe failures; the desktop and baseline enhancement
policy remain provider independent.

Validation: compilation with warnings treated as errors and all 12 checks passed.
The checks exercise real Windows Forms controls, real DPAPI storage, and a fresh
executable process reusing the saved test credential. Controlled HTTP checks
verify the submitted text, policy, authentication, invalid/truncated responses,
and safe errors. A replacement integration drives the same desktop flow.
No live API calls, real credentials, or paid usage were used. Live quality
evaluation remains ticket 06; a live account check is optional.

Run/check instructions are in [README](../../../README.md). The technical
decision is [ADR 0001](../../../docs/adr/0001-windows-desktop-and-provider-boundary.md).
The implementation is recorded in commit `5f87f6f`. Triage is ready for human
acceptance; execution is resolved, unblocking tickets that depend on 01.
The [implementation review](../implementation-review.md) records both review
axes: no outstanding Standards or Spec findings.
