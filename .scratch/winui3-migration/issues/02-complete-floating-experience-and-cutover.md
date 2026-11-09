# 02: Complete the floating experience and cut over to WinUI

Type: task
Status: ready-for-human
Execution: resolved (implementation authorized by the user's implement invocation on 2026-11-08); evidence in [verification](../verification.md#ticket-02-verification)
Blocked by: 01 (Migrate the enhancement workflow to WinUI 3)

**What to build:** The complete WinUI desktop experience around the migrated
enhancement workflow: a draggable floating button, always-on-top prompt editor,
retained current prompt session, Clear, Hide, tray restoration, and complete Quit
behavior. Deliver a self-contained Windows 11 x64 application folder, verify the
agreed behavior, and switch normal launching to WinUI while retaining a working
Windows Forms fallback.

- [x] Startup presents the small floating button. Clicking opens the migrated prompt editor; dragging repositions the button without accidentally opening the editor. The editor can be moved by its title bar without interfering with normal text selection/editing.
- [x] Both the floating button and prompt editor stay above ordinary application windows. Native interaction checks verify dragging and actual stacking while a companion editor is active, with usable positioning and scaling on the Windows 11 x64 target.
- [x] Closing or minimizing the editor returns to the floating button. Reopening preserves the rough prompt, enhanced prompt, and result edits. Preserve the existing launcher/editor close and application-exit semantics.
- [x] Clear is available while idle, empties both prompts, and permits a fresh enhancement using the saved protected key.
- [x] Hide from the supported editor/launcher controls removes both windows while retaining the current prompt session. The real notification-area menu and double-click restoration bring back the floating button and allow reopening the retained session, including when the tray icon is in overflow.
- [x] Hide and Quit remain available during enhancement. A completion while hidden stays hidden and is available when restored. Quit cancels pending work; a late completion cannot reopen a window or restore the application.
- [x] Quit through the existing application commands exits completely and removes the notification icon. Relaunch starts with empty prompts while the protected key remains available for enhancement. No prompt history or session persistence is introduced.
- [x] The complete interface follows the agreed native Fluent styling, stacked prompt layout, system light/dark theme and accent color. Verify keyboard navigation, normal text interaction, and display scaling across the floating button, editor, Settings, and relevant lifecycle states; record actual conditions and limitations.
- [x] Publish an unpackaged self-contained Windows 11 x64 application folder bundling both .NET and Windows App SDK runtime dependencies. Verify startup and the migrated workflow from the published folder and document any remaining prerequisite. Do not promise a single-file binary or introduce MSIX, an installer, Store publication, or an updater.
- [x] Build, check, publish, and run instructions are reproducible and reflect the supported stable toolchain. Document the larger folder tradeoff and responsibility for servicing bundled runtimes through future application updates.
- [x] Before cutover, warnings-as-errors compilation and the full migration regression suite pass. The suite exercises the actual WinUI workflow, explicit clipboard actions, native window/tray behavior, current prompt session lifecycle, failure/Retry recovery, supporting HTTP checks, isolated saved-key compatibility, and fresh-process behavior. Prior Windows Forms tests alone do not establish WinUI acceptance.
- [x] Repeatable checks use controlled cloud responses and non-sensitive isolated credentials, restore test-modified clipboard state, and avoid the production key. Do not claim controlled tests establish live model quality or require a new paid quality benchmark.
- [x] Keep the existing Windows Forms application runnable until acceptance passes. Only then switch normal launching to WinUI, retaining a documented, working Windows Forms fallback that remains compatible with the saved-key format.
- [x] Record acceptance evidence, commands, verified presentation conditions, any limitations, the cutover result, and the fallback procedure. Preserve the agreed enhancement policy, provider/model configuration, and all completed ticket 01 behavior.

## Comments

Approved as the second of two merged vertical slices. It combines the original
proposed floating-button, current prompt session, and folder-publication/cutover
tickets. Ticket 01 is the only blocking edge because this slice wraps and ships
its completed WinUI enhancement workflow.

The migration spec and ADRs 0001-0003 govern the work. The first migrated release
targets Windows 11 x64 for personal use. Windows 10/ARM64 builds, new product
features, a theme-settings screen, saved history, and provider/model changes
remain outside scope. The fallback is intentionally retained after cutover.
