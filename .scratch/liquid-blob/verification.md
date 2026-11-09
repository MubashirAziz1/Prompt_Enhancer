# Liquid floating button verification

2026-11-10, Windows 11 desktop at 150% scaling.

Application and real WinUI test host compile with warnings treated as errors.
The focused `scripts/check.ps1 -Only Blob` run passed:

- Transparent rendered corners and an opaque sphere center.
- A native window region with clear corners and a clickable center.
- Actual held-mouse press compression and return after release.
- No rendered frames once the interaction settles.
- A visible keyboard focus ring and native Space activation.
- Actual native dragging without activation, followed by click activation.
- Native right-click menu opening and dispatch of both Hide and Quit.

Reviewed screenshots: [rest](preview.png), [pressed](preview-pressed.png),
and [keyboard focus](preview-keyboard.png). The surface is generated at runtime,
with no external textures, rendering packages, or cloud requests.

Initial runs exposed an unavailable unpackaged-app accessibility event and
the borderless window's client inset. The implementation uses UISettings color
notifications and computes the window silhouette in the rendered client coordinates.
Native pointer delivery was intermittent in earlier attempts, consistent with the
desktop input limitation recorded during the compact editor work. The final
focused run received and passed actual native input.
Press/hover deformation now uses a compositor transform; lighting bitmaps are
cached until pointer or accessibility colors change. The native input fixture
allows capture to attach before dragging and waits for actual held-press deformation.
The spring uses an exact damped solution based on elapsed time, so delayed frames
cannot extend its settling time. After removing diagnostic tracing, the final
build and all three focused check groups above passed again, including the idle
quiet-period check and both native context-menu actions.

The broader `-Only Session` run also passed isolated credential/enhancement setup,
Clear, all three Hide/reopen cycles, prompt retention, hidden pending completion,
Quit, light/dark launcher rendering, native drag/click, editor title movement,
text selection, and stacking over the active companion. It did not complete:
`SessionChecks.cs:66` timed out after invoking the existing editor Collapse
control. Later minimize/tray/fresh-process checks were not reached. The editor's
Collapse handler was not changed by this work. The cleanup restored the captured
OLE clipboard object and flushed it, but its read-back verification reported
`OutOfMemoryException`; original clipboard content was not printed or persisted.
Do not describe the full session suite as green.

Windows reduced-motion and contrast-theme behavior are implemented using system
settings; those global settings were not changed during validation. Only the
available 150% monitor was exercised. The currently running published application
was left running; the updated executable is in the normal build output.
