# WinUI 3 Migration Design Map

## Notes

- The user invoked grill-with-docs to explore shifting the existing Prompt Enhancer application to WinUI 3.
- The interview combined grilling and domain modeling. The user confirmed shared understanding and explicitly instructed "Not implement yet." Implementation and tooling installation are on hold until a subsequent instruction to begin.
- The current application uses Windows Forms and .NET Framework 4.8. ADR 0001 records that choice; a migration would revisit its framework and build-tool tradeoffs. ADR 0002 records the evaluated OpenRouter model independently of the UI.
- A read-only investigation checked the current code, installed development tools, and official Microsoft guidance for migration constraints.
- Environment investigation found Windows 11 Pro x64 build 26200 and .NET Framework 4.8. No dotnet, MSBuild, Visual Studio, or winapp command was found on PATH, and standard SDK/Visual Studio installation directories were absent. Windows App Runtime packages are installed, but build-tool installation will be needed for WinUI development. Microsoft documents a supported Visual Studio/WinUI workload path and an optional preview winapp CLI path; the plan uses supported stable tooling.

## Decisions so far

- 2026-11-09: The user requested WinUI only. [Ticket 03](issues/03-consolidate-winui-only.md)
  supersedes the earlier fallback requirement: consolidate application and checks
  under `src/` and `tests/`, retire the Windows Forms app, and use one set of
  build/check/run commands. Resolved: compilation and full published WinUI
  acceptance passed; see [verification](verification.md#ticket-03-verification).
  Previous fallback decisions below are historical.

- 2026-11-08: [Ticket 02](issues/02-complete-floating-experience-and-cutover.md)
  is resolved and ready for human review. The floating experience is complete, `scripts/run.ps1` now
  launches WinUI, and the Windows Forms version is retained as
  `scripts/run-winforms.ps1`. See [Ticket 02 verification](verification.md#ticket-02-verification).

- 2026-11-08: [Ticket 01](issues/01-migrate-enhancement-workflow-to-winui3.md)
  is resolved and ready for human review. The independent WinUI migration editor
  passes 26 checks; all 33 retained Windows Forms checks pass. Standards and spec
  reviews found no actionable issues. [Verification](verification.md) records
  build setup, credential compatibility, recovery, clipboard restoration, and
  actual 150% display-scale conditions. Ticket 02 has since been completed (see above).

- 2026-11-08: The user explicitly invoked implement for ticket 01. Ticket 01 is
  claimed, including its required stable build-tool installation. Ticket 02
  remains on hold; the normal Windows Forms launch path is retained. Earlier
  hold statements below record the design/publication stage.

- Requested target: WinUI 3.
- Q1 confirmed: modern native Windows appearance using Fluent design, with improved typography, spacing, controls, and polish while keeping enhancement familiar.
- Q2 confirmed: carry over all current behavior: draggable floating button, always-on-top editor, Settings, Paste, Enhance, result editing, Copy, Retry, Clear, Hide, tray restoration, and Quit.
- Q2 confirmed: retain the evaluated enhancement policy, OpenRouter model, and existing saved API key. This migration adds no product features beyond the agreed visual improvements.
- Q3 confirmed: keep the rough prompt above the enhanced prompt, with cleaner spacing and clearly grouped actions.
- Q4 confirmed: follow the Windows light/dark theme and accent color automatically, using native Fluent controls and standard window styling. No theme-settings screen is added.
- Q5 confirmed: deliver an unpackaged, self-contained application folder, bundling .NET and Windows App SDK dependencies to minimize runtime setup; the larger folder is an accepted tradeoff for preserving installer-free launch.
- Q6 confirmed: include installation of the required supported Microsoft build tools and updated build/run scripts in the implementation plan. No tools have been installed during the interview.
- Q7 confirmed: target Windows 11 x64 for the first migrated version. Windows 10 and ARM64 builds are deferred.
- Q8 confirmed: keep the existing WinForms application runnable until WinUI acceptance passes, then switch the normal launch command to WinUI while retaining a documented fallback.
- Q9 confirmed: acceptance uses the actual WinUI windows for enhancement, result editing, clipboard actions, dragging, topmost behavior, tray restoration, session retention, Retry, and Quit; also verify keyboard navigation, both themes, display scaling, and saved-key compatibility.
- Q9 confirmed: use controlled cloud responses for repeatable workflow checks and an isolated legacy-format credential for compatibility testing.
- The consolidated migration design is in [spec.md](spec.md). [ADR 0003](../../docs/adr/0003-winui3-interface-and-folder-deployment.md) records the framework and deployment tradeoff.
- Q10 confirmed: the consolidated migration spec accurately captures the user's intended migration. The interview is complete. The user explicitly deferred implementation with "Not implement yet."
- The user invoked to-spec after confirming the design. [spec.md](spec.md) is published to the local Markdown tracker using the required seven-section template, with 49 user stories and `Status: ready-for-agent`. It reuses the already-confirmed application, HTTP, and credential test boundaries. Publication leaves implementation and tooling installation on hold.
- The user invoked to-tickets, requested that the six proposed slices be merged into two, and approved the revised breakdown. The two tickets below are published with `Status: ready-for-agent`; their execution remains explicitly on hold. Approval authorized publication only.

## Approved tickets

| Ticket | Blocked by | Published issue |
| --- | --- | --- |
| 01: Migrate the enhancement workflow to WinUI 3 | None | [01](issues/01-migrate-enhancement-workflow-to-winui3.md) |
| 02: Complete the floating experience and cut over to WinUI | 01 | [02](issues/02-complete-floating-experience-and-cutover.md) |

Ticket 01 combines enhancement, clipboard/review, and recovery with required
build setup. Ticket 02 combines the floating experience, current prompt session,
folder publication, and cutover. Each includes its own acceptance checks, using
the previously confirmed application, HTTP, and credential boundaries. Ticket 01
is dependency-free, but neither ticket may begin until explicitly authorized.

## Fog

- No unresolved product decisions. Consolidated shared understanding is confirmed; implementation is explicitly on hold.
- Exact supported SDK/package versions and installation commands will be verified during implementation; this does not reopen the agreed behavior or delivery scope.

## Design tree

- Migration intent
  - Desired outcome and visual direction: settled; modern native Fluent appearance.
  - Behavior-preserving migration versus feature changes: settled; preserve current behavior and integration.
- Platform feasibility
  - Code and environment facts: investigation complete; see research below.
  - Packaging: settled; unpackaged self-contained folder.
  - Build-tool setup: included in implementation scope.
  - Windows and processor compatibility: settled; Windows 11 x64 first.
- Detailed experience and acceptance
  - Editor layout: settled; stacked editors with grouped actions.
  - Appearance: settled; native Fluent controls and Windows-controlled theme/accent.
  - Cutover: settled; retain working WinForms until acceptance, then switch normal launch with fallback retained.
  - Acceptance: settled; real WinUI desktop checks and external HTTP boundary, with controlled responses and isolated credential compatibility checks.
  - Consolidated shared understanding: confirmed; implementation explicitly deferred by the user.

## Research

- The machine runs Windows 11 Pro x64. Current provider, enhancement policy, and credential interfaces are independent of Windows Forms and can be reused or ported without changing enhancement semantics. The UI, application lifetime, clipboard integration, and desktop tests depend on Windows Forms and require migration.
- An unpackaged self-contained WinUI app can bundle Windows App SDK and .NET separately. This is a folder deployment rather than a single-file promise. See [Microsoft self-contained deployment](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/self-contained-deploy/deploy-self-contained-apps) and [unpackaged distribution](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/unpackage-winui-app).
- Preserve the explicit `%LOCALAPPDATA%\PromptEnhancer\openrouter-key.dpapi` path, DPAPI CurrentUser scope, and file format. Inference from existing code and the DPAPI contract: the unpackaged replacement under the same Windows account can reuse the key without re-entry. Verification must use an isolated legacy-format fixture rather than overwrite the production credential.
- WinUI windowing supports topmost presentation; native notification-area integration can replace WinForms NotifyIcon. Drag-versus-click behavior, collapse/minimize, Hide/Restore, pending completion while hidden, Quit cancellation, and tray cleanup need real UI checks.
- Microsoft documents WinUI development tooling in [the setup guide](https://learn.microsoft.com/en-us/windows/apps/get-started/start-here). The optional winapp CLI path is preview; do not silently make preview tooling a requirement. Select a supported stable build setup during implementation.

Interview complete. All design branches and consolidated shared understanding are confirmed. No migration implementation or tooling installation has begun; both are explicitly on hold at the user's request.
