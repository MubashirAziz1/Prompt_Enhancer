# Editor Hide returns to the floating button

Type: task
Status: ready-for-human
Execution: resolved
Blocked by: none

The editor's Hide action must return to the small floating button, retain rough
and enhanced prompts, and keep pending enhancement alive. Reopening must expose
the retained session. Quit must remain the action that destroys the windows.
Do not modify the frontend or overwrite the existing workspace edits.

## Comments

The lifecycle coordinator currently connects editor Hide to the same handler
used to hide both windows from the launcher and notification icon.

## Answer

Connected editor Hide to the existing Collapse handler in FloatingExperience.
The floating button and current prompt session are retained, including pending
enhancement. Frontend files were not changed. Focused real-window checks failed
before the fix and passed after it; see [verification](../verification.md).
Existing native Session checks were updated for the new editor Hide behavior
while keeping launcher/tray Hide coverage. The broader suite remains limited
by desktop focus and clipboard failures documented in verification.
