# 03: Use a draggable floating widget

Type: task
Status: ready-for-agent
Execution: claimed
Blocked by: 01 (Enhance a rough prompt through OpenRouter)

**What to build:** Make the existing enhancement workflow available from a small floating button that expands into a movable editor and stays available over another Windows application.

**Execution:** Implementation explicitly authorized by the user's implement request on 2026-11-08.

- [x] The application starts as a small floating button; clicking it expands the prompt editor.
- [x] Both the floating button and the expanded editor can be moved with the mouse.
- [x] Both forms remain above ordinary application windows while the user works in another editor, until explicitly hidden or quit.
- [x] The user can type, enhance, and view the result in the expanded editor using the existing enhancement flow.
- [x] Minimizing or closing the expanded editor returns to the floating button instead of exiting the application.
- [x] Dragging the widget does not prevent normal use of editor text fields and controls.
- [ ] Verify expansion, dragging, enhancement, and collapse on Windows with another editor open. This does not require clipboard features from ticket 02.

## Comments

Session retention, Hide, Clear, and Quit are delivered in ticket 04. This ticket changes how the existing complete enhancement workflow is accessed, rather than building a disconnected UI shell.

## Answer

Implementation committed on 2026-11-08 in `162dab8`. A topmost launcher opens
the existing topmost editor. Dragging the launcher uses the Windows drag
threshold and suppresses expansion after a drag. The editor keeps its native
title-bar dragging, so text fields and controls retain their normal behavior.
Closing or minimizing the editor returns to the launcher. Alt+F4 while the
launcher is focused exits; dedicated session controls remain ticket 04.

Warnings-as-errors compilation and all 14 existing controlled checks passed.
An intermittent completion-order issue was reproduced and fixed by restoring
Copy availability before enabling Enhance. These existing checks exercise the
editor workflow directly; they do not verify the new launcher interaction.

The implementation checkboxes above describe code delivered, not completed
interactive acceptance. Execution remains claimed until the final Windows
acceptance check is complete. New widget checks await the requested test-seam
confirmation required by the TDD skill. The Computer Use helper failed both
initialization attempts with `windows sandbox failed: helper_unknown_error:
setup refresh had errors`, so interactive expansion, dragging, overlay behavior
over another editor, and collapse have not been verified. No live cloud calls,
real API credentials, or paid usage were used.

The [implementation review](../draggable-widget-review.md) reports zero
Standards findings and one Spec finding: the incomplete Windows acceptance
check. No material implementation error was found by static inspection.
