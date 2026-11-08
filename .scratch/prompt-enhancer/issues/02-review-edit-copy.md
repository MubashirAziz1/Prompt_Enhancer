# 02: Review, edit, and copy the enhanced prompt

Type: task
Status: ready-for-agent
Execution: claimed
Blocked by: 01 (Enhance a rough prompt through OpenRouter)

**What to build:** Let the user paste a rough prompt, request enhancement, review and edit the enhanced prompt, then explicitly copy the reviewed result into another application.

**Execution:** Implementation explicitly authorized by the user's implement request on 2026-11-08.

- [ ] Explicit paste, including normal user-initiated keyboard paste, inserts clipboard text into the rough-prompt editor; typing and editing remain available.
- [ ] The user can review and edit the enhanced prompt before copying it.
- [ ] Copy writes the current reviewed or edited enhanced prompt, rather than an earlier unedited version.
- [ ] Merely opening the widget, typing, or enhancing does not read the clipboard automatically or overwrite its contents.
- [ ] No clipboard monitoring, automatic copy, or interaction with the user's destination application is introduced.
- [ ] Verify paste -> Enhance -> edit -> Copy through the application workflow with controlled AI responses; confirm clipboard contents remain unchanged until Copy and match the edited result afterward.

## Comments

Depends only on the working enhancement flow. It does not require the floating-widget behavior or later model-quality evaluation.
