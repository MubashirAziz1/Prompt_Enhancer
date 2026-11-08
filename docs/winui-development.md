# WinUI migration editor (ticket 01)

The WinUI editor is an independent migration target. The normal
`scripts/run.ps1` command continues to launch Windows Forms. Floating startup,
session controls, folder publication, and cutover belong to ticket 02.

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
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-winui.ps1
# Launch the migration editor directly.
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-winui.ps1
# Build and run real WinUI workflow checks.
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-winui.ps1
# Select one boundary while developing (Settings, Workflow, Recovery, Appearance).
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-winui.ps1 -Only Recovery
# Retained Windows Forms regression checks.
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check.ps1
```

Run UI checks on an unlocked interactive desktop and leave keyboard/mouse idle.
The migration editor uses the existing DPAPI CurrentUser blob at
`%LOCALAPPDATA%\PromptEnhancer\openrouter-key.dpapi`. Automated checks use only
unique isolated fixture paths. They must never read or replace the production key.
Controlled HTTP responses prove transport/workflow behavior, not live model
quality. No paid evaluation is necessary for this UI migration.

The test host opens the real `EditorWindow` and native `ContentDialog`, locates
controls by their public automation IDs, and invokes native automation peers.
Keyboard checks send Windows input only after verifying the intended test window
is foreground. HTTP doubles replace only the external transport; request
serialization, authentication, response parsing, error mapping, and DPAPI remain
real. The Framework credential probe is independently compiled with the retained
compiler and credential sources. The test process saves the previous OLE
clipboard data object, restores it, and verifies its original text afterward.
Rendered fixture-only screenshots are regenerated under the test output's
`snapshots` directory; no production prompts or credentials appear in them.

The editor links the existing enhancement policy, provider, and credential
sources. The provider remains GPT-4.1 with temperature 0.2, non-streaming requests,
a 2048 output-token ceiling, and a 60-second production timeout. The WinForms
version remains usable with a key saved by the migration editor.

Build outputs carry runtime dependencies for development launch. Publication
of the supported distributable folder and its launch smoke test remain ticket 02.
Bundled .NET and Windows App SDK runtimes must be serviced with future releases.

Sources verified during implementation:
[stable Windows App SDK channels](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-channels),
[Microsoft build tools components](https://learn.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-build-tools),
[unpackaged deployment](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/unpackage-winui-app).
