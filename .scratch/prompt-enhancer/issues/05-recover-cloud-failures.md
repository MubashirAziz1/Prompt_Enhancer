# 05: Recover from cloud failures

Type: task
Status: ready-for-human
Execution: resolved
Blocked by: 01 (Enhance a rough prompt through OpenRouter)

**What to build:** Keep the user's work intact when enhancement fails, explain the failure separately from an enhanced prompt, and let the user recover and retry.

**Execution:** Implementation explicitly authorized by the user's implement request on 2026-11-08.

- [x] Invalid or missing credentials produce a distinct failure state with a path to Settings; a corrected credential can be used to retry.
- [x] Connectivity failures, timeouts, provider errors, and empty or unusable responses do not crash the application or erase the rough prompt.
- [x] A failure is never presented as a successful enhanced prompt or placed into the result as copyable error text.
- [x] Retrying after recovery submits the current rough prompt and displays a successful enhanced prompt when the provider succeeds.
- [x] Failure details do not expose the API key or unnecessarily reproduce prompt contents in logs or error messages.
- [x] Verify controlled authentication, connectivity, and unusable-response failures followed by success through the application workflow; assert retained input and recovery rather than exact error wording.

## Comments

Depends only on ticket 01. Copy-specific behavior can be checked when ticket 02 is present; it is not a prerequisite for distinct failure handling.

## Answer

Implemented on 2026-11-08 in `b758787`. Failure feedback stays in the status
area, with an explicit Retry action and retained editable rough prompt. Missing,
malformed, and rejected credentials direct the user to Settings. Retry reloads
the saved credential and submits the current rough prompt. Timeouts have distinct
feedback; errors remain outside the enhanced prompt and Copy stays unavailable.
Malformed saved keys are rejected before HTTP submission. Existing provider
validation and safe application-owned error messages remain in use.

Warnings-as-errors compilation and all 33 controlled checks passed, including
13 recovery workflow checks. Each new failure scenario is followed by successful
recovery with revised input. No live cloud requests, paid usage, real API keys,
prompt persistence, or logging were introduced. The running application was left
open; verification used an isolated build under `.test-artifacts/cloud-recovery`
because its executable locks the default build output.

The [ticket 05 review](../cloud-failure-recovery-review.md) records zero Standards
findings and zero Spec findings. The ticket is ready for human acceptance.
