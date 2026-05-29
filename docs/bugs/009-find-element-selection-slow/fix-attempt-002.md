# Fix Attempt 002

## Attempt Status
awaiting-user-confirmation

## Goal
Fix the crash reported after attempt 001 while preserving the faster **Find element on screen** tree selection.

## Relation To Previous Attempts
Attempt 001 made selection faster by materializing only the clicked control path. User retesting confirmed it was faster, but expansion crashed with a `NullReferenceException` in `treeWindow_BeforeExpand`.

## Proposed Change
- Keep the direct finder path selection from attempt 001.
- Harden `treeWindow_BeforeExpand` against null event/node values.
- Snapshot child tree nodes before mutating them during expansion.
- Skip null child nodes and null child proxy arrays.
- Clear/populate only proxy child nodes, leaving non-proxy nodes untouched.

## Risks
- This addresses the observed null-reference crash path, but target windows disappearing during expansion can still require additional stale-handle handling if reported.

## Expected Verification
- Build with `.\build.ps1`.
- Run x64 artifact tests.
- Run x86 artifact tests.
- Ask the user to retest **Find element on screen**.

## Files Or Components Involved
- `ManagedSpy\MainForm.cs`
- `docs\bugs\009-find-element-selection-slow\*`

## Actual Implementation Summary
- Added a guard for null expansion event/node values before touching `e.Node.Nodes`.
- Snapshot child nodes before clearing/populating them so expansion does not enumerate a collection being mutated.
- Skip null child nodes and null proxy child arrays.
- Clear and populate only nodes tagged with `ControlProxy`, avoiding unnecessary mutation of non-proxy nodes.

## Test And Verification Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\finder-crash-build` passed.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\finder-crash-build\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 17/17.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\finder-crash-build\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 17/17.

## Outcome And Remaining Gaps
- Local implementation and automated validation completed successfully.
- User reported a follow-up `NullReferenceException` in `treeWindow_AfterSelect`.
- Superseded by `fix-attempt-003.md`.
