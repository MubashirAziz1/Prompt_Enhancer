# Ticket 06 live enhancement evaluation

Date: 2026-11-08. Selected model: `openai/gpt-4.1`.

## Method and evidence boundary

The implement request authorized ticket 06, including live evaluation with an
explicitly configured account. The existing production DPAPI credential was
available and used read-only. No key was printed, copied to test data, replaced,
or committed. Only the fixed non-sensitive examples below were submitted.

`scripts/evaluate.ps1 -Live` compiled the application and a separate manual
runner. It opened the real `MainForm`, entered each rough prompt, clicked the
real Enhance control, pumped Windows messages, and read the displayed result
after the action became available. The actual `OpenRouterProvider` sent the
actual `EnhancementPolicy` using the saved key, temperature 0.2, non-streaming
completions, 2048 output-token ceiling, and 60-second timeout.

A delegating HTTP handler observed only numeric usage fields without changing
requests or responses. Timings measure Enhance-to-ready at the desktop, including
network and parsing. Console observations informed the semantic assessment below;
no live output is asserted as an exact expected string. There is one final
observation per example, not a statistical benchmark or guarantee.

The separate controlled suite checks workflow, credential protection, serialization,
and safe errors. Its canned responses are not evidence of model quality. The
manual runner is outside `tests/` and never runs from `check.ps1`.

## Iteration and selection

- Baseline policy, GPT-4.1 Mini, eight calls: the leave request produced an email
  rather than a prompt; ambiguous input guessed that the object was text; the
  announcement case produced announcement copy. Other basic constraints survived.
- Refined policy, Mini, eight calls: adding an explicit rewrite-versus-answer rule,
  constraint preservation, task criteria, and short examples corrected execution
  and ambiguity. However, the wiki case converted an undecided vendor into a
  prohibition on specifying vendors.
- Final policy, Mini, ten calls: added the distinction between background facts
  and requirements and two new cases. Vendor prohibition persisted. The announcement
  case moved a comma inside the exact quoted phrase. The programming comparison
  omitted the optional supporting criteria in this run, although a prior run
  included them. Observed latency: 1.449-2.102 seconds.
- Final policy, GPT-4.1, the same ten cases: all ten observations preserved intent
  under the rubric below, including the vendor state and exact phrase. Retain
  GPT-4.1 for the initial release because those semantic errors matter more than
  the small absolute per-request savings in this personal-use workflow.

The rubric checks rewriting instead of executing, grammar and clarity, all stated
constraints, no invented specifics or questions, useful essential details, concise
simple requests, and appropriate organization of complex requests. Assessment is
semantic review, not an automated model grader. Only two models were compared.

## Final representative observations

Every row used the exact rough prompt shown. Results are paraphrased observations.

| Category and rough prompt | Semantic observation | Latency ms | Input/output tokens | API cost USD |
| --- | --- | ---: | ---: | ---: |
| Writing: `write email boss tomorrow leave personal work short` | Short instruction to request leave tomorrow for personal reasons; no email body. | 2444 | 332/15 | 0.000784 |
| Comparison: `compare these two laptops for programming` | Added processor performance, RAM, storage, and development-tool compatibility; no invented specifications or budget. | 1612 | 330/23 | 0.000844 |
| Coding: `python function remove duplicate list keep order no external library explain briefly` | Preserved Python, original order, no external libraries, and brief explanation; no code execution. | 1819 | 336/30 | 0.000912 |
| Research: `research remote work productivity pros cons use reliable sources separate evidence opinion` | Preserved pros/cons, reliable sources, and evidence/opinion distinction; no fabricated findings or sources. | 1724 | 336/21 | 0.000840 |
| Everyday: `plan easy dinners next week vegetarian cheap 20 minutes no mushrooms` | Preserved next week, vegetarian, inexpensive, 20-minute preparation, and mushroom exclusion. | 1599 | 336/28 | 0.000896 |
| Ambiguous: `make it better for them` | Brief instruction; object and audience remained unresolved; no questions. | 1687 | 329/6 | 0.000706 |
| Complex: `plan migrate team wiki to new platform compare options migration steps risks rollback checklist keep links working no vendor chosen` | Organized comparison, steps, risks, rollback, and working links in a concise paragraph; vendor remained undecided. | 1698 | 344/48 | 0.001072 |
| Constraints: `rewrite announcement friendly keep exact text "Launch: TBD" under 40 words no emojis` | Kept exact phrase, friendly tone, under-40-word instruction, and emoji exclusion; no announcement body. | 1501 | 341/31 | 0.000930 |
| Unresolved context: `help fix this issue for them keep short` | Brief instruction to help resolve the issue; no guessed issue type or audience facts. | 1678 | 332/10 | 0.000744 |
| Restricted comparison: `compare these two laptops only RAM and storage no recommendations` | Limited comparison to RAM/storage and retained no recommendations; no extra criteria. | 1452 | 334/18 | 0.000812 |

## Latency, usage, and limitations

Final run: 10 successful calls; 3350 input tokens, 230 output tokens; latency
1.452-2.444 seconds, median 1.6825 seconds. API-reported cost: $0.00854 total,
$0.000854 mean per request, $0.000706-$0.001072 range. All 36 development
evaluation calls across the four runs reported $0.012424 in aggregate usage cost.
These are completion usage charges, not a reconciliation of account billing,
top-up fees, or taxes.

Catalog rates checked on the evaluation date: GPT-4.1 $2 input/$8 output per
million tokens; Mini $0.40/$1.60. At the final sample's average token counts,
1000 similar GPT-4.1 enhancements would cost about $0.854 in completion usage.
Longer prompts, longer outputs, different routes, caching, or rate changes alter
this estimate. Missing usage telemetry on future runs means unknown, not free.
Sources: [GPT-4.1](https://openrouter.ai/openai/gpt-4.1),
[GPT-4.1 Mini](https://openrouter.ai/openai/gpt-4.1-mini).

The final sample is small and not independent of policy development: the email,
ambiguous-reference, vendor-state, and laptop criteria examples informed the
instructions. Unseen requests and repeated runs can still change intent, omit
constraints, over-expand, or execute a request. No semantic postprocessor or
guarantee was added. Complexity does not require bullets: the observed wiki
prompt remained readable as a paragraph. Long or adversarial prompts and
non-English translation were not evaluated. Review/edit remains necessary.
The evaluation rewrites research requests; it does not perform research or
validate downstream factual answers. Provider routing and availability can vary.

Reproduce manually with `powershell -NoProfile -ExecutionPolicy Bypass -File
.\scripts\evaluate.ps1 -Live` after configuring your own account in Settings.
Without `-Live`, the script refuses to build or send requests. It uses no
clipboard actions and writes no prompt files; it prints only fixed examples,
validated results, safe UI feedback, and numeric usage metadata.
