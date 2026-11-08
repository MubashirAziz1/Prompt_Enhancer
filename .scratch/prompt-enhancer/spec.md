# Floating Prompt Enhancer

State: Product decisions confirmed; ticket 01 implementation explicitly authorized and completed.

## Problem

The user has an idea but struggles to express it as a clear, grammatically correct, structured English prompt. Moving away from their current editor to improve wording adds friction.

## First-version scope

- Windows desktop application for the user's personal daily use.
- Accept typed or pasted English, including broken English; produce English only.
- General-purpose prompts for writing, coding, research, and everyday tasks.
- Correct grammar, improve wording, rearrange information, and add essential supporting details when needed.
- Preserve intent and avoid unnecessary verbosity or requirements.
- Never ask clarification questions. Do not resolve ambiguity by inventing facts about the user or their context.
- Use a short paragraph for simple requests and bullets when complexity warrants them; no mandatory role/context/constraints template.

## Widget interaction

- Start as a small floating button; clicking expands the prompt editor.
- Button and expanded editor are draggable with the mouse and remain above other windows until explicitly hidden or closed.
- Flow: paste or type, enhance, review or edit the result, copy.
- Minimizing the expanded editor returns to the floating button.
- Hiding removes the widget from the screen.
- No saved prompt history; prompt contents exist only in the current session.
- Read clipboard text only through an explicit paste action and replace clipboard contents only through Copy.
- Retain the current input and result while minimized or hidden. Clear removes them; Quit discards session content and exits.
- Closing the expanded editor returns to the floating button.

## AI integration requirements

- Use cloud AI; internet access and potential usage costs are accepted.
- OpenRouter is the first provider. The user will configure an API key in the app.
- Store the API key using Windows credential protection, outside prompt history and ordinary settings files.
- Keep enhancement behavior and widget interaction independent of provider-specific request and response handling.
- Isolate provider integrations so another provider can be added without rewriting the widget or enhancement policy.
- Select the initial OpenRouter model during technical planning using representative examples, output quality, latency, and cost.

## Agreed examples

### Simple rewrite

Input: "write email boss tomorrow leave personal work short"

Expected style: "Write a short email to my manager requesting leave tomorrow for personal reasons."

### Essential supporting additions

Input: "compare these two laptops for programming"

Accepted output: "Compare these two laptops for programming, considering processor performance, RAM, storage, and compatibility with development tools."

The output must not invent laptop specifications or assume a budget.

## Proposed acceptance checks

- Grammar and structure improve without changing the user's goal.
- Essential supporting additions match the accepted laptop example; no invented facts or unrelated requirements.
- Ambiguous input still produces an enhanced prompt without clarification questions.
- Simple requests remain short; complex requests gain only useful structure.
- Widget remains visible over another editor and can be dragged in both forms.
- Provider errors retain the draft and allow retry, without copying an error as the result.
- Adding another provider does not require changes to widget behavior or the enhancement policy.

## Outside the first version

- Non-English input or translation.
- Saved prompt history.
- Model-specific enhancement modes.
- Public distribution, accounts, subscriptions, and billing infrastructure.

Shared understanding confirmed by the user. Ticket 01 was explicitly authorized
through the implement request on 2026-11-08. Other implementation work remains
scoped to its individual tickets.
