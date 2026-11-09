# Compact reference-inspired prompt editor

Status: ready-for-human
Type: task
Execution: resolved

Implement the supplied reference image as a native WinUI floating prompt editor,
using approximately 420 by 530 logical pixels. Keep the existing enhancement,
Settings, explicit clipboard actions, session lifetime, and launcher behavior.
Place the enhanced prompt above a bottom rough-prompt composer, use a dark
indigo frame, and group secondary session commands in the toolbar menu.

## Validation

Compile with warnings as errors; exercise the real window, keyboard, clipboard,
Settings, recovery, session behavior, compact sizing, and rendered screenshots.

## Answer

Implemented the 420 by 530 logical-pixel dark floating editor, native caption
region, circular toolbar, session menu, result-first workspace, and bottom
rough-prompt composer. Updated the README and appearance decision.

Application/test-host builds pass with warnings as errors. Real WinUI checks
passed provider, Settings, clipboard/workflow, 17 recovery scenarios, keyboard,
reference proportions, and compact/expanded layout at 150% display scaling.
Reviewed fixture screenshots are in ../preview.png and ../preview-empty.png.
The full suite did not pass: native launcher input/focus prevented completing
session checks. See ../verification.md for the exact limitation and rerun scope.