# Fix Attempt 003

## Attempt Status
awaiting-user-confirmation

## Goal
Fix the follow-up crash reported in `treeWindow_AfterSelect` while preserving the faster finder selection path.

## Relation To Previous Attempts
Attempt 002 fixed a `treeWindow_BeforeExpand` null-reference crash. User retesting then reported a new `NullReferenceException` in `treeWindow_AfterSelect` at the selected-node access.

## Proposed Change
- Use the event's selected node when available instead of assuming `treeWindow.SelectedNode` is non-null.
- Guard against null selected nodes.
- Clear the property grid, Layout tab, status text, and event logging state if selection is temporarily null.
- Keep normal selection behavior unchanged for valid process/control nodes.

## Risks
- If selection becomes null because a target is removed during finder selection, the UI will clear the selection-dependent panels rather than keeping stale data.

## Expected Verification
- Build with `.\build.ps1`.
- Run x64 artifact tests.
- Run x86 artifact tests.
- Refresh the default `artifacts\release` output so the user can retest the same launch path.
- Ask the user to retest **Find element on screen**.

## Files Or Components Involved
- `ManagedSpy\MainForm.cs`
- `docs\bugs\009-find-element-selection-slow\*`

## Actual Implementation Summary
- Changed `treeWindow_AfterSelect` to use `e.Node` when available instead of assuming `treeWindow.SelectedNode` is non-null.
- Added a null-selected-node branch that clears selection-dependent UI state and stops logging safely.
- Preserved the normal property grid, Layout tab, status text, and logging update path for valid selections.

## Test And Verification Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\finder-select-crash-build` passed.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\finder-select-crash-build\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 17/17.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\finder-select-crash-build\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 17/17.
- A default `.\build.ps1` refresh of `artifacts\release` was blocked because the target `MailClient` processes still have x86 `Ijwhost.dll` and `ManagedSpyLib.dll` loaded from `artifacts\release\x86`.
- Copied the updated `ManagedSpy.dll` and `ManagedSpy.pdb` from the validated build into `artifacts\release\x86` and `artifacts\release\x64` so the reported `treeWindow_AfterSelect` crash fix is available on the user's current launch path.

## Outcome And Remaining Gaps
- Local implementation and automated validation completed successfully.
- User reported tree results are incomplete and some sub-components are missing after finder selection.
- Superseded by `fix-attempt-004.md`.
- A full default artifact rebuild still requires closing/restarting the target processes that have the x86 hook DLL loaded.
