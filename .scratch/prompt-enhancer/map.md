# Prompt Enhancer Design Map

## Notes

- The user requested the grill-with-docs interview workflow, combining grilling and domain modeling.
- The initial repository contained agent instructions and domain documentation only. Ticket 01 now supplies the Windows Forms application and its controlled workflow/HTTP checks.

## Decisions so far

- Product: a floating desktop widget that accepts a pasted rough prompt and produces a structured, grammatically correct English prompt.
- Preserve the user's intent and avoid unnecessary verbosity.
- First version targets Windows only.
- Input is broken English only; output is always English. Translation from other languages is outside the agreed scope.
- Enhancement corrects and rearranges supplied text and automatically adds essential supporting details when needed. The user explicitly confirmed that rejecting clarification questions does not prohibit essential additions.
- No clarification questions in the product (latest answer supersedes earlier clarification agreement).
- Adapt structure to complexity: short paragraphs for simple requests, bullets when useful; no mandatory template headings.
- Widget flow: floating button opens an editor supporting paste or typing, enhancement, review/edit, and copy.
- First version is for the user's personal daily use before wider distribution.
- Prompts remain in the current session only; no saved prompt history in the first version.
- General-purpose enhancement across writing, coding, research, and everyday tasks, without model-specific modes initially.
- Cloud AI processing is accepted, including internet access and potential usage costs.
- The floating window or button must be draggable with the mouse and remain visible above the user's work/editor until explicitly closed or hidden.
- Start as a floating button that expands into an editor. Both forms are draggable and stay above other windows; minimizing returns to the button, hiding removes it from the screen.
- No existing API provider or key. Maintainable, extensible support for adding providers later is a requirement.
- OpenRouter is the first provider. Configure the API key in the app and store it using Windows credential protection.
- Clipboard access is explicit: paste reads text, Copy writes the enhanced prompt.
- Keep the current input and result while minimized or hidden. Clear removes them; Quit discards session content and exits. Closing the expanded editor returns to the button.
- Canonical terms are recorded in the root GLOSSARY.md.
- The current to-spec review draft is in spec-review.md. At the user's request, the later deployment phase and user stories 35-40 were removed; website, installer, and public deployment work are excluded from the ticket breakdown.
- At ticket publication, the user had approved six vertical-slice tickets with ready-for-agent triage status; implementation was then on hold. The later ticket 01 implement request superseded that hold for ticket 01.
- The user explicitly authorized [ticket 01](issues/01-enhance-through-openrouter.md) on 2026-11-08 and confirmed the desktop/HTTP test boundaries and review baseline `435da2d`.
- Ticket 01 is implemented: native Windows Forms on .NET Framework 4.8, DPAPI `CurrentUser` encrypted credential storage, and the initial OpenRouter model `openai/gpt-4.1-mini`. [ADR 0001](../../docs/adr/0001-windows-desktop-and-provider-boundary.md) records the decision; [README](../../README.md) records run/check commands.
- Compilation with warnings treated as errors and all 12 controlled checks passed, including Settings-to-result, real DPAPI, a fresh-process relaunch, and a replacement provider. No live OpenRouter request was made; model-quality evaluation remains ticket 06.
- Triage retains the five default labels. Implementation lifecycle uses `Execution: claimed/resolved`, separate from `Status:`. Ticket 01 is resolved and ready for human acceptance, unblocking its dependent tickets.
- The [ticket 01 implementation review](implementation-review.md) has no outstanding Standards or Spec findings; one documentation clarity finding was corrected.
- The user explicitly authorized [ticket 02](issues/02-review-edit-copy.md) on 2026-11-08. It is implemented in `62ade75`: explicit Paste, native Ctrl+V, editable enhanced prompts, and Copy of the current reviewed result. Clipboard access remains explicit.
- Ticket 02 is resolved and ready for human acceptance. All 14 controlled checks passed with warnings treated as errors, including both paste paths through enhancement, editing, and Copy. The [ticket 02 review](review-edit-copy-review.md) reports zero Standards findings and zero Spec findings.
- The user explicitly authorized [ticket 03](issues/03-draggable-floating-widget.md) on 2026-11-08. Commit `162dab8` adds a draggable topmost launcher around the existing topmost editor; closing or minimizing the editor returns to the launcher. All 14 existing checks pass, including the completion-order fix that restores Copy before enabling Enhance.
- The user confirmed the DesktopWorkflow test seam and review baseline for ticket 03. All 16 checks now pass. New native Windows checks verify launcher and editor dragging, normal text interaction, controlled enhancement, collapse/reopen, and actual stacking above an active companion editor in a separate process. The tests exposed and drove a fix for minimize/reopen state and size restoration.
- Ticket 03 is resolved and ready for human acceptance. Commit `61d0331` completes Windows acceptance and fixes minimize/reopen state and size restoration. The final [ticket 03 implementation review](draggable-widget-review.md) reports zero Standards findings and zero Spec findings. Ticket 04 is unblocked; its implementation still requires its own user authorization.
- The user explicitly authorized [ticket 04](issues/04-current-prompt-session.md) on 2026-11-08. Commit `540184e` adds Clear, Hide, tray restoration, and Quit; `d4c81cf` shares native tray test steps after review. Closing/minimizing/hiding preserve current prompts. Clear permits a fresh task; Quit cancels pending work and exits. Fresh-process relaunch starts empty while the protected key still authenticates. No prompt persistence or logging was added.
- Ticket 04 is resolved and ready for human acceptance. All 20 controlled checks pass with warnings treated as errors at the previously confirmed desktop/HTTP boundaries, including actual Windows tray/overflow restoration and pending-request lifecycle. The [ticket 04 review](current-prompt-session-review.md) uses pre-implementation baseline `0beca54` (proposed to the user; no alternative supplied). Tickets 05 and 06 remain outside this implementation request.

