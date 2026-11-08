# 06: Deliver concise, intent-preserving enhancement

Type: task
Status: ready-for-human
Execution: resolved
Blocked by: 01 (Enhance a rough prompt through OpenRouter)

**What to build:** Refine the actual enhancement behavior and choose the initial OpenRouter model so the user's rough prompts become concise, clear English prompts with useful essential additions and preserved intent.

**Execution:** Implementation explicitly authorized by the user's implement request on 2026-11-08.

- [x] Grammar, wording, and organization improve while preserving the user's goal and explicitly stated constraints.
- [x] Add essential supporting details when needed, but do not introduce unnecessary requirements, invented facts, or clarification questions.
- [x] Simple requests remain short; complex requests gain useful structure without mandatory role/context/constraints headings or an arbitrary fixed length requirement.
- [x] The rough prompt "write email boss tomorrow leave personal work short" produces the meaning and concise style of "Write a short email to my manager requesting leave tomorrow for personal reasons."
- [x] The rough prompt "compare these two laptops for programming" may gain processor performance, RAM, storage, and development-tool compatibility as comparison criteria, without invented laptop specifications or an assumed budget.
- [x] Ambiguous input still produces an enhanced prompt without asking questions or guessing user-specific facts; unresolved specificity is preserved.
- [x] Evaluate representative writing, coding, research, and everyday prompts through the existing application workflow using the selected OpenRouter model. Record semantic quality, observed latency, and usage cost considerations; do not assert exact live output strings.
- [x] Live checks use an explicitly configured account and non-sensitive examples. Controlled-response workflow tests remain separate evidence and are not presented as proof of live model quality.
- [x] Record the chosen model, representative results, known quality limitations, and reasons for the selection. Keep the enhancement policy independent of provider-specific request handling.

## Comments

Depends only on ticket 01, which supplies the runnable cloud enhancement path. Clipboard, floating-window polish, and cloud-error recovery do not gate successful-response quality evaluation. No additional production provider is required.

## Answer

Implemented on 2026-11-08 in `2b64303`. The provider-neutral policy explicitly
rewrites rather than executes requests, preserves constraints and unresolved
references, distinguishes background facts from requirements, and permits
essential task criteria without mandatory headings or output-length targets.

Selected `openai/gpt-4.1` after live Mini comparisons exposed intent changes
despite policy improvements. The final ten non-sensitive examples used the saved
configured account through the actual editor and Enhance action. All ten
observations met the semantic rubric; latency was 1.452-2.444 seconds (median
1.6825 seconds), with $0.00854 total API-reported usage charges. These observations
are not exact-string expectations or guarantees for unseen requests.

The [evaluation report](../enhancement-evaluation.md) records all examples,
earlier failures, cost considerations, and known limitations. A manual opt-in
runner permits repeating the evaluation independently of controlled tests.
[ADR 0002](../../../docs/adr/0002-intent-preserving-enhancement-model.md) records
the model choice and provider boundary.

Validation: warnings-as-errors compilation; nine targeted HTTP checks; full
suite 33/33 passed; manual runner's refusal without `-Live` verified. The
model-selection check failed before the model switch and passed afterward.
The replacement-provider check now tests forwarding the complete policy rather
than depending on its wording. Controlled responses do not establish live quality.

The [implementation review](../concise-enhancement-review.md) records Standards
and Spec review against pre-implementation baseline `4b1186b`.
