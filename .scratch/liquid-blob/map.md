# Liquid blob map

## Notes

Native WinUI 3 floating button for the existing prompt enhancement utility.
The user supplied a glossy liquid sphere and requested a blue interactive version.

## Decisions so far

See [design spec](spec.md). Procedural surface lighting rendered to a native
bitmap; a matching Win32 window region removes the rectangular background.
Keep the existing Button automation peer and desktop interactions.

Implemented in [ticket 01](issues/01-liquid-floating-button.md). The glossy
surface uses cached procedural lighting and compositor deformation; a matching
native region follows the actual client coordinates. Reviewed
[preview](preview.png), [press](preview-pressed.png), and
[keyboard focus](preview-keyboard.png). Build and focused real-window/native
mouse, keyboard, and menu checks passed; see [verification](verification.md).

## Fog

The broader session suite timed out on the editor's Collapse control and its
clipboard read-back verification also failed. Later session checks were not
reached. Only the available 150% monitor was exercised; global animation and
contrast settings were not changed. The running published app was left intact;
the updated app is in the normal build output.
