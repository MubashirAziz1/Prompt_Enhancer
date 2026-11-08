# 01: Enhance a rough prompt through OpenRouter

Type: task
Status: claimed
Blocked by: None (dependency-free)

**What to build:** A working Windows application where the user configures an OpenRouter API key, types a rough prompt, requests enhancement, and sees an enhanced prompt. Keep provider-specific behavior replaceable from the first working flow.

**Execution:** Implementation explicitly authorized by the user's implement request on 2026-11-08.

- [ ] The application launches on Windows with a rough-prompt editor, an explicit Enhance action, and a visible result.
- [ ] Settings accepts the user's OpenRouter API key and stores it using Windows credential protection, without saving it in plaintext settings or exposing it in logs or errors.
- [ ] The saved credential can be used after relaunch without re-entry.
- [ ] Enhance sends the submitted rough prompt through OpenRouter using a supported initial model and displays the returned enhanced prompt. Final model-quality evaluation belongs to ticket 06.
- [ ] The baseline enhancement policy corrects and organizes English, preserves intent, permits essential additions, avoids unnecessary verbosity and invented facts, and does not ask clarification questions.
- [ ] Provider-specific authentication, requests, responses, and failures are separate from the desktop workflow and enhancement policy; a controlled replacement integration can drive the same workflow without rewriting it.
- [ ] Verify the complete Settings-to-enhanced-prompt workflow with controlled cloud responses and non-sensitive test credentials. A live OpenRouter check can be performed separately with an explicitly configured account.
- [ ] Record the chosen Windows framework, credential-protection mechanism, initial model configuration, and local run/check instructions. Website, installer, and deployment work are excluded.

## Comments

Approved as the first vertical slice. No existing application or test suite was found, so this ticket establishes the minimum runnable application and its first observable workflow rather than creating separate foundation-only tickets.
