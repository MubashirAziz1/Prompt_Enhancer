# 04: Manage the current prompt session

Type: task
Status: ready-for-agent
Blocked by: 03 (Use a draggable floating widget)

**What to build:** Let the user temporarily reduce or hide the widget without losing the current rough prompt and enhanced prompt, clear the current task, or quit without retaining prompt history.

**Execution:** Implementation remains on hold until the user explicitly instructs it to begin. Ticket approval authorizes publication only.

- [ ] Minimizing or closing the expanded editor preserves the current rough prompt and enhanced prompt when the editor is reopened.
- [ ] Hide removes the floating UI from the screen while retaining session contents; an explicit user-accessible action restores the widget with those contents.
- [ ] Clear removes both the current rough prompt and enhanced prompt and allows a fresh enhancement.
- [ ] Quit exits the application and discards session prompt contents. Relaunch starts without the previous input or result.
- [ ] The widget does not persist prompt history in settings, local history files, or application logs. This does not imply that the cloud provider has no retention policy.
- [ ] Clear and Quit do not remove the separately protected OpenRouter credential; it remains usable in a new session.
- [ ] Verify an enhancement followed by collapse/reopen, hide/restore, Clear, and Quit/relaunch, checking visible session contents and credential availability through the application.

## Comments

Depends on ticket 03 because it completes the floating widget's lifecycle. Clipboard and model-quality work do not gate these behaviors.
