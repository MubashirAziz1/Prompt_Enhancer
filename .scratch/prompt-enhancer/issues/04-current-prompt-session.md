# 04: Manage the current prompt session

Type: task
Status: ready-for-human
Execution: resolved
Blocked by: 03 (Use a draggable floating widget)

**What to build:** Let the user temporarily reduce or hide the widget without losing the current rough prompt and enhanced prompt, clear the current task, or quit without retaining prompt history.

**Execution:** Implementation explicitly authorized by the user's implement request on 2026-11-08.

- [x] Minimizing or closing the expanded editor preserves the current rough prompt and enhanced prompt when the editor is reopened.
- [x] Hide removes the floating UI from the screen while retaining session contents; an explicit user-accessible action restores the widget with those contents.
- [x] Clear removes both the current rough prompt and enhanced prompt and allows a fresh enhancement.
- [x] Quit exits the application and discards session prompt contents. Relaunch starts without the previous input or result.
- [x] The widget does not persist prompt history in settings, local history files, or application logs. This does not imply that the cloud provider has no retention policy.
- [x] Clear and Quit do not remove the separately protected OpenRouter credential; it remains usable in a new session.
- [x] Verify an enhancement followed by collapse/reopen, hide/restore, Clear, and Quit/relaunch, checking visible session contents and credential availability through the application.

## Comments

Depends on ticket 03 because it completes the floating widget's lifecycle. Clipboard and model-quality work do not gate these behaviors.

## Answer

Implemented on 2026-11-08 in `540184e`, with review-driven test cleanup in
`d4c81cf`. The editor has Clear, Hide, and Quit controls. The floating button and
Windows notification icon share Show widget, Hide, and Quit commands; double-clicking
the notification icon also restores the button. The tray overflow is supported.
Closing, minimizing, and hiding retain both rough and reviewed enhanced prompts
in the existing editor instance. Clear is available while enhancement is idle,
empties both prompts, disables Copy, and permits a fresh enhancement. Hide and
Quit remain available during enhancement. Completing a hidden request does not
show the widget; Quit cancels pending work, disposes the tray/menu and windows,
and ends application lifetime.

All 20 controlled checks pass with compiler warnings treated as errors.
Tests at the previously confirmed DesktopWorkflow boundary verify retention
through close/minimize/reopen, reviewed-result Hide/Restore through the real
Windows tray/overflow menu, Clear followed by saved-key-authenticated enhancement,
and Quit/relaunch in two separate application processes running the real message
loop. Relaunch starts empty and enhances with the saved protected credential.
Pending-request Hide/Restore and Quit cancellation are also checked.

Source inspection confirms the only application persistence path remains the
DPAPI credential store. Checks verify that the isolated credential directory
contains only the unchanged protected credential after Quit/relaunch. No prompt
history or application logging was introduced, and no live cloud calls or real
credentials were used. Local session behavior makes no claim about cloud retention.

The [implementation review](../current-prompt-session-review.md) records separate
Standards and Spec reviews against pre-implementation baseline `0beca54`.
Run and restore instructions are in [README](../../../README.md).
