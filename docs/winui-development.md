# WinUI desktop development and folder delivery

The Windows 11 x64 WinUI application starts as a floating button and owns one
in-memory prompt session. The editor, launcher, and notification-area commands
share that lifetime. `scripts\run.ps1` now builds and launches this application;
the repository contains only the WinUI application. See the
[acceptance evidence](../.scratch/winui3-migration/verification.md) for the
cutover result and actual presentation conditions.

Only one instance runs per Windows session (a named mutex). Launching again signals
the running instance to bring back its floating button and exits. Startup failures
show a message box instead of closing silently; the notification icon is retried
if Explorer is not ready yet or restarts. The notification icon is the application's
own icon (`src/app.ico`, also the executable icon).

## Stable build setup

Target: Windows 11 x64, Windows SDK 26100, .NET 10 LTS, and Windows App SDK
2.6.5 (stable). `global.json` pins the .NET SDK feature band; package lock files
record resolved dependencies. No preview SDK or winapp CLI is needed.

Install the stable Microsoft Visual Studio Build Tools 2026 managed desktop
workload, Windows 11 SDK, and .NET 10 SDK:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup-winui.ps1
```

This installs Microsoft tooling on the machine. The installer may request
administrator access or a restart. Start a fresh PowerShell afterward.
Alternatively use Visual Studio 2026 with its WinUI application development
workload and the same SDK. Build scripts find 64-bit MSBuild through `vswhere`
and explicitly resolve the x64 .NET SDK, including in shells opened before setup.
The managed-only build leaves `VsInstallRoot` empty for the XAML target: its
C++ metadata directory probe otherwise fails on a managed-only Build Tools
installation. This application does not compile C++ or require VC metadata.

```powershell
# Compile / typecheck with warnings as errors; restore stable packages.
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
# Build and launch the application (normal launch; floating startup).
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run.ps1
# Build and run real WinUI workflow checks.
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check.ps1
# Select one boundary (Settings, Workflow, Recovery, Appearance, Session, OpenRouter).
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check.ps1 -Only Recovery
# Publish the distributable self-contained folder.
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-winui.ps1
# Full acceptance in a published test host plus production startup/runtime smoke.
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-published-winui.ps1
# Provider contract checks in the WinUI test host.
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check.ps1 -Only OpenRouter
```

Run UI checks on an unlocked interactive desktop and leave keyboard/mouse idle.
The migration editor uses the existing DPAPI CurrentUser blob at
`%LOCALAPPDATA%\PromptEnhancer\openrouter-key.dpapi`. Automated checks use only
unique isolated fixture paths. They must never read or replace the production key.
Controlled HTTP responses prove transport/workflow behavior, not live model
quality. No paid evaluation is necessary for this UI migration.

The test host opens the real `EditorWindow`, `LauncherWindow`, and native `ContentDialog`, locates
controls by their public automation IDs, and invokes native automation peers.
Keyboard checks send Windows input only after verifying the intended test window
is foreground. HTTP doubles replace only the external transport; request
serialization, authentication, response parsing, error mapping, and DPAPI remain
real. The Framework credential probe is independently compiled with the installed
compiler and credential sources. The test process saves the previous OLE
clipboard data object, restores it, and verifies its original text afterward.
Rendered fixture-only screenshots are regenerated under the test output's
`snapshots` directory; no production prompts or credentials appear in them.

The editor uses the enhancement policy, provider, and credential sources in `src/`.
The provider remains GPT-4.1 with temperature 0.2, non-streaming requests,
a 2048 output-token ceiling, and a 60-second production timeout. Existing saved
keys remain compatible with the protected-key format used before migration.

## Folder publication

`publish-winui.ps1` publishes Release, win-x64, unpackaged output to
`build\publish\win-x64\`. Copy the **entire folder** and start
`PromptEnhancer.WinUI.exe`. It includes .NET and Windows App SDK runtime files;
the app is not a single-file executable. No MSIX registration, installer,
Store publication, updater, Node, or Python is involved. Building requires the
toolchain above; the published executable does not require the .NET SDK.
`Directory.Build.targets` explicitly includes the generated project PRI and
compiled XBF markup in publication; ordinary unpackaged build output alone does
not establish that the publish item list carries these resources.

The larger folder is the accepted cost of carrying both runtimes. The maintainer
must update the pinned SDK/package versions, rebuild, repeat acceptance, and
redistribute the complete folder to service bundled runtime fixes. There is no
automatic update mechanism. The supported execution target is Windows 11 x64;
the Microsoft Visual C++ v14 x64 Redistributable remains a native prerequisite.
The verified machine has version 14.44.35211.00 installed. Use Microsoft's
[supported x64 redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist)
on another machine; neither the VC runtime nor a clean Windows installation was
removed/reimaged during acceptance. See Microsoft's
[unpackaged runtime requirements](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/deployment-architecture#additional-requirements)
and the acceptance evidence. No additional runtime setup was needed on this target.

`check-published-winui.ps1` runs the full real-window suite from a separately
published test host using isolated credentials. It verifies that host contains
the identical production application assembly and XAML resources, then launches the actual production
executable, checks floating startup and full Quit, and verifies loaded .NET/WinUI
runtime module paths point into the published folder, and that a second launch hands
off to the first and exits. Quit any running Prompt Enhancer first, because the
single-instance guard would otherwise swallow the script's launch. If the identical-asset
comparison reports a mismatch after test projects were renamed, delete
`build\winui-tests`, `build\publish-tests`, and `tests\obj` (stale compiled
markup) and rerun. It never opens the production
editor or reads its saved credential. The workflow uses controlled responses;
these checks do not establish live model quality.

Session checks exercise actual dragging, text selection, stacking over an active
companion process, native close/minimize, retained edits, Clear, Hide, real tray
menu/double-click restoration (including overflow), hidden completion, cancellation,
late completion, and fresh-process Quit/relaunch. The out-of-process shell probe
has a per-monitor DPI manifest. The test host retains a hidden host window only
to observe the application's windows after Quit; separate child-process checks
verify complete application exit. Test screenshots contain fixtures only.

## Source layout

`src/` contains the sole WinUI application, including the provider and credential core.
`tests/` contains its WinUI workflow host and provider contract checks.
`tests/fixtures/` contains independently compiled credential and desktop automation probes;
the desktop probe uses Framework controls only as a test companion. It is not another
Prompt Enhancer application. The old Windows Forms fallback and duplicate source roots
were removed at the user's request on 2026-11-09.

Sources verified during implementation:
[stable Windows App SDK channels](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-channels),
[Microsoft build tools components](https://learn.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-build-tools),
[unpackaged deployment](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/unpackage-winui-app).
