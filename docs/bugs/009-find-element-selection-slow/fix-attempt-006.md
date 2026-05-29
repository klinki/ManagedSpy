# Fix Attempt 006

## Attempt Status
awaiting-user-confirmation

## Goal
Fix the remaining user-visible **Find element on screen** failure after attempt 005 by preventing async refresh from overwriting finder selection and making the selected tree item visibly focused.

## Relation To Previous Attempts
Attempt 005 made refreshed top-level nodes use the same lazy child placeholders as finder-created nodes, but the user reported the finder still does not find/show the correct tree item. The unresolved symptoms are still part of the same finder/tree bug.

## Proposed Change
- Recompute the HWND at the moment the mouse-down is detected instead of relying only on the previous timer tick's highlighted handle.
- Ignore current-process HWNDs for the click target when a valid external clicked, post-overlay-hide, or highlighted handle is available.
- Await any currently running background refresh before mutating/selecting the tree, so an initial or manual refresh cannot clear the finder-created selection immediately afterward.
- Expand ancestors before setting the selected node, ensure the node is visible, bring ManagedSpy forward, focus the tree, and show a clear status message.

## Risks
- If a refresh is already running, finder selection waits for that refresh to complete instead of immediately mutating a tree that is about to be rebuilt.
- Bringing ManagedSpy forward changes focus after a finder click, but that matches the workflow goal of showing the selected tree item.

## Expected Verification
- Build with `.\build.ps1`.
- Run x64 artifact tests.
- Run x86 artifact tests.
- Copy updated `ManagedSpy.dll`/PDB into `artifacts\release` for the user's launch path.
- Ask the user to retest **Find element on screen** from `artifacts\release`.

## Files Or Components Involved
- `ManagedSpy\MainForm.cs`
- `docs\bugs\009-find-element-selection-slow\*`

## Actual Implementation Summary
- `elementFinderTimer_Tick` now recomputes the click HWND at mouse-down time, hides the finder overlay, and resolves the target from the clicked handle, a post-hide handle, or the last highlighted external handle.
- Finder selection now waits for any running background refresh before mutating/selecting the tree.
- Finder selection expands ancestors before selecting the node, ensures it is visible, restores ManagedSpy if minimized, activates the form, focuses the tree, and writes a clear status message.

## Test And Verification Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\attempt30-build` passed.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\attempt30-build\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 17/17.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\attempt30-build\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 17/17.
- Copied updated `ManagedSpy.dll`/PDB into `artifacts\release\x86` and `artifacts\release\x64`.

## Outcome And Remaining Gaps
- Awaiting user confirmation that **Find element on screen** selects the correct node quickly and visibly in the tree.
