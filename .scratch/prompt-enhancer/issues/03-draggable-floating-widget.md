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
- [x] Verify expansion, dragging, enhancement, and collapse on Windows with another editor open. This does not require clipboard features from ticket 02.

## Comments

Session retention, Hide, Clear, and Quit are delivered in ticket 04. This ticket changes how the existing complete enhancement workflow is accessed, rather than building a disconnected UI shell.

## Answer

Implementation committed on 2026-11-08 in `162dab8`. A topmost launcher opens
the existing topmost editor. Dragging the launcher uses the Windows drag
threshold and suppresses expansion after a drag. The editor keeps its native
title-bar dragging, so text fields and controls retain their normal behavior.
Closing or minimizing the editor returns to the launcher. Alt+F4 while the
launcher is focused exits; dedicated session controls remain ticket 04.

The user confirmed the DesktopWorkflow test seam and review baseline. All 16
controlled checks pass with compiler warnings treated as errors. Two widget
checks exercise the real application context: startup, expansion, enhancement,
close/minimize collapse, reopening at the original size, and application exit;
native mouse dragging of both forms; text selection and typing; and native
Alt+F4 collapse. An isolated companion editor in a separate process receives
typing while Windows hit-testing verifies the launcher and expanded editor
remain visibly above it. The companion process is closed after the test and
the mouse position is restored.

The lifecycle regression initially failed when reopening after minimization:
Windows could leave the editor minimized or restore it at the minimum size.
Collapse now waits until the native minimize transition finishes; expansion
uses the native restore dimensions and explicitly restores normal state.
The earlier Copy/Enhance completion-order fix is also covered by the existing
clipboard workflow checks. No live cloud calls, real API credentials, or paid
usage were used.

The prior interactive Computer Use helper failure was bypassed by the native
Windows acceptance tests at the confirmed seam. Windows widget acceptance is
complete; execution remains claimed pending the final independent review.
The [implementation review](../draggable-widget-review.md) will record that
review against the confirmed baseline `e002829`.