## Approved tickets

| Ticket | Blocked by | Published issue |
| --- | --- | --- |
| 01: Enhance a rough prompt through OpenRouter | None | [01](issues/01-enhance-through-openrouter.md) |
| 02: Review, edit, and copy the enhanced prompt | 01 | [02](issues/02-review-edit-copy.md) |
| 03: Use a draggable floating widget | 01 | [03](issues/03-draggable-floating-widget.md) |
| 04: Manage the current prompt session | 03 | [04](issues/04-current-prompt-session.md) |
| 05: Recover from cloud failures | 01 | [05](issues/05-recover-cloud-failures.md) |
| 06: Deliver concise, intent-preserving enhancement | 01 | [06](issues/06-concise-intent-preserving-enhancement.md) |

## Fog

- None for shared product understanding; the user confirmed the consolidated spec accurately captures their intent.
- Initial technical choices are recorded in ADR 0001. Representative live model quality/latency/cost evaluation remains ticket 06.
- Success examples, failure handling, distribution, and first-version boundaries.

## Design tree

- First round settled: Windows, broken English to English, additions allowed when needed, material ambiguity triggers clarification, adaptive output structure.
- Second round settled: automatic essential additions only, paste/type-enhance-review/edit-copy flow, personal use first, no saved history, general-purpose output.
- Third round settled: laptop example accepted, cloud processing accepted, draggable persistent overlay required. Clarification was explicitly rejected, superseding the earlier agreement.
- Fourth round settled: essential additions remain allowed, no clarification questions, provider extensibility required, floating button expands to editor.
- Final product round settled: OpenRouter first, Windows credential protection, explicit clipboard actions, session-only draft retention with Clear and Quit.
- Consolidated spec is in spec.md; shared understanding confirmed by the user.
- At interview completion, the user requested no action on the spec. The later implement request supplied the instruction to begin ticket 01.

Interview complete. Later implement requests authorized tickets 01-04; all four are complete. Other tickets retain their own scope and prerequisites.
