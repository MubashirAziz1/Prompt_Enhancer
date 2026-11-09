# Editor Hide map

## Notes

The user reports that Hide makes the application disappear and requests return
to the small floating button. Frontend files must remain untouched.

## Decisions so far

Scope the fix to the window lifecycle coordinator. Editor Hide should collapse
to the floating button and retain the current prompt session. The floating
button and notification icon retain their explicit Hide commands.

Implementation: [ticket 01](issues/01-return-to-floating-button.md) resolved.
Editor Hide now uses the existing Collapse lifecycle action. Focused real-window
checks pass; see [verification](verification.md). Frontend hashes are unchanged.

After the user approved restart, closed the older instance, rebuilt both normal
and published folders, and launched the updated normal executable. Application
code and XAML resources in both folders match the Hide-tested build byte for
byte. The updated process is responsive and is the only running app instance.

## Fog

The broader Session suite remains limited by native desktop focus and clipboard
failures. Focused Hide checks pass and deployment is complete; the updated app
is running.
