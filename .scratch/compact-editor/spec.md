# Compact floating editor

## Brief

Adapt reference/image.jpeg to Prompt Enhancer, including a similarly compact
window. The prompt editor remains a native Windows 11 WinUI 3 application.

## Design

Open at 420 by 530 logical pixels, clamped to the monitor work area and scaled
by Windows DPI. Use a dark indigo Mica-backed outer frame, small circular toolbar
buttons, an editable result area above the rough-prompt composer, and a light
Enhance button. All content is left aligned except the quiet toolbar title.

Palette: frame #303047, result #20212D, composer #17181E, border #494960,
text #F3F2F7, secondary text #B4B1C4. Segoe UI Variable Text provides native
Windows text rendering with 14 px prompts, 13 px labels, and 12 px controls.

## Review against the reference

The narrow, tall floating utility and bottom composer are the defining features.
The email content and decorative cursor in the image do not belong to this
product. Keep actual Enhance, Paste, Copy, Settings, Clear, Hide, and Quit actions.
Use a sparkle only for the enhanced prompt marker; keep the surrounding surface
quiet and preserve the reference's visual hierarchy without unrelated features.

## Acceptance

- The editor opens at 420 by 530 logical pixels.
- The editable enhanced prompt sits above the rough-prompt composer.
- The toolbar title supports native window dragging; text selection stays local.
- Close collapses to the launcher and retains the current prompt session.
- The session menu offers Hide and Quit, including during enhancement.
- Explicit clipboard actions, Settings, retry, and cancellation keep working.
- Keyboard navigation and compact scrolling remain usable.
- Real WinUI screenshots establish the rendered appearance.
