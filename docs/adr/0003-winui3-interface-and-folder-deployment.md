# Migrate the interface to WinUI 3 with self-contained folder deployment

Status: accepted. Ticket 01 implemented and verified on 2026-11-08; ticket 02 remains on hold. Hold statements below record the earlier design stage.

The user chose WinUI 3 to give Prompt Enhancer a modern native Fluent appearance
while preserving its existing enhancement workflow and desktop behavior. Keep
the rough prompt above the editable enhanced prompt, group actions clearly, and
follow the Windows light/dark theme and accent color using native controls and
standard window styling. The evaluated enhancement policy, OpenRouter model,
and existing saved API key carry over.

Deliver an unpackaged, self-contained application folder with .NET and Windows
App SDK dependencies bundled. The larger folder and responsibility for updating
bundled runtimes are accepted in exchange for preserving installer-free launch.
This decision does not promise a single-file executable or add MSIX, Store
publication, or an updater.

This revisits ADR 0001's Windows Forms/.NET Framework and SDK-free build choice:
modern native presentation now outweighs avoiding build-tool installation. The
implementation plan includes supported Microsoft build tooling and updated
build/run scripts. ADR 0001's provider boundary, protected credential storage,
and in-memory prompt lifecycle remain applicable; ADR 0002's model selection
remains applicable. The existing application continues to use Windows Forms
until the migration is implemented and verified.

Target Windows 11 x64 for the first migrated build. Keep WinForms runnable until
the WinUI acceptance checks pass, then switch the normal launch command while
retaining a documented fallback. Verify behavior through actual WinUI windows,
with controlled cloud responses, external HTTP checks, and isolated legacy-format
credential checks; include keyboard navigation, both themes, and display scaling.

The [migration spec](../../.scratch/winui3-migration/spec.md) consolidates answers
Q1-Q9. The user confirmed shared understanding in Q10 and explicitly instructed
"Not implement yet." Implementation and tooling installation await an instruction
to begin. The
[migration map](../../.scratch/winui3-migration/map.md) records the interview.

References: [WinUI setup](https://learn.microsoft.com/en-us/windows/apps/get-started/start-here),
[self-contained deployment](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/self-contained-deploy/deploy-self-contained-apps).
