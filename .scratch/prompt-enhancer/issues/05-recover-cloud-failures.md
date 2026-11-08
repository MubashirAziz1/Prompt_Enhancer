# 05: Recover from cloud failures

Type: task
Status: ready-for-agent
Blocked by: 01 (Enhance a rough prompt through OpenRouter)

**What to build:** Keep the user's work intact when enhancement fails, explain the failure separately from an enhanced prompt, and let the user recover and retry.

**Execution:** Implementation remains on hold until the user explicitly instructs it to begin. Ticket approval authorizes publication only.

- [ ] Invalid or missing credentials produce a distinct failure state with a path to Settings; a corrected credential can be used to retry.
- [ ] Connectivity failures, timeouts, provider errors, and empty or unusable responses do not crash the application or erase the rough prompt.
- [ ] A failure is never presented as a successful enhanced prompt or placed into the result as copyable error text.
- [ ] Retrying after recovery submits the current rough prompt and displays a successful enhanced prompt when the provider succeeds.
- [ ] Failure details do not expose the API key or unnecessarily reproduce prompt contents in logs or error messages.
- [ ] Verify controlled authentication, connectivity, and unusable-response failures followed by success through the application workflow; assert retained input and recovery rather than exact error wording.

## Comments

Depends only on ticket 01. Copy-specific behavior can be checked when ticket 02 is present; it is not a prerequisite for distinct failure handling.
