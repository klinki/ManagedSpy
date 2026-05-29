# Fix Attempt 004

## Attempt Status
awaiting-user-confirmation

## Goal
Restore correct tree contents after the faster finder path while keeping selection responsive and crash-free.

## Relation To Previous Attempts
Attempt 003 fixed the reported `treeWindow_AfterSelect` null-selection crash. User retesting then reported the tree sometimes showed incomplete results and missed sub-components, which points to the path-only tree materialization leaving branches partially populated.

## Proposed Change
- Keep direct finder path selection.
- Add lightweight placeholder child nodes to newly created proxy nodes so controls remain expandable without preloading entire subtrees.
- Populate only the expanded proxy node's immediate children in `treeWindow_BeforeExpand` instead of preloading every child node's children.
- While building a finder path, populate each ancestor's immediate children so expanded path branches contain real siblings/sub-components rather than only the clicked path.
- Populate the selected node's immediate children so its sub-components are available after selection.

## Risks
- Leaf nodes may briefly show an expand glyph until expanded once and found empty.
- Target-specific handle churn during expansion can still require stale-handle handling if reported.

## Expected Verification
- Build with `.\build.ps1`.
- Run x64 artifact tests.
- Run x86 artifact tests.
- Refresh the user's `artifacts\release` `ManagedSpy.dll` so they can retest the same launch path.
- Ask the user to retest **Find element on screen** and tree completeness.

## Files Or Components Involved
- `ManagedSpy\MainForm.cs`
- `docs\bugs\009-find-element-selection-slow\*`

## Actual Implementation Summary
- Added lightweight placeholder child nodes for newly created proxy nodes so controls remain expandable without broad preloading.
- Changed `treeWindow_BeforeExpand` to populate only the expanded proxy node's immediate children.
- Updated finder path materialization to populate each proxy ancestor's immediate children before descending to the target, so expanded finder branches include real sibling/sub-component nodes.
- Populated the selected target node's immediate children after direct path selection.
- Kept programmatic finder path expansion from re-triggering tree mutation while still leaving the path populated.
- Updated the Layout diagram to remove duplicate full `T/R/B/L` summaries, show edge values as numbers only, and center the content label inside the content box.

## Test And Verification Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\finder-tree-build` passed.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\finder-tree-build\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 17/17.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\finder-tree-build\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 17/17.
- Copied the updated `ManagedSpy.dll` and `ManagedSpy.pdb` from the validated build into `artifacts\release\x86` and `artifacts\release\x64`.

## Outcome And Remaining Gaps
- Local implementation and automated validation completed successfully.
- Bug remains open until the user confirms **Find element on screen** remains fast and the tree no longer misses sub-components.
