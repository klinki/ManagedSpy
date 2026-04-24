# Fix Attempt 001

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Add right-click subtree refresh for control nodes and a toggleable persistent highlight for the selected component.

## Relation To Previous Attempts
First attempt for this bug.

## Proposed Change
- Extend the tree context menu with:
  - **Refresh Subtree**
  - **Keep Highlighted** (checkable)
- Implement subtree rebuild logic for the selected control node.
- Implement persistent highlight using a dedicated overlay instance and periodic rectangle refresh.

## Risks
- Recursive subtree refresh could be expensive on very large trees.
- Persistent overlay must not interfere with existing finder/flash overlay behavior.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Startup smoke run of the produced x64 binary.

## Implementation Summary
- Added tree context menu initialization in `MainForm` to register:
  - `Refresh Subtree`
  - `Keep Highlighted` (checkable)
- Added subtree rebuild helpers to repopulate the selected control branch and preserve expanded-node state.
- Added persistent-highlight state and a dedicated timer + overlay form (`persistentHighlightOverlay`) to keep selected control highlighted until toggled off.
- Added context-menu opening logic to enable/disable relevant actions for control nodes and synchronize check state with the currently highlighted handle.
- Kept existing temporary flash and finder overlays untouched.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out` succeeded for Release x86/x64.
- Startup smoke run from x64 build output stayed alive during initial window scan.

## Outcome
Feature-level fix is implemented and builds cleanly. Subtree refresh and persistent highlight are now available from the tree context menu for component nodes.

## Next Step
User confirmation in real target applications with dynamically added descendants (including the `TableLayoutPanelEx` case).

## Remaining Gaps
- Behavioral confirmation in the reported application environment is still pending.
