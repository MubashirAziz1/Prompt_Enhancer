# 03: Use a draggable floating widget

Type: task
Status: ready-for-agent
Blocked by: 01 (Enhance a rough prompt through OpenRouter)

**What to build:** Make the existing enhancement workflow available from a small floating button that expands into a movable editor and stays available over another Windows application.

**Execution:** Implementation remains on hold until the user explicitly instructs it to begin. Ticket approval authorizes publication only.

- [ ] The application starts as a small floating button; clicking it expands the prompt editor.
- [ ] Both the floating button and the expanded editor can be moved with the mouse.
- [ ] Both forms remain above ordinary application windows while the user works in another editor, until explicitly hidden or quit.
- [ ] The user can type, enhance, and view the result in the expanded editor using the existing enhancement flow.
- [ ] Minimizing or closing the expanded editor returns to the floating button instead of exiting the application.
- [ ] Dragging the widget does not prevent normal use of editor text fields and controls.
- [ ] Verify expansion, dragging, enhancement, and collapse on Windows with another editor open. This does not require clipboard features from ticket 02.

## Comments

Session retention, Hide, Clear, and Quit are delivered in ticket 04. This ticket changes how the existing complete enhancement workflow is accessed, rather than building a disconnected UI shell.
