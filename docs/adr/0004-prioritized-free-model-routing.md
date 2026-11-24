# Route enhancement through prioritized free models

Date: 2026-11-24
Status: Accepted; TDD seams confirmed by the user on 2026-11-24

Live-request correction, 2026-11-24: OpenRouter rejected the original six-item
`models` array with HTTP 400 and a three-item validation limit. Changing only
the candidate list to the first three returned HTTP 200 and a valid enhanced
prompt using the same saved credential. This supersedes the original decision
to send all six candidates in one HTTP request. Controlled tests did not model
the service's array-length validation and therefore missed this incompatibility.

The user requests free OpenRouter routing with five backend priorities instead
of a single selected model. This supersedes ADR 0002's paid GPT-4.1 selection;
the provider-neutral enhancement policy and existing provider boundary remain.

OpenRouter's `openrouter/free` selects randomly, so it cannot enforce a preferred
order. Use the chat-completions API's `models` array to try these candidates in
order, with `openrouter/free` as the final automatic fallback:

1. `google/gemma-4-31b-it:free`
2. `inclusionai/ling-3.2-flash:free`
3. `baidu/ernie-5.5-flash-preview:free`
4. `nvidia/nemotron-3.5-lightning:free`
5. `google/gemma-4-26b-a4b-it:free`
6. `openrouter/free`

The first preference is an instruction-tuned general model, followed by Flash
alternatives from different organizations, a lightweight NVIDIA model, and a
smaller Gemma option. This is an initial engineering preference for general
rewriting and availability diversity, not a measured quality or latency ranking.
All five explicit IDs were listed with zero prompt and completion prices and
text output in the public model catalog on this date.

The backend owns the entire order; callers no longer select a model. Every
entry is a free variant or the free router. Send the first three preferences in
one authenticated request. If it fails with a routing, capacity, or server error
(HTTP 404, 408, 422, 429, or 5xx), send the remaining three candidates in a second
request. OpenRouter handles failover within each group. Stop for authentication,
credits, other request errors, network failures, invalid completions, or
cancellation. At most two HTTP requests share the client's timeout budget
(60 seconds in production). Keep non-streaming responses, temperature 0.2,
the 2048-token output limit, and safe response/error handling. Each request can
consume the account's free-tier quota.

Availability, free-tier limits, and model quality vary. ADR 0002's ten-example
GPT-4.1 evaluation remains historical evidence and does not establish the quality
of these replacements. Controlled HTTP and editor workflow tests verify the
application's request contract and behavior, not OpenRouter's internal fallback
engine or live semantic quality.

Sources: [model fallbacks](https://openrouter.ai/docs/guides/routing/model-fallbacks),
[free router](https://openrouter.ai/docs/guides/routing/routers/free-router),
[chat completions](https://openrouter.ai/docs/api/api-reference/chat/create-a-chat-completion),
[current catalog](https://openrouter.ai/api/v1/models).
