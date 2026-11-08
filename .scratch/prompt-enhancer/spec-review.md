# Floating Prompt Enhancer

Publication: Review draft; awaiting the testing-boundary check before replacing the published spec and applying ready-for-agent.

Execution: The user confirmed the product design but explicitly put implementation on hold. Preparing or publishing this document does not authorize application development or deployment.

## Problem Statement

The user knows what they want but struggles to express that intent as a clear, grammatically correct, structured English prompt. Their rough prompt can contain broken English and missing supporting details. Improving it should not interrupt work in another editor, introduce unnecessary requirements, or require answering clarification questions.

## Solution

Provide a Windows desktop widget that stays above other windows and can be moved with the mouse. A small floating button expands into an editor where the user pastes or types a rough prompt, requests enhancement, reviews or edits the enhanced prompt, and explicitly copies it.

The enhanced prompt preserves intent, corrects grammar, improves wording and organization, and includes essential additions when needed. It remains concise, with simple paragraphs for simple requests and useful structure for complex ones. The widget never asks clarification questions or invents facts to resolve missing context.

Use OpenRouter initially, with the user's API key protected using Windows credential protection. Keep provider integrations separate from enhancement behavior so future providers can be added without rewriting the widget. Build and validate the desktop application for personal use.

## User Stories

1. As a Windows user, I want a floating prompt-enhancement button, so that I can improve prompts while working in another application.
2. As a user, I want to drag the floating button with my mouse, so that it does not obstruct my work.
3. As a user, I want the button to expand into an editor when clicked, so that I can enter a rough prompt.
4. As a user, I want to move the expanded editor, so that I can position it conveniently.
5. As a user, I want the widget to remain above other windows, so that it stays available while I use my usual editor.
6. As a user, I want to paste broken English into the editor, so that I can express an idea without first correcting it myself.
7. As a user, I want to type and revise a rough prompt, so that I can prepare it directly in the widget.
8. As a user, I want an explicit Enhance action, so that I control when my text is submitted for cloud processing.
9. As a user, I want grammatically correct English output, so that my request is easier to understand.
10. As a user, I want better wording and organization, so that the enhanced prompt clearly communicates my intent.
11. As a user, I want my intended goal and stated constraints preserved, so that enhancement does not change my request.
12. As a user, I want essential additions supplied automatically, so that the prompt includes supporting details needed for its goal.
13. As a user, I want unnecessary details excluded, so that the enhanced prompt stays focused.
14. As a user, I want no invented facts about my circumstances, so that missing information does not become false information.
15. As a user, I want enhancement without clarification questions, so that I can obtain a result in one interaction.
16. As a user, I want a concise paragraph for a simple request, so that enhancement does not make it needlessly long.
17. As a user, I want bullets or other useful structure for a complex request, so that its requirements are easier to follow.
18. As a user, I want general-purpose output for writing, coding, research, and everyday tasks, so that I can use the widget across different AI assistants.
19. As a user, I want to review the enhanced prompt before copying it, so that I can check whether it expresses my intent.
20. As a user, I want to edit the enhanced prompt, so that I can make final adjustments.
21. As a user, I want Copy to copy the reviewed result, so that I can paste it into my chosen application.
22. As a user, I want clipboard reads and writes to happen only through explicit paste and Copy actions, so that clipboard changes remain under my control.
23. As a user, I want minimizing or closing the expanded editor to return to the button, so that I can reduce its footprint.
24. As a user, I want Hide to remove the widget from the screen, so that I can temporarily work without it.
25. As a user, I want the current input and result preserved while the widget is minimized or hidden, so that I can resume the current task.
26. As a user, I want Clear to remove the current input and result, so that I can start a fresh prompt.
27. As a user, I want Quit to exit the application and discard session prompt contents, so that those contents do not become saved history.
28. As a user, I want no persistent prompt history, so that past prompts are not stored locally by the widget.
29. As a user, I want to enter my OpenRouter API key in Settings, so that the widget can use my own cloud account.
30. As a user, I want my API key stored using Windows credential protection, so that it is not kept in an ordinary plaintext settings file.
31. As a user, I want cloud failures to preserve my rough prompt and allow retry, so that a failed request does not lose my work.
32. As a user, I want failures distinguished from enhanced prompts, so that I do not accidentally copy an error as my result.
33. As a maintainer, I want provider-specific behavior isolated from the widget and enhancement policy, so that another provider can be added with limited changes.
34. As the initial user, I want the desktop workflow validated before public deployment, so that distribution work does not delay a usable application.

## Implementation Decisions

