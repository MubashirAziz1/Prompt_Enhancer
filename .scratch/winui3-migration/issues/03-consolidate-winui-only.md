# 03: Consolidate the repository into one WinUI application

Type: task
Status: ready-for-human
Execution: resolved
Blocked by: 02

The user requested WinUI only on 2026-11-09. This supersedes the earlier
requirement to retain a runnable Windows Forms fallback.

Consolidate the WinUI application under `src` and its checks under `tests`.
Keep the existing provider, enhancement policy, protected-key format, and
in-progress WinUI changes. Remove the old application, redundant launch/build
paths, and stale Framework executables. Make the standard build/check/run
commands operate on WinUI and verify the relocated application and publication.

Test-only probes may continue using the Framework compiler for independent
saved-key compatibility and native desktop automation; they are not a second
Prompt Enhancer implementation.

## Answer

Consolidated the application under `src/` and its WinUI host under `tests/`,
preserving the existing in-progress WinUI changes. Removed the Windows Forms
application, its desktop suite, redundant build/check/run scripts, and obsolete
Framework application/test/evaluation executables. Retained the provider contract
checks in the WinUI host; the manual opt-in evaluation now drives WinUI too.
The three core provider/policy/credential files are unchanged.

`scripts/build.ps1 -Tests` passed with warnings as errors.
`scripts/check-published-winui.ps1` passed the complete published workflow suite,
identical application-resource comparison, app-local runtime loading,
single-instance handoff, production floating startup, and complete Quit.
An initial clipboard-contention failure passed on retry without test-code changes;
the successful run verified original clipboard restoration. No paid evaluation
or production-key access was performed. See [verification](../verification.md#ticket-03-verification).
