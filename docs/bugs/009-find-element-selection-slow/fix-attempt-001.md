# Fix Attempt 001

## Attempt Status
awaiting-user-confirmation

## Goal
Make **Find element on screen** select the matching tree item quickly without doing full refresh or broad sibling subtree population.

## Relation To Previous Attempts
This is the first attempt for the finder-selection slowdown. It follows the Refresh fix because both regressions were caused by expensive discovery work happening on user-visible UI paths.

## Proposed Change
- Materialize only the clicked control's process/top-level/ancestor path in the tree.
- Reuse existing tree nodes where present.
- Create missing path nodes directly from the known `ControlProxy` chain instead of refreshing all windows.
- Suppress broad `treeWindow_BeforeExpand` child preloading while expanding the finder-created path for visibility.
- Preserve normal tree preloading for manual user expansion.

## Risks
- A process node created by finder path selection may initially contain only the selected branch until the user refreshes or manually browses elsewhere.
- If a target recreates handles during selection, the direct path may still fail and leave no selection.

## Expected Verification
- Build with `.\build.ps1`.
- Run x64 artifact tests.
- Run x86 artifact tests.
- Ask the user to retest **Find element on screen**.

## Files Or Components Involved
- `ManagedSpy\MainForm.cs`

## Actual Implementation Summary
- Changed finder click handling to materialize the clicked control's tree path directly from the known `ControlProxy` parent chain.
- Reused existing process/control nodes when present and created only missing path nodes.
- Removed the finder path's dependence on a full refresh when the matching node is not already loaded.
- Suppressed broad "one step ahead" `treeWindow_BeforeExpand` population while programmatically expanding the finder-created path for visibility.
- Left normal manual tree expansion behavior unchanged.

## Test And Verification Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\finder-layout-build-final` passed.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\finder-layout-build-final\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 17/17.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\finder-layout-build-final\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 17/17.

## Outcome And Remaining Gaps
- Local implementation and automated validation completed successfully.
- User confirmed the selection path is faster, but reported a `NullReferenceException` crash in `treeWindow_BeforeExpand`.
- Superseded by `fix-attempt-002.md`.
