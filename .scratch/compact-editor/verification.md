# Compact editor verification

The application and real WinUI test host compile with warnings treated as errors.
The 2026-11-09 full check passed provider contracts, isolated protected credential
compatibility, Settings, explicit Paste and Ctrl+V, busy state, edited Copy,
clipboard retry, Clear, all 17 recovery scenarios, provider replacement, native
Tab/selection/typing, Settings keyboard entry, and the redesigned appearance.

Appearance was verified on the actual 150% Windows display: the editor opens at
420 by 530 logical pixels; the result sits above the bottom composer; native
Caption drag regions are registered; 360 by 450 windows scroll while keeping
readable fields; 540 by 650 windows expand the result without page scrolling.
Fixture-only screenshots were visually reviewed and copied to preview.png and
preview-empty.png in this directory. The Settings screenshot was also generated.

The complete check did not pass: the native launcher drag did not receive mouse
movement. A separate Session run was stopped by the existing input guard because
Windows did not grant foreground focus to the test companion. The launcher code
was not changed. No input was sent to the wrong foreground window. Remaining
session drag, close/menu/collapse/restore, and Quit checks require an idle unlocked
desktop; a rerun is pending the user's availability.

No live cloud request or production credential was used by these checks. The
original clipboard was restored after both runs.
