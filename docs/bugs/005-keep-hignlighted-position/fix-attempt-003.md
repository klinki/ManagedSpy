# Fix Attempt 003

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Remove duplicate control nodes that can appear in the tree after target dialog close/reopen lifecycle updates.

## Relation To Previous Attempts
Follow-up to `fix-attempt-002.md`. The user confirmed the highlight positioning is better/fixed for the lifecycle case, but noticed duplicated tree items after the lifecycle invalidation changes.

## Proposed Change
- Avoid adding duplicate child nodes for the same `ControlProxy.Handle` when rebuilding or lazily expanding tree branches.
- After handle-change notifications update tree node handles, remove duplicate proxy nodes within sibling collections.
- Preserve normal tree refresh behavior and avoid changing proxy geometry logic.

## Risks
- Some target apps could theoretically expose different controls with the same HWND, but ManagedSpy keys nodes by handle already, so duplicate siblings with the same handle cannot be reliably distinguished in the current tree.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\005-keep-hignlighted-position\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64` into an isolated output root.
- Run `ManagedSpy.Tests.dll` from both built outputs with matching `/Platform` values.
- Ask the user to retest the Settings close/reopen flow and check for duplicate tree items.

## Implementation Summary
- Added `AddProxyNodeIfMissing` so subtree rebuilds and lazy expansion skip duplicate sibling nodes with the same `ControlProxy.Handle`.
- Added duplicate cleanup after handle-change notifications update tree node handles.
- Preserved the first sibling node for a handle and removed later duplicate siblings.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\dedupe-attempt3-build` succeeded for Release x86/x64.
- Initial x64/x86 test runs each hit the known UIAutomation startup timeout waiting for the ManagedSpy main window.
- Manual smoke check showed the same build can start and show the `Managed Spy` main window after a longer wait.
- Rerun `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\dedupe-attempt3-build\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 14/14 tests.
- Rerun `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\dedupe-attempt3-build\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 14/14 tests.

## Outcome
- Tree population and handle-change updates now de-duplicate sibling nodes by live proxy handle.

## Next Step
- Ask the user to retest the Settings close/reopen flow and check whether duplicate tree items are gone.

## Remaining Gaps
- Direct reproduction in eM Client is still unavailable in this environment.
