# Free OpenRouter routing

Replace the single paid model selection with backend-owned routing. Submit five
explicit free text models in priority order, followed by `openrouter/free` so
OpenRouter can select another available free model if the preferred models fail.
No model selector is added to the editor.

Initial order, checked against OpenRouter's public catalog on 2026-11-24:

1. `google/gemma-4-31b-it:free`
2. `inclusionai/ling-3.2-flash:free`
3. `baidu/ernie-5.5-flash-preview:free`
4. `nvidia/nemotron-3.5-lightning:free`
5. `google/gemma-4-26b-a4b-it:free`
6. `openrouter/free` (automatic free-model fallback)

Use OpenRouter's ordered `models` request parameter in two groups of three: the
first three preferences, then the remaining two preferences plus `openrouter/free`.
This corrects a live HTTP 400 caused by the service's three-item array limit.
Advance only on HTTP 404, 408, 422, 429, or 5xx; stop for authentication, credits,
other request errors, network failures, invalid completions, and cancellation.
The client's timeout budget covers the entire enhancement, including both groups.
Keep authentication, exact
rough prompt, provider-neutral enhancement policy, response validation, timeout,
and safe application errors. The provider owns the order; callers cannot select
a single paid model. Availability and semantic quality remain outside controlled
test evidence.

TDD boundaries confirmed by the user on 2026-11-24: provider public
`EnhanceAsync` at the HTTP boundary; existing editor-to-provider workflow.

References: [ordered fallbacks](https://openrouter.ai/docs/guides/routing/model-fallbacks),
[free router](https://openrouter.ai/docs/guides/routing/routers/free-router),
[model catalog](https://openrouter.ai/api/v1/models).