- First target: Windows desktop, personal use. No application code, dependency manifest, framework choice, existing tests, or ADRs were found in the repository; framework selection remains an implementation-planning task.
- Use the glossary terms rough prompt, enhanced prompt, intent, and essential addition consistently.
- Separate the desktop interaction layer, enhancement orchestration and policy, provider integrations, and Windows-protected credential access. These are conceptual responsibilities, not a prescribed number of files or services.
- The desktop interaction layer owns the floating button, expanded editor, settings, editing, explicit clipboard actions, and session draft lifecycle.
- Enhancement consumes the rough prompt and produces the enhanced prompt or a distinct failure. Its policy preserves intent, permits essential additions, prohibits unnecessary requirements and fabricated context, and never introduces a clarification-question flow.
- Choose paragraph or bullet structure according to the request; do not force role/context/constraints headings onto every output. No fixed word limit was agreed.
- Integrate OpenRouter first. Cloud processing, internet access, and potential usage costs are accepted. Select the initial model during technical planning based on representative prompt quality, latency, and cost; no model is selected in this spec.
- Keep provider-specific authentication, request construction, response parsing, and failures behind a replaceable integration boundary. Adding a provider should not require changes to desktop behavior or the enhancement policy. A provider marketplace or generalized plugin system is not required.
- Let the user configure an API key in Settings and store it using Windows credential protection. The exact Windows credential mechanism remains a technical choice.
- Keep the API credential independent of session prompt contents. Clearing or quitting discards prompts, not the configured credential.
- Start with a floating button. Clicking expands the editor. Both forms are mouse-draggable and remain above other windows. Minimize or close of the editor collapses to the button; Hide removes the widget from view; Quit exits.
- Keep the current input and result in session memory while minimized or hidden. Clear removes them; Quit discards them. Do not add persisted prompt history.
- Read clipboard text only on an explicit paste action; write the reviewed or edited enhanced prompt only on Copy.
- Preserve the rough prompt on provider failure and support retry. Do not expose failures as successful enhanced prompts.

## Testing Decisions

- Proposed primary test seam: the complete desktop application workflow, observed through user-visible behavior. Exercise the widget and its underlying enhancement, provider, credential, clipboard, and session responsibilities together rather than testing internal method calls or private data structures.
- Use controlled AI responses at the cloud boundary for repeatable success, malformed-result, authentication-failure, and connectivity-failure scenarios. The exact automation framework and test-double mechanism remain implementation choices.
- No existing application or test suite was found, so there is no test prior art or existing seam to reuse. Propose one application-level seam instead of multiple module-specific suites. Confirm this boundary with the user before publishing the final spec.
- Cover paste/type, Enhance, review/edit, and Copy, including copying the edited result. Verify that clipboard contents are not changed by enhancement alone.
- Cover drag and topmost behavior over another editor for the button and expanded editor, collapse on close/minimize, Hide, retained session contents, Clear, and discarded contents after Quit and relaunch.
- Cover API-key setup and retrieval through Settings on Windows, including absence from ordinary settings files. Never use real personal credentials in deterministic tests.
- Cover failure behavior through retained input, a distinct failure state, and retry. Avoid brittle assertions about exact error wording.
- Check actual enhancement quality separately with representative prompts against the selected OpenRouter model. Controlled responses prove workflow behavior, not live model quality. Live checks are explicit, use non-sensitive examples, and consume the configured provider's usage allowance.
- Quality acceptance: improved grammar and organization, preserved intent and stated constraints, useful essential additions, no unnecessary requirements or fabricated context, no clarification questions, and appropriate concision. Use semantic review rather than exact-string equality for live outputs.
- Agreed simple example: rough prompt "write email boss tomorrow leave personal work short" should have the meaning and concise style of "Write a short email to my manager requesting leave tomorrow for personal reasons."
- Agreed addition example: rough prompt "compare these two laptops for programming" may include processor performance, RAM, storage, and development-tool compatibility as comparison criteria. It must not invent laptop specifications or assume a budget.
- Include an ambiguous rough prompt and verify that it still yields an enhanced prompt without clarification questions or invented facts. Preserve unresolved specificity rather than guessing.
- Future provider integration acceptance: a replacement integration can produce enhanced prompts through the same desktop workflow without changing that workflow or its enhancement policy.

## Out of Scope

- Non-Windows platforms in the first version.
- Non-English input, multilingual support, and translation.
- Clarification conversations.
- Persistent prompt history.
- Model-specific enhancement modes.
- User accounts, subscriptions, and billing infrastructure operated by this project.
- Public distribution, website development, installer creation, and deployment.
- Choosing a framework, initial OpenRouter model, specific provider request schema, or installer technology in this product spec.

## Further Notes

- The user confirmed shared product understanding and explicitly instructed not to act on it. This skill invocation authorizes documentation synthesis and local spec publication only; application implementation and deployment remain on hold until instructed.
- Publish the final spec to the project's local Markdown issue tracker with Status: ready-for-agent after the testing-boundary check. That triage label does not revoke the execution hold.
- Reuse the existing feature's canonical spec location when publishing; do not create a second feature or remote issue. No task decomposition was requested.
- No ADR is added: provider extensibility is agreed, while framework, credential mechanism, model, and packaging choices have not been made.

