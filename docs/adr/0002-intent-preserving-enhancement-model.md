# Select GPT-4.1 for intent-preserving enhancement

Date: 2026-11-08
Status: Accepted

Ticket 06 replaces ADR 0001's provisional model with `openai/gpt-4.1`.
Temperature 0.2, non-streaming chat completions, the 2048 output-token ceiling,
and the 60-second timeout stay in the provider integration. The ceiling is not
an instruction to generate a particular length.

The baseline GPT-4.1 Mini run executed an email request instead of rewriting it
and guessed an ambiguous object's type. A clearer provider-neutral policy fixed
those examples, but Mini still changed an undecided vendor into a vendor
prohibition and changed a quoted phrase. With the same final policy, GPT-4.1
preserved intent and constraints across ten representative examples, including
those cases. Observed latency was 1.452-2.444 seconds, median 1.6825 seconds;
API-reported usage cost totaled $0.00854 for ten calls.

Choose the observed quality improvement over Mini's lower cost. This is a small
task-specific evaluation of two candidates, not evidence of market-wide superiority.
At evaluation time, catalog rates were $2/$8 per million input/output tokens for
GPT-4.1 and $0.40/$1.60 for Mini. Costs and latency can change with routing,
prompt length, caching, and provider/account configuration.

The desktop continues to receive `IEnhancementProvider`; only its adapter selects
the OpenRouter model. `EnhancementPolicy` owns grammar, concise structure,
essential additions, preservation of constraints and ambiguity, and the rule to
rewrite rather than answer. No postprocessor guesses at semantic correctness.
Users must review results; live output strings and quality are not guaranteed.

Evidence: [evaluation](../../.scratch/prompt-enhancer/enhancement-evaluation.md).
Sources checked on 2026-11-08: [GPT-4.1](https://openrouter.ai/openai/gpt-4.1),
[GPT-4.1 Mini](https://openrouter.ai/openai/gpt-4.1-mini).
