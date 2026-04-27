# Fix Attempt 007

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Make `Keep Highlighted` and related tree context-menu actions target the exact tree node that opened the menu, rather than any prior selection left by the magnifier.

## Relation To Previous Attempts
Follow-up to `fix-attempt-006.md`, which corrected geometry back to the magnifier path, but user feedback showed the context-menu target itself could still be wrong.

## Proposed Change
- Capture the tree node that opened the context menu.
- Use that explicit menu target for `Keep Highlighted`, `Refresh Subtree`, and `Show Window`.
- Clear the stored menu target when the context menu closes.

## Risks
- Keyboard-opened context menus still need a sensible fallback target.
- The stored context target must not outlive the menu interaction.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Startup smoke run of the produced x64 binary.

## Implementation Summary
- Added `treeMenuTargetNode` field in `MainForm`.
- Added `GetTreeMenuTargetNode` and `SetTreeMenuTargetNode` helpers.
- Updated `treeWindow_NodeMouseClick` to store the target node before showing the context menu.
- Updated context-menu opening/closing and action handlers to use the explicit menu target node rather than blindly using `treeWindow.SelectedNode`.
- Applied the same explicit-target behavior to `Show Window` and `Refresh Subtree` for consistency.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt7` succeeded for Release x86/x64.
- Startup smoke run from x64 build output stayed alive during initial window scan.

## Outcome
Tree context-menu actions now bind to the exact node that opened the menu, preventing stale magnifier selection state from hijacking `Keep Highlighted`.

## Next Step
User confirmation in the scenario where `Keep Highlighted` previously followed the last magnifier target.

## Remaining Gaps
- Behavioral confirmation in the user environment is still pending.
