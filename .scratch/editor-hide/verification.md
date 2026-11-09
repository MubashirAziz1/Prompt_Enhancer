# Editor Hide verification

## Reproduction

`powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check.ps1 -Only Hide`

Before the fix, the build passed with warnings treated as errors and the
focused check failed:

```text
FAIL System.Exception: Editor Hide must return to the floating button without quitting or destroying either window.
```

The check invokes the actual editor Hide button through its automation peer.
It uses real WinUI windows, isolated DPAPI credentials, and controlled HTTP.
It does not use mouse input or the clipboard.

## Cause and fix

`FloatingExperience` connected editor Hide to the launcher/tray Hide handler,
which hides both windows. Connect editor Hide to the existing Collapse handler
instead, preserving the session and showing the existing floating button.

## Verification scope

The focused check covers three Hide/reopen cycles, button size and position,
rough and enhanced prompt retention, pending enhancement through Hide,
completion without reopening the editor, and Quit destroying both windows.

The broader Session command could not reach its Hide checks on this desktop:
it failed in `NativeDesktop.Activate` with "Keyboard input must target only the
WinUI test window." Its clipboard-restoration check also reported an
`OutOfMemoryException`. Those checks are not reported as passing.

The normal post-fix build was blocked by the running application's executable
lock (process 27004). The existing application and its current session were left
running. A separate build avoids that lock:

`powershell -NoProfile -ExecutionPolicy Bypass -File .\.scratch\editor-hide\verify.ps1`

Post-fix isolated build: passed with warnings treated as errors. After allowing
the session flyout's closing animation to settle between repeated invocations,
the real-window checks passed:

```text
PASS Editor Hide/reopen cycle 1
PASS Editor Hide/reopen cycle 2
PASS Editor Hide/reopen cycle 3
PASS Editor Hide returns to the same floating button and preserves the session through repeated reopenings
PASS Hide during enhancement retains pending work and its result without reopening the editor
PASS Quit still destroys both windows and invokes shutdown
```

The updated production executable is
`build/hide-verification/x64/Release/net10.0-windows10.0.26100.0/win-x64/PromptEnhancer.WinUI.exe`.
Quit the existing instance before launching it, since the application's
single-instance guard otherwise restores the older running instance.

Frontend SHA256 hashes were unchanged for EditorWindow.xaml,
EditorWindow.xaml.cs, LauncherWindow.xaml, and LauncherWindow.xaml.cs.
`git diff --check` passed.

## Follow-up: Hide still appears unchanged

Inspection found the original process 27004 still running from 22:51:22. It
loads the normal output, whose application DLL was last written at 22:41:30.
The corrected isolated DLL was written at 23:07:55 and has a different SHA256.
The application's single-instance guard hands launches of the corrected binary
back to the older running process. The normal output was never replaced while
that process held its executable lock.

Restored the old instance's floating button through its existing Show event.
Re-ran the isolated `--only Hide` checks: all three Hide/reopen cycles, session
retention, completion while collapsed, and Quit passed again. Restart approval
was requested because the current prompt session exists only in memory. The
remaining deployment step is to close the older process, rebuild the normal
output, and launch that updated executable.

## Reverification of the reported updated app

On 2026-11-09 at 23:27 local time, reran `verify.ps1` against the current
source. The isolated build and all six focused checks passed again, including
three Hide/reopen cycles, retained session text, pending enhancement, and Quit.

Process 27004 was still the original instance started at 22:51:22 from the
normal `build/winui` folder. The normal and published application DLL hashes
still differed from the verified fixed DLL. Launching another executable does
not load that fix while this process owns the single-instance mutex.

Native desktop inspection could not start: both Node-based automation entry
points failed with `windows sandbox failed: helper_unknown_error: setup refresh
had errors`. No current prompt text was read or discarded. Restart confirmation
was requested because the running session exists only in memory. The verified
fixed executable is ready in the isolated folder above; deployment to the normal
and published folders remains pending restart.

## Approved restart and deployment completed

The user approved closing the older instance and launching the fixed app.
Closed process 27004 and ran `scripts/publish-winui.ps1` successfully with
warnings treated as errors, updating both the normal and published folders.

SHA256 comparisons confirmed that `PromptEnhancer.WinUI.dll`,
`PromptEnhancer.WinUI.pri`, `App.xbf`, `EditorWindow.xbf`, `LauncherWindow.xbf`,
and `SettingsDialog.xbf` in both folders exactly match the isolated build that
passed all six focused Hide checks. No further source change was needed.

Launched the normal executable as process 32212. It became interactive,
reported `Responding: true`, and was the only running Prompt Enhancer instance.
The old single-instance handoff and executable-lock deployment blockers are
resolved. The newly running app contains the verified Hide-to-floating-button
behavior.
