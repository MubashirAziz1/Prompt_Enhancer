# Compact editor map

## Notes

The user supplied reference/image.jpeg and requested a similar design and dimensions.
The application is a native WinUI 3 prompt enhancement utility.

## Decisions so far

420 by 530 logical-pixel editor, dark indigo Mica-backed frame, circular toolbar
controls, editable result above a bottom composer. Segoe UI Variable Text at
14 px for prompts, 13 px for the result label, and 12 px for utility controls.
No email, attachment, or microphone controls: the reference is adapted to the
product's actual Enhance, Paste, Copy, Settings, and session commands.

Implementation: [ticket 01](issues/01-reference-design.md) resolved; see [verification](verification.md) and [preview](preview.png).

## Fog

Rendered compact/expanded layout and caption-region registration passed at 150% DPI. Native session mouse checks remain pending an idle desktop because launcher drag/focus was not received; the full suite is not green.
