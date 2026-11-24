# Free-model routing

## Notes

- The user requested free OpenRouter routing, five backend priorities, and TDD.
- Existing uncommitted UI work must be preserved.
- The TDD skill requires user confirmation of test seams before tests are written.

## Decisions so far

- Use OpenRouter's ordered model fallbacks for five preferred free models and
  `openrouter/free` as the final fallback; the free router itself is random.
- The user confirmed both TDD seams on 2026-11-24: public `EnhanceAsync` at the
  HTTP boundary and the existing editor-to-provider workflow. Review against
  `7c53693f03d5dc6e5dea86dece9b571aa54c5b42`, excluding pre-existing edits.
- [Specification](spec.md); [implementation ticket](issues/01-route-free-models.md).
- [Implemented and reviewed routing](issues/01-route-free-models.md#answer): the
  provider owns all six ordered free candidates, and callers cannot select a paid
  model. HTTP, Workflow, and Recovery checks pass; see
  [ADR 0004](../../docs/adr/0004-prioritized-free-model-routing.md).

## Fog

- No live enhancement quality evaluation is part of controlled HTTP checks.
- Full-suite runs reach the native tray checks but fail to deliver mouse input
  to the expected physical point; a Session rerun also hit foreground activation
  failure. The full desktop suite is not green. This routing change does not
  modify the native tray input code.
