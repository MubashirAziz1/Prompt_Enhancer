# 01: Route enhancement through prioritized free models

Type: task
Status: ready-for-human
Execution: resolved
Blocked by: None

Implement the [specification](../spec.md) with one failing behavior test followed
by the minimum implementation per TDD cycle. Confirm test seams before writing
tests. Remove the single-model choice from composition and use five prioritized
free models plus the automatic free router fallback in the backend.

Acceptance: authenticated requests carry the documented ordered free models;
the existing editor displays valid enhanced prompts; invalid responses and
failures remain safe; documentation describes the current routing and limitations.

## Comments

- 2026-11-24: Inspected provider, workflow tests, glossary, ADRs, and current
  OpenRouter documentation/catalog. Waiting for required test-seam confirmation.
- 2026-11-24: The user confirmed the recommended HTTP and editor workflow seams
  and review against the starting HEAD, excluding existing uncommitted UI edits.
- 2026-11-24: A subsequent live failure exposed OpenRouter's three-item array
  limit. [Ticket 02](02-respect-model-list-limit.md) corrects the original
  single-request implementation while preserving all six priorities.

## Answer

Implemented backend-owned ordered free routing: the five specified free models
followed by `openrouter/free`, sent in the `models` parameter without `model`.
Removed caller-selected models from application, test, and manual evaluation
composition. Authentication, exact prompt and policy, response validation,
transport limits, and safe failure mapping remain in place. README and
[ADR 0004](../../../docs/adr/0004-prioritized-free-model-routing.md) describe the
current routing and distinguish historical GPT-4.1 evidence from free-model quality.

Validation:

- Observed the HTTP routing behavior test fail against the old single-model
  implementation, then pass after the minimal provider change.
- All nine HTTP checks pass. The editor Workflow check passes, including clipboard
  restoration on rerun; the first run hit a clipboard cleanup allocation error.
- All 17 Recovery scenarios pass, including safe errors and current-input Retry.
- Compiler checks pass with warnings treated as errors. Standards and Spec
  reviews have zero remaining findings; their one duplication suggestion was
  resolved with a test-only expected routing contract.
- The full suite was run twice. Feature, settings, clipboard, recovery, and editor
  checks pass, but the suite exits unsuccessfully in native tray input with
  `Native shell input did not reach its expected physical point.` A Session-only
  rerun also encountered native foreground activation failure. Whole-suite
  validation remains limited by desktop input automation; no tray code was changed.
- No live requests were made; availability, server-side failover, and semantic
  quality remain outside controlled evidence.

See [Decisions so far](../map.md#decisions-so-far) for the completed routing decision
and remaining validation limitation. Existing UI edits are preserved separately.
