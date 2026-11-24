# 02: Respect OpenRouter's per-request model-list limit

Type: task
Status: ready-for-human
Execution: resolved
Blocked by: None

The user reports Enhancement failed and Retry after saving an API key. A minimal
call through the real provider reproduces HTTP 400 with six candidates; the same
call succeeds with the first three. The server validation identifies the models
array and its three-item restriction. No credential or remote body was printed.

Preserve the five preferred free models and `openrouter/free`, but send at most
three candidates per HTTP request. Try the second ordered group only after a
routing/capacity/server failure from the first. Stop for authentication, credit,
invalid completion, network failure, or cancellation. Share the client's timeout
budget across both requests. Keep the exact prompt, policy, and safe messages.
Update the original one-request decision using this live evidence.

Use the previously confirmed provider HTTP and editor workflow seams. Verify the
actual saved-key repro after the fix with a small non-sensitive prompt.

## Comments

- 2026-11-24: Reproduced through `EnhanceAsync`: six candidates return HTTP 400;
  reducing only the candidate list to three returns HTTP 200 and a valid prompt.

## Answer

The six-item request was rejected by the service's three-model array validation.
`OpenRouterProvider` now sends the first three preferences, advancing to the
remaining two preferences and `openrouter/free` only after the specified routing,
capacity, or server errors. Both groups share the client's timeout budget;
authentication, credits, invalid completions, network failures, and cancellation
stop the operation. Cancellation is checked before and after each HTTP attempt.

Validation:

- Reproduced the actual failure using the real provider and saved protected key
  with a small non-sensitive prompt. No credential or raw remote error was logged.
- Observed the bounded-fallback regression fail before implementation. All 20
  HTTP checks now pass, including every supported fallback status, authentication
  and invalid-response stops, shared timeout, and caller cancellation.
- Reran the original saved-key reproduction against the final provider: HTTP 200,
  complete assistant response, and `PROVIDER_SUCCESS`. The credential was unchanged.
- All 17 editor Recovery scenarios pass. The full suite passes HTTP, workflow,
  recovery, settings, clipboard, and appearance checks, but exits later in native
  tray automation: `Native tray hide: Shell element unavailable: Hide`. Whole-suite
  validation remains limited by desktop automation, as before this correction.
- Build/typecheck passes with warnings as errors. Standards and Spec reviews have
  zero remaining findings. The editor's controlled transport now recognizes both
  groups; the regression separately verifies their exact order.
- The temporary live diagnostic probe was removed. Live availability and semantic
  quality remain variable; this successful request is transport evidence only.

[ADR 0004](../../../docs/adr/0004-prioritized-free-model-routing.md) and the spec
now record the service restriction and corrected request contract. See
[Decisions so far](../map.md#decisions-so-far). The app was rebuilt; an already
running process must be quit and reopened to load the corrected provider.
